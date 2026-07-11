/**
 * @file mis-herencias.tsx
 * @description Pantalla que lista las herencias asignadas al usuario actual (Frame 24).
 *
 * Antes solo mostraba un conteo agrupado por titular con un badge "Disponible/No
 * disponible", sin exponer el ESTADO real de cada asignación (Pendiente/Aceptado/
 * Rechazado) ni forma de actuar sobre él. El backend ya expone
 * PATCH /api/asignaciones/{id}/estado para que el BENEFICIARIO acepte o rechace una
 * herencia pendiente; esta pantalla ahora lo consume: cada activo pendiente muestra
 * botones "Aceptar"/"Rechazar" en vez de quedar mudo hasta que alguien reclame el link
 * de invitación original (que además es efímero: solo se imprime una vez por consola).
 */

import React, { useState, useCallback } from 'react';
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
import { useRouter, useFocusEffect } from 'expo-router';
import { ArrowLeft, Info, HelpCircle } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService, MiHerenciaDTO } from '../services/assets.service';

/** Agrupación por titular, armada en el cliente, para mostrar una card por cada emisor. */
interface HerenciaAgrupada {
  titular: string;
  disponible: boolean;
  items: MiHerenciaDTO[];
}

export default function MisHerenciasScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const [loading, setLoading] = useState(true);
  const [herencias, setHerencias] = useState<MiHerenciaDTO[]>([]);
  // Id de la asignación que tiene una acción (Aceptar/Rechazar) en curso, para
  // deshabilitar SOLO esos botones puntuales y no toda la pantalla mientras responde.
  const [procesandoId, setProcesandoId] = useState<number | null>(null);

  const fetchHerencias = useCallback(async () => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }
    try {
      const data = await AssetsService.getMisHerencias();
      setHerencias(data);
    } catch (err: any) {
      console.log('Error al cargar mis herencias:', err.message);
    } finally {
      setLoading(false);
    }
  }, [token]);

  // Recarga cada vez que la pantalla entra en foco (por ejemplo, al volver de aceptar
  // una invitación desde el link público en otra pantalla).
  useFocusEffect(
    useCallback(() => {
      fetchHerencias();
    }, [fetchHerencias])
  );

  /**
   * Agrupa las asignaciones por titular emisor para consolidar la tarjeta del Frame 24,
   * conservando cada ítem individual (con su propio estado) para poder actuar sobre él.
   */
  const obtenerHerenciasAgrupadas = (): HerenciaAgrupada[] => {
    const agrupado: { [key: string]: HerenciaAgrupada } = {};

    herencias.forEach((item) => {
      const key = item.titularNombre;
      if (!agrupado[key]) {
        agrupado[key] = { titular: item.titularNombre, disponible: item.disponible, items: [] };
      }
      agrupado[key].items.push(item);
    });

    return Object.values(agrupado);
  };

  const herenciasAgrupadas = obtenerHerenciasAgrupadas();

  /**
   * Acepta o rechaza una asignación puntual. "nuevoEstado" usa los mismos valores que
   * el enum EstadoBeneficiario del backend: 2 = Aceptado, 3 = Rechazado.
   */
  const handleResponder = async (asignacionId: number, nuevoEstado: 2 | 3) => {
    setProcesandoId(asignacionId);
    try {
      await AssetsService.actualizarEstadoAsignacion(asignacionId, nuevoEstado);
      // Actualiza el estado localmente en vez de esperar un refetch completo: la
      // transición ya se confirmó en el servidor (si hubiera fallado, el catch de abajo
      // ni siquiera llegaría a este punto).
      setHerencias((prev) =>
        prev.map((h) =>
          h.asignacionId === asignacionId
            ? { ...h, estado: nuevoEstado === 2 ? 'Aceptado' : 'Rechazado' }
            : h
        )
      );
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo actualizar la herencia.');
    } finally {
      setProcesandoId(null);
    }
  };

  const confirmarRechazo = (asignacionId: number, activoNombre: string) => {
    Alert.alert(
      'Rechazar herencia',
      `¿Estás seguro de que querés rechazar "${activoNombre}"? Esta acción no se puede deshacer.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Rechazar', style: 'destructive', onPress: () => handleResponder(asignacionId, 3) },
      ]
    );
  };

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
                <View style={styles.ownerHeaderRow}>
                  <Text style={styles.ownerName}>{item.titular}</Text>
                  <View style={[styles.badge, item.disponible ? styles.badgeGreen : styles.badgeOrange]}>
                    <Text style={[styles.badgeText, item.disponible ? styles.textGreen : styles.textOrange]}>
                      {item.disponible ? 'Disponible' : 'No disponible'}
                    </Text>
                  </View>
                </View>

                {item.items.map((asig) => (
                  <View key={asig.asignacionId} style={styles.itemCard}>
                    <View style={styles.itemInfoRow}>
                      <Text style={styles.itemName}>{asig.activoNombre}</Text>
                      <Text
                        style={[
                          styles.itemEstado,
                          asig.estado === 'Aceptado' && styles.estadoAceptado,
                          asig.estado === 'Rechazado' && styles.estadoRechazado,
                          asig.estado === 'Pendiente' && styles.estadoPendiente,
                        ]}
                      >
                        {asig.estado}
                      </Text>
                    </View>

                    {asig.estado === 'Pendiente' && (
                      <View style={styles.actionsRow}>
                        {procesandoId === asig.asignacionId ? (
                          <ActivityIndicator size="small" color="#23856C" />
                        ) : (
                          <>
                            <TouchableOpacity
                              style={styles.acceptSmallButton}
                              onPress={() => handleResponder(asig.asignacionId, 2)}
                            >
                              <Text style={styles.acceptSmallButtonText}>Aceptar</Text>
                            </TouchableOpacity>
                            <TouchableOpacity
                              style={styles.rejectSmallButton}
                              onPress={() => confirmarRechazo(asig.asignacionId, asig.activoNombre)}
                            >
                              <Text style={styles.rejectSmallButtonText}>Rechazar</Text>
                            </TouchableOpacity>
                          </>
                        )}
                      </View>
                    )}
                  </View>
                ))}

                <View style={styles.infoBox}>
                  <Info size={20} color="#0E4A4C" style={styles.infoIcon} />
                  <Text style={styles.infoText}>
                    Los activos aceptados estarán disponibles cuando {item.titular.split(' ')[0]} no responda a la verificación de vida.
                  </Text>
                </View>
              </View>
            )}
          />
        )}

        {/* Acceso al flujo de certificado de defunción: solo tiene sentido si ya
            aceptaste al menos una herencia (regla que valida subir-certificado.tsx). */}
        <TouchableOpacity
          style={styles.uploadCertButton}
          onPress={() => router.push('/subir-certificado')}
        >
          <Text style={styles.uploadCertButtonText}>Subir certificado de defunción</Text>
        </TouchableOpacity>
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
  uploadCertButton: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1.5,
    borderColor: '#02213D',
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 20,
  },
  uploadCertButtonText: {
    color: '#02213D',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
  cardContainer: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 20,
    gap: 12,
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
  ownerHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  ownerName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  badge: {
    borderWidth: 1.5,
    borderRadius: 12,
    paddingVertical: 4,
    paddingHorizontal: 12,
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
  itemCard: {
    backgroundColor: '#F7FBF3',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#EEFDE2',
    padding: 12,
    gap: 8,
  },
  itemInfoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  itemName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1a2e2e',
    flex: 1,
  },
  itemEstado: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 12,
  },
  estadoPendiente: {
    color: '#E2A53C',
  },
  estadoAceptado: {
    color: '#2E7D32',
  },
  estadoRechazado: {
    color: '#D32F2F',
  },
  actionsRow: {
    flexDirection: 'row',
    gap: 10,
  },
  acceptSmallButton: {
    flex: 1,
    backgroundColor: '#2E7D32',
    borderRadius: 8,
    paddingVertical: 8,
    alignItems: 'center',
  },
  acceptSmallButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
  },
  rejectSmallButton: {
    flex: 1,
    backgroundColor: '#FFFFFF',
    borderWidth: 1.5,
    borderColor: '#D32F2F',
    borderRadius: 8,
    paddingVertical: 8,
    alignItems: 'center',
  },
  rejectSmallButtonText: {
    color: '#D32F2F',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
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
