/**
 * @file beneficiarios.tsx
 * @description Pantalla que lista los beneficiarios del usuario autenticado (Frame 8).
 * 
 * Consume el endpoint GET /api/beneficiarios para listar a las personas asignadas.
 * Determina el estado ("Verificado" / "Pendiente") de manera inteligente:
 * - Los beneficiarios semilla Ana y Laura figuran como "Verificado" para demos.
 * - Los beneficiarios nuevos o Martín figuran como "Pendiente" (esperando registro).
 * Dispone de un botón para agregar un nuevo beneficiario redirigiendo al formulario.
 */

import React, { useState, useEffect, useCallback } from 'react';
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
import { useRouter, useLocalSearchParams, useFocusEffect } from 'expo-router';
import { ArrowLeft, ArrowRight, HelpCircle, Info } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../../context/AuthContext';
import { AssetsService, BeneficiarioDTO } from '../../services/assets.service';

export default function BeneficiariosScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const { deleted } = useLocalSearchParams();
  const [showSuccessBanner, setShowSuccessBanner] = useState(false);
  const [loading, setLoading] = useState(true);
  const [beneficiarios, setBeneficiarios] = useState<BeneficiarioDTO[]>([]);

  // Función para volver a cargar la lista (se puede invocar al enfocar la pantalla)
  const fetchBeneficiarios = async () => {
    if (!token) return;
    try {
      const data = await AssetsService.getBeneficiarios(token);
      setBeneficiarios(data);
    } catch (err: any) {
      console.log('Error loading beneficiaries:', err.message);
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

  // Recargar la lista automáticamente cada vez que el usuario enfoca esta pestaña
  useFocusEffect(
    useCallback(() => {
      fetchBeneficiarios();
    }, [token])
  );

  // Controlar la visualización del banner de éxito al eliminar (Frame 33)
  useEffect(() => {
    if (deleted === 'true') {
      setShowSuccessBanner(true);
      const timer = setTimeout(() => {
        setShowSuccessBanner(false);
        // Limpiamos los params de búsqueda reemplazando la ruta
        router.setParams({ deleted: undefined });
      }, 4000);
      return () => clearTimeout(timer);
    }
  }, [deleted]);

  /**
   * Determina si un beneficiario está verificado, pendiente o rechazado.
   * Lee la propiedad de estado real de la base de datos (1 = Pendiente, 2 = Verificado/Aceptado, 3 = Rechazado)
   * con fallback basado en el nombre para la demo original.
   */
  const obtenerEstadoBeneficiario = (item: BeneficiarioDTO): 'Verificado' | 'Pendiente' | 'Rechazado' => {
    if (item.estado !== undefined) {
      if (item.estado === 2) return 'Verificado';
      if (item.estado === 3) return 'Rechazado';
      return 'Pendiente';
    }
    const n = item.nombre.toLowerCase();
    if (n.includes('ana') || n.includes('laura')) {
      return 'Verificado';
    }
    return 'Pendiente';
  };

  /**
   * Navega a la pantalla de detalle del beneficiario (Frame 9).
   */
  const handleVerDetalles = (item: BeneficiarioDTO) => {
    router.push({
      pathname: '/detalle-beneficiario',
      params: { id: item.id.toString() },
    });
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando beneficiarios...</Text>
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
          <TouchableOpacity onPress={() => router.replace('/')} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Mis beneficiarios</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <View style={styles.content}>
        {showSuccessBanner && (
          <View style={styles.successBanner}>
            <Info size={18} color="#0E4A4C" />
            <Text style={styles.successBannerText}>Beneficiario eliminado con éxito</Text>
          </View>
        )}

        {beneficiarios.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin beneficiarios aún</Text>
            <Text style={styles.emptyStateText}>
              Presiona el botón de abajo para registrar a tu primer heredero y asignarle activos.
            </Text>
          </View>
        ) : (
          <FlatList
            data={beneficiarios}
            keyExtractor={(item) => item.id.toString()}
            contentContainerStyle={styles.listContent}
            showsVerticalScrollIndicator={false}
            renderItem={({ item }) => {
              const estado = obtenerEstadoBeneficiario(item);
              return (
                <View style={styles.beneficiaryCard}>
                  <View style={styles.cardHeaderRow}>
                    <View style={styles.cardHeaderLeft}>
                      <Text style={styles.cardName}>{item.nombre}</Text>
                      <Text style={styles.cardRelation}>{item.parentesco}</Text>
                    </View>
                    <Text style={[
                      styles.cardStatus,
                      estado === 'Verificado' && styles.statusVerified,
                      estado === 'Pendiente' && styles.statusPending,
                      estado === 'Rechazado' && styles.statusRejected
                    ]}>
                      {estado}
                    </Text>
                  </View>

                  <TouchableOpacity
                    style={styles.detailsButton}
                    onPress={() => handleVerDetalles(item)}
                  >
                    <Text style={styles.detailsButtonText}>Detalles</Text>
                    <ArrowRight size={18} color="#FFFFFF" style={styles.detailsIcon} />
                  </TouchableOpacity>
                </View>
              );
            }}
          />
        )}

        <TouchableOpacity
          style={styles.addBeneficiaryButton}
          onPress={() => router.push('/agregar-beneficiario')}
        >
          <Text style={styles.addBeneficiaryText}>+ Agregar beneficiario</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo verde claro pastel
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
    justifyContent: 'space-between',
  },
  listContent: {
    gap: 16,
    paddingBottom: 20,
  },
  beneficiaryCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 20,
    gap: 16,
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
  cardHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  cardHeaderLeft: {
    flex: 1,
  },
  cardName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 17,
    color: '#1a2e2e',
    marginBottom: 4,
  },
  cardRelation: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  cardStatus: {
    fontSize: 13,
    fontFamily: 'MPLUS2-Bold',
  },
  statusVerified: {
    color: '#2E7D32', // Verde
  },
  statusPending: {
    color: '#E2A53C', // Naranja/Amarillo
  },
  statusRejected: {
    color: '#D32F2F', // Rojo
  },
  detailsButton: {
    backgroundColor: '#7EA49E', // Color verde-gris opaco del mockup
    borderRadius: 12,
    height: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
  },
  detailsButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
  detailsIcon: {
    marginLeft: 6,
  },
  addBeneficiaryButton: {
    backgroundColor: '#FFFFFF', // Botón blanco con borde verde oscuro (Frame 8)
    height: 52,
    borderRadius: 16,
    borderWidth: 1.5,
    borderColor: '#2E7D32',
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    marginTop: 10,
    marginBottom: Platform.OS === 'ios' ? 10 : 0,
  },
  addBeneficiaryText: {
    color: '#2E7D32',
    fontSize: 16,
    fontFamily: 'MPLUS2-Bold',
  },
  // Banner de éxito (Frame 33)
  successBanner: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    borderRadius: 20,
    paddingVertical: 10,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 16,
    alignSelf: 'center',
    shadowColor: '#1a2e2e',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
    width: '100%',
    justifyContent: 'center',
  },
  successBannerText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    color: '#1a2e2e',
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
