/**
 * @file admin/certificados.tsx
 * @description Panel de Administrador para revisar certificados de defunción pendientes:
 * listarlos, ver el archivo adjunto, aprobarlos (libera todos los bienes del titular) o
 * rechazarlos con un motivo. Los 3 endpoints ya exigen `[Authorize(Roles = "Administrador")]`
 * en el backend; el chequeo de rol acá (vía AuthContext) es solo UX, no el control real.
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  FlatList,
  RefreshControl,
  Modal,
  TextInput,
  Alert,
  Platform,
} from 'react-native';
import { useRouter, useFocusEffect } from 'expo-router';
import { ArrowLeft, ShieldCheck, FileText, Eye } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
// API "legacy" de expo-file-system: la nueva API basada en clases (File/Directory) rechazaba
// la descarga con un error genérico y, a diferencia de esta, no expone el status HTTP real.
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import * as SecureStore from 'expo-secure-store';
import { useAuth } from '../../context/AuthContext';
import { TOKEN_KEY } from '../../services/api';
import { API_BASE_URL } from '../../constants/api';
import { CertificadosService, CertificadoDefuncionDTO } from '../../services/certificados.service';

export default function AdminCertificadosScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { userRole } = useAuth();

  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [pendientes, setPendientes] = useState<CertificadoDefuncionDTO[]>([]);
  const [procesandoId, setProcesandoId] = useState<number | null>(null);
  const [viendoArchivoId, setViendoArchivoId] = useState<number | null>(null);

  // Modal de rechazo: Alert.prompt (con input de texto) solo existe en iOS, así que se
  // arma un modal propio con TextInput para pedir el motivo en cualquier plataforma.
  const [certificadoARechazar, setCertificadoARechazar] = useState<CertificadoDefuncionDTO | null>(null);
  const [motivoRechazo, setMotivoRechazo] = useState('');

  const fetchPendientes = useCallback(async () => {
    if (userRole !== 'Administrador') {
      Alert.alert('Sin permiso', 'Esta sección es exclusiva para administradores.');
      router.replace('/');
      return;
    }
    try {
      const data = await CertificadosService.obtenerPendientes();
      setPendientes(data);
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudieron cargar los certificados pendientes.');
    } finally {
      setLoading(false);
    }
  }, [userRole]);

  useFocusEffect(
    useCallback(() => {
      fetchPendientes();
    }, [fetchPendientes])
  );

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await fetchPendientes();
    setRefreshing(false);
  }, [fetchPendientes]);

  /**
   * Descarga el certificado (con el JWT del admin, ya que el endpoint está protegido) y
   * abre el selector nativo "Abrir con..." para visualizarlo, en vez de dejar que se
   * apruebe o rechace a ciegas sin ver el documento real.
   */
  const handleVerArchivo = async (certificado: CertificadoDefuncionDTO) => {
    if (!FileSystem.cacheDirectory) {
      Alert.alert('No disponible', 'Este dispositivo no tiene una carpeta de caché accesible.');
      return;
    }

    setViendoArchivoId(certificado.id);
    try {
      const token = await SecureStore.getItemAsync(TOKEN_KEY);
      const destino = `${FileSystem.cacheDirectory}${certificado.nombreArchivoOriginal}`;
      const resultado = await FileSystem.downloadAsync(
        `${API_BASE_URL}/certificados-defuncion/${certificado.id}/archivo`,
        destino,
        { headers: token ? { Authorization: `Bearer ${token}` } : {} }
      );

      // downloadAsync guarda el body tal cual venga aunque el servidor haya respondido un
      // error (ej: JSON de "{ mensaje: ... }" en vez del archivo): hay que validar el status.
      if (resultado.status !== 200) {
        Alert.alert('Error', `El servidor respondió con un error (${resultado.status}) al pedir el archivo.`);
        return;
      }

      if (await Sharing.isAvailableAsync()) {
        await Sharing.shareAsync(resultado.uri);
      } else {
        Alert.alert('No disponible', 'Este dispositivo no puede abrir archivos.');
      }
    } catch (err: any) {
      console.log('Error al abrir el archivo del certificado:', err.message);
      Alert.alert('Error', 'No se pudo abrir el archivo del certificado.');
    } finally {
      setViendoArchivoId(null);
    }
  };

  const handleAprobar = (certificado: CertificadoDefuncionDTO) => {
    Alert.alert(
      'Aprobar certificado',
      `Al aprobar, se liberarán TODOS los bienes aceptados de ${certificado.usuarioTitularNombre}. Esta acción no se puede deshacer.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Aprobar',
          onPress: async () => {
            setProcesandoId(certificado.id);
            try {
              await CertificadosService.aprobar(certificado.id);
              setPendientes((prev) => prev.filter((c) => c.id !== certificado.id));
            } catch (err: any) {
              Alert.alert('Error', err.message || 'No se pudo aprobar el certificado.');
            } finally {
              setProcesandoId(null);
            }
          },
        },
      ]
    );
  };

  const handleConfirmarRechazo = async () => {
    if (!certificadoARechazar) return;
    if (!motivoRechazo.trim()) {
      Alert.alert('Motivo requerido', 'El motivo del rechazo es obligatorio.');
      return;
    }

    setProcesandoId(certificadoARechazar.id);
    try {
      await CertificadosService.rechazar(certificadoARechazar.id, motivoRechazo.trim());
      setPendientes((prev) => prev.filter((c) => c.id !== certificadoARechazar.id));
      setCertificadoARechazar(null);
      setMotivoRechazo('');
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo rechazar el certificado.');
    } finally {
      setProcesandoId(null);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando panel...</Text>
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
          <Text style={styles.headerTitle}>Certificados pendientes</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <View style={styles.content}>
        {pendientes.length === 0 ? (
          <View style={styles.emptyCard}>
            <ShieldCheck size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyTitle}>Sin certificados pendientes</Text>
            <Text style={styles.emptyText}>No hay ningún certificado esperando revisión en este momento.</Text>
          </View>
        ) : (
          <FlatList
            data={pendientes}
            keyExtractor={(item) => String(item.id)}
            contentContainerStyle={styles.listContent}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#23856C']} tintColor="#23856C" />
            }
            renderItem={({ item }) => (
              <View style={styles.card}>
                <View style={styles.cardHeaderRow}>
                  <FileText size={20} color="#23856C" />
                  <Text style={styles.cardFileName} numberOfLines={1}>{item.nombreArchivoOriginal}</Text>
                </View>
                <Text style={styles.cardLine}>
                  Titular: <Text style={styles.cardBold}>{item.usuarioTitularNombre}</Text>
                </Text>
                <Text style={styles.cardLine}>
                  Subido por: <Text style={styles.cardBold}>{item.subidoPorNombre}</Text>
                </Text>
                <Text style={styles.cardLine}>
                  Fecha: {new Date(item.fechaSubida).toLocaleDateString('es-AR')}
                </Text>

                <TouchableOpacity
                  style={styles.viewFileButton}
                  onPress={() => handleVerArchivo(item)}
                  disabled={viendoArchivoId === item.id}
                >
                  {viendoArchivoId === item.id ? (
                    <ActivityIndicator size="small" color="#005B9A" />
                  ) : (
                    <>
                      <Eye size={16} color="#005B9A" />
                      <Text style={styles.viewFileButtonText}>Ver archivo</Text>
                    </>
                  )}
                </TouchableOpacity>

                {procesandoId === item.id ? (
                  <ActivityIndicator size="small" color="#23856C" style={{ marginTop: 12 }} />
                ) : (
                  <View style={styles.actionsRow}>
                    <TouchableOpacity style={styles.approveButton} onPress={() => handleAprobar(item)}>
                      <Text style={styles.approveButtonText}>Aprobar</Text>
                    </TouchableOpacity>
                    <TouchableOpacity
                      style={styles.rejectButton}
                      onPress={() => {
                        setCertificadoARechazar(item);
                        setMotivoRechazo('');
                      }}
                    >
                      <Text style={styles.rejectButtonText}>Rechazar</Text>
                    </TouchableOpacity>
                  </View>
                )}
              </View>
            )}
          />
        )}
      </View>

      {/* MODAL: pedir motivo de rechazo (Alert.prompt no existe en Android) */}
      <Modal
        visible={certificadoARechazar !== null}
        transparent
        animationType="fade"
        onRequestClose={() => setCertificadoARechazar(null)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Motivo del rechazo</Text>
            <Text style={styles.modalSubtitle}>
              Certificado de {certificadoARechazar?.usuarioTitularNombre}
            </Text>
            <TextInput
              style={styles.modalInput}
              placeholder="Ej: el documento no es legible"
              placeholderTextColor="#8A9E95"
              value={motivoRechazo}
              onChangeText={setMotivoRechazo}
              multiline
            />
            <View style={styles.modalButtonsRow}>
              <TouchableOpacity
                style={styles.modalCancelButton}
                onPress={() => setCertificadoARechazar(null)}
              >
                <Text style={styles.modalCancelText}>Cancelar</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.modalConfirmButton} onPress={handleConfirmarRechazo}>
                <Text style={styles.modalConfirmText}>Confirmar rechazo</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#DAF8BD' },
  loadingContainer: { flex: 1, backgroundColor: '#DAF8BD', alignItems: 'center', justifyContent: 'center' },
  loadingText: { fontFamily: 'MPLUS2-Regular', fontSize: 16, color: '#23856C', marginTop: 12 },
  header: { paddingHorizontal: 20, paddingBottom: 20, borderBottomLeftRadius: 20, borderBottomRightRadius: 20 },
  headerContent: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  backButton: { padding: 4 },
  headerTitle: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 18, textAlign: 'center' },
  content: { flex: 1, padding: 20 },
  listContent: { gap: 16 },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 16,
    gap: 6,
    ...Platform.select({
      ios: { shadowColor: '#1a2e2e', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.08, shadowRadius: 6 },
      android: { elevation: 3 },
    }),
  },
  cardHeaderRow: { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 4 },
  cardFileName: { fontFamily: 'MPLUS2-Bold', fontSize: 15, color: '#1a2e2e', flex: 1 },
  cardLine: { fontFamily: 'MPLUS2-Regular', fontSize: 13, color: '#5E746A' },
  cardBold: { fontFamily: 'MPLUS2-Bold', color: '#1a2e2e' },
  viewFileButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    borderWidth: 1.5,
    borderColor: '#005B9A',
    borderRadius: 10,
    paddingVertical: 8,
    marginTop: 10,
  },
  viewFileButtonText: { color: '#005B9A', fontFamily: 'MPLUS2-Bold', fontSize: 13 },
  actionsRow: { flexDirection: 'row', gap: 10, marginTop: 12 },
  approveButton: { flex: 1, backgroundColor: '#2E7D32', borderRadius: 10, paddingVertical: 10, alignItems: 'center' },
  approveButtonText: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 14 },
  rejectButton: {
    flex: 1,
    backgroundColor: '#FFFFFF',
    borderWidth: 1.5,
    borderColor: '#D32F2F',
    borderRadius: 10,
    paddingVertical: 10,
    alignItems: 'center',
  },
  rejectButtonText: { color: '#D32F2F', fontFamily: 'MPLUS2-Bold', fontSize: 14 },
  emptyCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingVertical: 40,
    paddingHorizontal: 24,
    alignItems: 'center',
    gap: 12,
    marginTop: 40,
  },
  emptyTitle: { fontFamily: 'MPLUS2-Bold', fontSize: 17, color: '#1a2e2e' },
  emptyText: { fontFamily: 'MPLUS2-Regular', fontSize: 14, color: '#8A9E95', textAlign: 'center' },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(26, 46, 46, 0.6)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  modalContent: {
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    padding: 24,
    width: '100%',
    maxWidth: 380,
    gap: 12,
  },
  modalTitle: { fontFamily: 'MPLUS2-Bold', fontSize: 18, color: '#1a2e2e' },
  modalSubtitle: { fontFamily: 'MPLUS2-Regular', fontSize: 13, color: '#8A9E95', marginBottom: 4 },
  modalInput: {
    backgroundColor: '#FAFAFA',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    padding: 12,
    minHeight: 80,
    textAlignVertical: 'top',
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#1a2e2e',
  },
  modalButtonsRow: { flexDirection: 'row', gap: 12, marginTop: 8 },
  modalCancelButton: {
    flex: 1,
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    height: 46,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalCancelText: { color: '#5E746A', fontFamily: 'MPLUS2-Bold', fontSize: 14 },
  modalConfirmButton: {
    flex: 1,
    backgroundColor: '#D32F2F',
    borderRadius: 12,
    height: 46,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalConfirmText: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 14 },
});
