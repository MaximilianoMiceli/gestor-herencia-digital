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
  TextInput,
  ActivityIndicator,
  FlatList,
  RefreshControl,
  Modal,
  ScrollView,
  Platform,
  Alert,
} from 'react-native';
import { useRouter, useFocusEffect } from 'expo-router';
import { ArrowLeft, Info, HelpCircle, Search } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
// API "legacy" de expo-file-system (más estable que la nueva basada en File/Directory,
// ver el mismo criterio en admin/certificados.tsx).
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import * as SecureStore from 'expo-secure-store';
import { useAuth } from '../context/AuthContext';
import { TOKEN_KEY } from '../services/api';
import { API_BASE_URL } from '../constants/api';
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
  const [refreshing, setRefreshing] = useState(false);
  const [herencias, setHerencias] = useState<MiHerenciaDTO[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  // Id de la asignación que tiene una acción (Aceptar/Rechazar) en curso, para
  // deshabilitar SOLO esos botones puntuales y no toda la pantalla mientras responde.
  const [procesandoId, setProcesandoId] = useState<number | null>(null);
  // Activo cuya "información" (instrucciones/credenciales reales) se está mostrando en
  // el modal de detalle: solo se puede abrir cuando la asignación ya está disponible
  // (ver el comentario de MiHerenciaDTO.descripcion en assets.service.ts).
  const [activoAVer, setActivoAVer] = useState<MiHerenciaDTO | null>(null);
  const [descargandoArchivo, setDescargandoArchivo] = useState(false);

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

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchHerencias();
    setRefreshing(false);
  };

  /**
   * Agrupa las asignaciones por titular emisor para consolidar la tarjeta del Frame 24,
   * conservando cada ítem individual (con su propio estado) para poder actuar sobre él.
   */
  const obtenerHerenciasAgrupadas = (): HerenciaAgrupada[] => {
    const agrupado: { [key: string]: HerenciaAgrupada } = {};

    // Filtro local por titular o nombre de activo (mismo criterio que activos.tsx): no
    // hace falta un endpoint de búsqueda propio, ya se trae la lista completa.
    const query = searchQuery.trim().toLowerCase();
    const herenciasFiltradas = query.length === 0
      ? herencias
      : herencias.filter(
          (h) => h.titularNombre.toLowerCase().includes(query) || h.activoNombre.toLowerCase().includes(query)
        );

    herenciasFiltradas.forEach((item) => {
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

  const mapearTipoActivo = (tipoVal: number): string => {
    switch (tipoVal) {
      case 0: return 'Cuenta bancaria';
      case 1: return 'Red social';
      case 2: return 'Cripto';
      case 3: return 'Correo electrónico';
      default: return 'Archivo / Otro';
    }
  };

  /**
   * Descarga el archivo adjunto del activo heredado (con el JWT del beneficiario como
   * header, ya que GET /{id}/archivo exige ser el titular o un heredero Aceptado con el
   * bien ya liberado) y abre el selector nativo "Abrir con..." para visualizarlo.
   */
  const handleDescargarArchivo = async (activo: MiHerenciaDTO) => {
    if (!activo.nombreArchivoOriginal) return;

    if (!FileSystem.cacheDirectory) {
      Alert.alert('No disponible', 'Este dispositivo no tiene una carpeta de caché accesible.');
      return;
    }

    setDescargandoArchivo(true);
    try {
      const token = await SecureStore.getItemAsync(TOKEN_KEY);
      const destino = `${FileSystem.cacheDirectory}${activo.nombreArchivoOriginal}`;
      const resultado = await FileSystem.downloadAsync(
        `${API_BASE_URL}/activosdigitales/${activo.activoDigitalId}/archivo`,
        destino,
        { headers: token ? { Authorization: `Bearer ${token}` } : {} }
      );

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
      console.log('Error al descargar el archivo del activo:', err.message);
      Alert.alert('Error', 'No se pudo descargar el archivo del activo.');
    } finally {
      setDescargandoArchivo(false);
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
        {herencias.length > 0 && (
          <View style={styles.searchContainer}>
            <Search size={18} color="#8A9E95" style={styles.searchIcon} />
            <TextInput
              style={styles.searchInput}
              placeholder="Buscar por titular o activo"
              placeholderTextColor="#8A9E95"
              value={searchQuery}
              onChangeText={setSearchQuery}
              autoCapitalize="none"
            />
          </View>
        )}

        {herencias.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin asignaciones aún</Text>
            <Text style={styles.emptyStateText}>
              Aún ningún usuario te ha designado como beneficiario de sus activos digitales.
            </Text>
          </View>
        ) : herenciasAgrupadas.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin resultados</Text>
            <Text style={styles.emptyStateText}>Ninguna herencia coincide con la búsqueda.</Text>
          </View>
        ) : (
          <FlatList
            data={herenciasAgrupadas}
            keyExtractor={(item) => item.titular}
            contentContainerStyle={styles.listContent}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#23856C']} tintColor="#23856C" />
            }
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

                    {/* Solo aparece cuando el bien ya está realmente liberado: antes de
                        eso el backend ni siquiera manda la descripción (ver
                        MiHerenciaDTO.descripcion), así que no habría nada que mostrar. */}
                    {asig.estado === 'Aceptado' && asig.disponible && (
                      <TouchableOpacity style={styles.viewInfoButton} onPress={() => setActivoAVer(asig)}>
                        <Text style={styles.viewInfoButtonText}>Ver información</Text>
                      </TouchableOpacity>
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

      {/* MODAL: información real del activo ya liberado (instrucciones, credenciales,
          archivo adjunto si tiene). Antes de esto, un heredero que ya había aceptado la
          herencia no tenía ninguna forma de ver el contenido, aunque figurara
          "Disponible". */}
      <Modal
        visible={activoAVer !== null}
        transparent
        animationType="fade"
        onRequestClose={() => setActivoAVer(null)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>{activoAVer?.activoNombre}</Text>
            <Text style={styles.modalSubtitle}>
              {activoAVer ? mapearTipoActivo(activoAVer.activoTipo) : ''} · de {activoAVer?.titularNombre}
            </Text>

            <ScrollView style={styles.modalScroll}>
              <Text style={styles.modalDescripcion}>
                {activoAVer?.descripcion || 'Este activo no tiene instrucciones cargadas.'}
              </Text>
              {activoAVer?.nombreArchivoOriginal && (
                <TouchableOpacity
                  style={styles.modalArchivoButton}
                  onPress={() => activoAVer && handleDescargarArchivo(activoAVer)}
                  disabled={descargandoArchivo}
                >
                  {descargandoArchivo ? (
                    <ActivityIndicator size="small" color="#02213D" />
                  ) : (
                    <Text style={styles.modalArchivo}>
                      📎 Descargar archivo adjunto: {activoAVer.nombreArchivoOriginal}
                    </Text>
                  )}
                </TouchableOpacity>
              )}
            </ScrollView>

            <TouchableOpacity style={styles.modalCloseButton} onPress={() => setActivoAVer(null)}>
              <Text style={styles.modalCloseButtonText}>Cerrar</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
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
  searchContainer: {
    backgroundColor: '#E6EAE7',
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    height: 48,
    marginBottom: 16,
  },
  searchIcon: {
    marginRight: 8,
  },
  searchInput: {
    flex: 1,
    color: '#1a2e2e',
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    padding: 0,
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
  viewInfoButton: {
    backgroundColor: '#02213D',
    borderRadius: 8,
    paddingVertical: 9,
    alignItems: 'center',
    marginTop: 4,
  },
  viewInfoButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
  },
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
    maxWidth: 420,
    maxHeight: '75%',
    gap: 8,
  },
  modalTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#1a2e2e',
  },
  modalSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
    marginBottom: 8,
  },
  modalScroll: {
    marginBottom: 12,
  },
  modalDescripcion: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#1a2e2e',
    lineHeight: 21,
  },
  modalArchivoButton: {
    backgroundColor: '#EEFDE2',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 10,
    paddingVertical: 12,
    paddingHorizontal: 12,
    alignItems: 'center',
    marginTop: 12,
  },
  modalArchivo: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    color: '#02213D',
    textAlign: 'center',
  },
  modalCloseButton: {
    backgroundColor: '#EEFDE2',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    height: 46,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalCloseButtonText: {
    color: '#2E7D32',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
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
