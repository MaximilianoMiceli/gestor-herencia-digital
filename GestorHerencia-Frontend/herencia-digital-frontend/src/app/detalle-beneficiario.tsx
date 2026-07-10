/**
 * @file detalle-beneficiario.tsx
 * @description Pantalla de detalle y gestión de beneficiarios (Frames 9, 28 y 33).
 * 
 * Permite visualizar el perfil del heredero, listando los activos digitales del usuario titular
 * que posee asignados con un enlace directo a sus detalles.
 * Dispone de la funcionalidad de borrado con confirmación modal translúcida (Frame 28).
 * Al eliminarlo, retorna al listado inyectando parámetros locales para mostrar el banner de éxito (Frame 33).
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Modal,
  Platform,
  Alert,
  ScrollView,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { ArrowLeft, ArrowRight, Info, AlertTriangle } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService, BeneficiarioDTO } from '../services/assets.service';

interface ActivoAsignado {
  id: number;
  nombre: string;
  tipoString: string;
}

export default function DetalleBeneficiarioScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();
  const { id } = useLocalSearchParams();

  const beneficiarioId = Number(id);

  const [loading, setLoading] = useState(true);
  const [beneficiario, setBeneficiario] = useState<BeneficiarioDTO | null>(null);
  const [activosAsignados, setActivosAsignados] = useState<ActivoAsignado[]>([]);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const loadDatos = async () => {
      try {
        // 1. Obtener detalles del beneficiario
        const bData = await AssetsService.getBeneficiarioPorId(token, beneficiarioId);
        setBeneficiario(bData);

        // 2. Obtener todos los activos del usuario
        const todosLosActivos = await AssetsService.getAssets(token);
        
        // 3. Consultar las asignaciones de cada activo en paralelo
        const asignacionesPromises = todosLosActivos.map(async (activo) => {
          try {
            const asignaciones = await AssetsService.getAssignmentsForAsset(token, activo.id);
            // Filtrar si este beneficiario está asignado al activo
            const tieneAsignacion = asignaciones.some(asig => asig.beneficiarioId === beneficiarioId);
            if (tieneAsignacion) {
              return {
                id: activo.id,
                nombre: activo.nombre,
                tipoString: mapearTipoActivo(activo.tipo),
              };
            }
          } catch (e) {
            console.log('Error fetching assignments for asset', activo.id, e);
          }
          return null;
        });

        const resultados = await Promise.all(asignacionesPromises);
        // Filtrar nulos
        const asignadosValidos = resultados.filter((item): item is ActivoAsignado => item !== null);
        setActivosAsignados(asignadosValidos);

      } catch (err: any) {
        console.log('Error al cargar detalle del beneficiario', err.message);
        Alert.alert('Error de carga', err.message);
        router.back();
      } finally {
        setLoading(false);
      }
    };

    loadDatos();
  }, [token, id]);

  /**
   * Mapea el número del enum TipoActivoDigital a un String descriptivo.
   */
  const mapearTipoActivo = (tipoVal: number): string => {
    switch (tipoVal) {
      case 0: return 'Cuenta Bancaria';
      case 1: return 'Red Social';
      case 2: return 'Cripto';
      case 3: return 'Correo Electrónico';
      default: return 'Archivo';
    }
  };

  /**
   * Genera las iniciales a partir del nombre completo del beneficiario
   */
  const obtenerIniciales = (nombre: string) => {
    const partes = nombre.trim().split(' ');
    if (partes.length >= 2) {
      return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase();
    }
    return nombre.substring(0, 2).toUpperCase();
  };

  /**
   * Determina el estado del beneficiario según la base de datos (1 = Pendiente, 2 = Verificado/Aceptado, 3 = Rechazado)
   * con un fallback basado en nombres semilla para compatibilidad con datos demo.
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
   * Ejecuta el borrado del beneficiario y maneja errores por restricciones (por ej. si tiene asignaciones).
   */
  const handleEliminarBeneficiario = async () => {
    if (!token || !beneficiario) return;

    setDeleting(true);
    try {
      await AssetsService.deleteBeneficiario(token, beneficiario.id);
      setShowConfirmModal(false);
      
      // Redirigir al listado inyectando parámetro de éxito para el banner (Frame 33)
      router.replace({
        pathname: '/(tabs)/beneficiarios',
        params: { deleted: 'true' },
      });
    } catch (err: any) {
      setShowConfirmModal(false);
      Alert.alert(
        'No se puede eliminar',
        'Este beneficiario tiene activos asignados. Primero desvincula o elimina los activos asociados para poder borrarlo.'
      );
    } finally {
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando detalle...</Text>
      </View>
    );
  }

  if (!beneficiario) {
    return (
      <View style={styles.loadingContainer}>
        <Text style={styles.loadingText}>No se encontró el beneficiario.</Text>
      </View>
    );
  }

  const estado = obtenerEstadoBeneficiario(beneficiario);

  return (
    <View style={styles.container}>
      {/* HEADER DE ALTA FIDELIDAD */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.replace('/(tabs)/beneficiarios')} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Detalle de beneficiario</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>
          
          {/* TARJETA DE PERFIL (Frame 9) */}
          <View style={styles.profileCard}>
            <View style={styles.avatarCircle}>
              <Text style={styles.avatarText}>{obtenerIniciales(beneficiario.nombre)}</Text>
            </View>

            <View style={styles.infoGroup}>
              <Text style={styles.infoLabel}>Nombre</Text>
              <Text style={styles.infoValue}>{beneficiario.nombre}</Text>
            </View>

            <View style={styles.infoGroup}>
              <Text style={styles.infoLabel}>Relación</Text>
              <Text style={styles.infoValue}>{beneficiario.parentesco}</Text>
            </View>

            <View style={styles.infoGroup}>
              <Text style={styles.infoLabel}>Estado</Text>
              <View style={[
                styles.statusBadge,
                estado === 'Verificado' && styles.badgeVerified,
                estado === 'Pendiente' && styles.badgePending,
                estado === 'Rechazado' && styles.badgeRejected
              ]}>
                <Text style={[
                  styles.statusBadgeText,
                  estado === 'Verificado' && styles.textVerified,
                  estado === 'Pendiente' && styles.textPending,
                  estado === 'Rechazado' && styles.textRejected
                ]}>
                  {estado}
                </Text>
              </View>
            </View>
          </View>

          {/* SECCIÓN ACTIVOS ASIGNADOS */}
          <Text style={styles.sectionTitle}>ACTIVOS ASIGNADOS</Text>

          {activosAsignados.length === 0 ? (
            <View style={styles.emptyAssetsCard}>
              <Text style={styles.emptyAssetsText}>No posee activos asignados actualmente.</Text>
            </View>
          ) : (
            <View style={styles.assetsListContainer}>
              {activosAsignados.map((activo) => (
                <TouchableOpacity
                  key={activo.id}
                  style={styles.assetCard}
                  activeOpacity={0.8}
                  onPress={() => router.replace({ pathname: '/(tabs)/activos' })}
                >
                  <View style={styles.assetInfo}>
                    <Text style={styles.assetName}>{activo.nombre}</Text>
                    <Text style={styles.assetType}>{activo.tipoString}</Text>
                  </View>
                  <ArrowRight size={20} color="#005B9A" />
                </TouchableOpacity>
              ))}
            </View>
          )}

          {/* BOTÓN ELIMINAR BENEFICIARIO (Frame 9) */}
          <TouchableOpacity
            style={styles.deleteButton}
            onPress={() => setShowConfirmModal(true)}
          >
            <Text style={styles.deleteButtonText}>Eliminar beneficiario</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* MODAL DE CONFIRMACIÓN DE BORRADO (Frame 28) */}
      <Modal
        visible={showConfirmModal}
        transparent
        animationType="fade"
        onRequestClose={() => setShowConfirmModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>¿Estas seguro?</Text>
            <Text style={styles.modalText}>
              Se borraran todos los datos del beneficiario. Esta accion no se puede deshacer.
            </Text>

            <View style={styles.modalButtonsRow}>
              <TouchableOpacity
                style={styles.modalCancelButton}
                onPress={() => setShowConfirmModal(false)}
                disabled={deleting}
              >
                <Text style={styles.modalCancelText}>Cancelar</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.modalConfirmButton}
                onPress={handleEliminarBeneficiario}
                disabled={deleting}
              >
                {deleting ? (
                  <ActivityIndicator size="small" color="#FFFFFF" />
                ) : (
                  <Text style={styles.modalConfirmText}>Eliminar</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo general verde claro pastel
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
  scrollContent: {
    padding: 24,
    paddingBottom: 40,
  },
  centeredWrapper: {
    width: '100%',
    maxWidth: 600,
    alignSelf: 'center',
    gap: 24,
  },
  profileCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 24,
    alignItems: 'center',
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
  avatarCircle: {
    width: 68,
    height: 68,
    borderRadius: 34,
    backgroundColor: '#F8B5B1', // Fondo rosado/rojo claro del mockup (Frame 9)
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 8,
  },
  avatarText: {
    color: '#C53929', // Texto rojo
    fontFamily: 'MPLUS2-Bold',
    fontSize: 24,
  },
  infoGroup: {
    width: '100%',
    alignItems: 'center',
    gap: 4,
  },
  infoLabel: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  infoValue: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#1a2e2e',
    textAlign: 'center',
  },
  statusBadge: {
    borderRadius: 12,
    paddingVertical: 4,
    paddingHorizontal: 16,
    marginTop: 4,
  },
  badgeVerified: {
    backgroundColor: '#C5E2D0', // Verde claro translúcido
  },
  badgePending: {
    backgroundColor: '#FFF9E6', // Naranja claro translúcido
  },
  badgeRejected: {
    backgroundColor: '#FADBD8', // Rojo claro translúcido
  },
  statusBadgeText: {
    fontSize: 13,
    fontFamily: 'MPLUS2-Bold',
  },
  textVerified: {
    color: '#2E7D32',
  },
  textPending: {
    color: '#E2A53C',
  },
  textRejected: {
    color: '#C0392B',
  },
  sectionTitle: {
    fontSize: 14,
    fontFamily: 'MPLUS2-Bold',
    color: '#5E746A',
    letterSpacing: 0.8,
    marginTop: 8,
  },
  emptyAssetsCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 24,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyAssetsText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#8A9E95',
  },
  assetsListContainer: {
    gap: 12,
  },
  assetCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingVertical: 18,
    paddingHorizontal: 20,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  assetInfo: {
    flex: 1,
    gap: 4,
  },
  assetName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  assetType: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  deleteButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1.5,
    borderColor: '#C53929', // Borde rojo (Frame 9)
    height: 52,
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    marginTop: 12,
  },
  deleteButtonText: {
    color: '#C53929',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
  },
  // Confirmación de Borrado Modal (Frame 28)
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(26, 46, 46, 0.6)', // Fondo oscuro translúcido
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  modalContent: {
    backgroundColor: '#FFFFFF',
    borderRadius: 24,
    padding: 24,
    alignItems: 'center',
    width: '100%',
    maxWidth: 340,
    gap: 16,
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 6 },
        shadowOpacity: 0.15,
        shadowRadius: 10,
      },
      android: {
        elevation: 6,
      },
    }),
  },
  modalTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    color: '#1a2e2e',
    textAlign: 'center',
  },
  modalText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#5E746A',
    textAlign: 'center',
    lineHeight: 20,
    paddingHorizontal: 8,
  },
  modalButtonsRow: {
    flexDirection: 'row',
    width: '100%',
    gap: 12,
    marginTop: 8,
  },
  modalCancelButton: {
    flex: 1,
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalCancelText: {
    color: '#5E746A',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
  modalConfirmButton: {
    flex: 1,
    backgroundColor: '#D32F2F', // Fondo rojo sólido (Frame 28)
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalConfirmText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
});
