/**
 * @file mis-herencias.tsx
 * @description Pantalla que lista las herencias asignadas al usuario actual (Frame 24).
 * 
 * Consulta al backend las asignaciones donde el correo del usuario logueado figura como beneficiario,
 * mostrando las tarjetas con el nombre del titular emisor, parentesco, cantidad de activos heredados
 * y el estado de disponibilidad ("No disponible" si la verificación de vida sigue en curso).
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  FlatList,
  Platform,
  Alert,
} from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, Info, HelpCircle } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService, MiHerenciaDTO } from '../services/assets.service';

export default function MisHerenciasScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const [loading, setLoading] = useState(true);
  const [herencias, setHerencias] = useState<MiHerenciaDTO[]>([]);

  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const fetchHerencias = async () => {
      try {
        const data = await AssetsService.getMisHerencias(token);
        setHerencias(data);
      } catch (err: any) {
        // Redirigir al inicio si la sesión expiró o el token es inválido
        if (
          err.message.includes('401') ||
          err.message.includes('autorización') ||
          err.message.includes('token')
        ) {
          router.replace('/(auth)/welcome');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchHerencias();
  }, [token]);

  /**
   * Agrupa las asignaciones por titular emisor para consolidar la tarjeta del Frame 24
   */
  const obtenerHerenciasAgrupadas = () => {
    const agrupado: { [key: string]: { titular: string; parentesco: string; count: number; disponible: boolean } } = {};
    
    herencias.forEach((item) => {
      const key = item.titularNombre;
      if (!agrupado[key]) {
        agrupado[key] = {
          titular: item.titularNombre,
          parentesco: item.parentesco || 'Familiar',
          count: 0,
          disponible: item.disponible,
        };
      }
      if (item.activoNombre !== 'Ninguno') {
        agrupado[key].count++;
      }
    });

    return Object.values(agrupado);
  };

  const herenciasAgrupadas = obtenerHerenciasAgrupadas();

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando herencias...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.back()} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Mis herencias</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <View style={styles.content}>
        {herenciasAgrupadas.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin asignaciones aún</Text>
            <Text style={styles.emptyStateText}>
              Aún ningún usuario te ha designado como beneficiario de sus activos digitales.
            </Text>
          </View>
        ) : (
          <FlatList
            data={herenciasAgrupadas}
            keyExtractor={(item) => item.titular}
            contentContainerStyle={styles.listContent}
            renderItem={({ item }) => (
              <View style={styles.cardContainer}>
                <View style={styles.inheritanceCard}>
                  <View style={styles.cardLeft}>
                    <Text style={styles.ownerName}>{item.titular}</Text>
                    <Text style={styles.ownerMeta}>
                      {item.parentesco} - {item.count} {item.count === 1 ? 'activo asignado' : 'activos asignados'}
                    </Text>
                  </View>
                  <View style={styles.badgeContainer}>
                    <View style={[styles.badge, item.disponible ? styles.badgeGreen : styles.badgeOrange]}>
                      <Text style={[styles.badgeText, item.disponible ? styles.textGreen : styles.textOrange]}>
                        {item.disponible ? 'Disponible' : 'No disponible'}
                      </Text>
                    </View>
                  </View>
                </View>

                <View style={styles.infoBox}>
                  <Info size={20} color="#0E4A4C" style={styles.infoIcon} />
                  <Text style={styles.infoText}>
                    Los activos estarán disponibles cuando {item.titular.split(' ')[0]} no responda a la verificación de vida.
                  </Text>
                </View>
              </View>
            )}
          />
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo general verde pastel
  },
  loadingContainer: {
    flex: 1,
    backgroundColor: '#DAF8BD',
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#23856C',
    marginTop: 12,
  },
  header: {
    paddingHorizontal: 20,
    paddingBottom: 20,
    borderBottomLeftRadius: 20,
    borderBottomRightRadius: 20,
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  backButton: {
    padding: 4,
  },
  headerTitle: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    textAlign: 'center',
  },
  content: {
    flex: 1,
    padding: 20,
  },
  listContent: {
    gap: 20,
  },
  cardContainer: {
    gap: 12,
  },
  inheritanceCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingVertical: 20,
    paddingHorizontal: 20,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.08,
        shadowRadius: 6,
      },
      android: {
        elevation: 3,
      },
    }),
  },
  cardLeft: {
    flex: 1,
  },
  ownerName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
    marginBottom: 4,
  },
  ownerMeta: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  badgeContainer: {
    marginLeft: 12,
  },
  badge: {
    borderWidth: 1.5,
    borderRadius: 12,
    paddingVertical: 4,
    paddingHorizontal: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeOrange: {
    borderColor: '#E2A53C',
    backgroundColor: '#FFFFFF',
  },
  badgeGreen: {
    borderColor: '#2E7D32',
    backgroundColor: '#EEFDE2',
  },
  badgeText: {
    fontSize: 12,
    fontFamily: 'MPLUS2-Bold',
  },
  textOrange: {
    color: '#E2A53C',
  },
  textGreen: {
    color: '#2E7D32',
  },
  infoBox: {
    backgroundColor: '#C5E2D0', // Fondo celeste/verde agua claro del mockup
    borderRadius: 12,
    borderWidth: 1.2,
    borderColor: '#A1CBB2',
    paddingVertical: 16,
    paddingHorizontal: 16,
    alignItems: 'center',
    flexDirection: 'column',
    gap: 8,
  },
  infoIcon: {
    marginBottom: 2,
  },
  infoText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#0E4A4C',
    textAlign: 'center',
    lineHeight: 20,
    paddingHorizontal: 8,
  },
  // Vacío / Empty State
  emptyStateCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingVertical: 40,
    paddingHorizontal: 24,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 16,
    marginTop: 40,
  },
  emptyStateTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#1a2e2e',
  },
  emptyStateText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#8A9E95',
    textAlign: 'center',
    lineHeight: 20,
  },
});
