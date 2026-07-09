/**
 * @file editar-activo.tsx
 * @description Pantalla para la edición y eliminación de un activo digital (Frames 7, 19 y 5).
 * 
 * Permite modificar el nombre, instrucciones del activo, nivel de prioridad y beneficiario asignado,
 * actualizando el activo y recreando su asignación de herencia transaccionalmente en el backend.
 * Incluye el modal de confirmación de eliminación (Frame 19) que redirecciona al listado con un banner de éxito.
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  Modal,
  Platform,
  Alert,
  ScrollView,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { ArrowLeft, ChevronDown } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService, BeneficiarioDTO, AsignacionDTO } from '../services/assets.service';

export default function EditarActivoScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();
  const { id } = useLocalSearchParams();

  const activoId = Number(id);

  // Estados del formulario
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const [nombre, setNombre] = useState('');
  const [tipoVal, setTipoVal] = useState(0);
  const [descripcion, setDescripcion] = useState('');
  const [prioridad, setPrioridad] = useState<'Alta' | 'Media' | 'Baja'>('Media');
  const [selectedBeneficiarioId, setSelectedBeneficiarioId] = useState<number | null>(null);

  // Listas de selección
  const [beneficiarios, setBeneficiarios] = useState<BeneficiarioDTO[]>([]);
  const [asignacionesExistentes, setAsignacionesExistentes] = useState<AsignacionDTO[]>([]);
  
  // Control de dropdowns personalizados inline
  const [showPrioridadDropdown, setShowPrioridadDropdown] = useState(false);
  const [showBeneficiarioDropdown, setShowBeneficiarioDropdown] = useState(false);

  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarDatos = async () => {
      try {
        // 1. Obtener la lista de beneficiarios para el dropdown
        const bList = await AssetsService.getBeneficiarios(token);
        setBeneficiarios(bList);

        // 2. Obtener los activos para rellenar los datos (usamos la lista paginada filtrando por ID)
        const assets = await AssetsService.getAssets(token);
        const activo = assets.find(a => a.id === activoId);

        if (!activo) {
          Alert.alert('Error', 'No se encontró el activo digital solicitado.');
          router.back();
          return;
        }

        setNombre(activo.nombre);
        setTipoVal(activo.tipo);
        setDescripcion(activo.descripcion || '');

        // 3. Obtener asignaciones existentes para este activo
        const asigs = await AssetsService.getAssignmentsForAsset(token, activoId);
        setAsignacionesExistentes(asigs);

        if (asigs.length > 0) {
          const primerAsig = asigs[0];
          setSelectedBeneficiarioId(primerAsig.beneficiarioId);
          
          // Parsear prioridad guardada en condicionLiberacion (ej: "Prioridad: Alta")
          const cond = primerAsig.condicionLiberacion || '';
          if (cond.includes('Alta')) setPrioridad('Alta');
          else if (cond.includes('Baja')) setPrioridad('Baja');
          else setPrioridad('Media');
        }

      } catch (err: any) {
        console.log('Error al cargar datos del activo:', err.message);
        Alert.alert('Error al cargar', 'No se pudieron recuperar los detalles del activo.');
        router.replace('/(tabs)/activos');
      } finally {
        setLoading(false);
      }
    };

    cargarDatos();
  }, [token, id]);

  const mapearTipoString = (tipo: number): string => {
    switch (tipo) {
      case 0: return 'Cuenta bancaria';
      case 1: return 'Red social';
      case 2: return 'Cripto';
      case 3: return 'Correo electrónico';
      default: return 'Archivo / Otro';
    }
  };

  /**
   * Guarda las modificaciones del activo y recrea las asignaciones
   */
  const handleGuardarCambios = async () => {
    if (!token) return;
    if (!nombre.trim()) {
      Alert.alert('Campo requerido', 'El nombre del activo es obligatorio.');
      return;
    }
    if (!selectedBeneficiarioId) {
      Alert.alert('Campo requerido', 'Debes seleccionar un beneficiario.');
      return;
    }

    setSaving(true);
    try {
      // 1. Actualizar el activo principal (PUT)
      await AssetsService.updateAsset(token, activoId, {
        nombre: nombre.trim(),
        tipo: tipoVal,
        descripcion: descripcion.trim(),
      });

      // 2. Eliminar las asignaciones de herencia viejas del activo
      const deletePromises = asignacionesExistentes.map(asig => 
        AssetsService.deleteAssignment(token, asig.id)
      );
      await Promise.all(deletePromises);

      // 3. Crear la nueva asignación para el beneficiario seleccionado
      await AssetsService.createAssignments(token, activoId, [
        {
          beneficiarioId: selectedBeneficiarioId,
          porcentajeAsignado: 100,
          condicionLiberacion: `Prioridad: ${prioridad}`,
        }
      ]);

      Alert.alert('Activo Actualizado', 'Los cambios se han guardado con éxito.', [
        { text: 'OK', onPress: () => router.replace('/(tabs)/activos') }
      ]);

    } catch (err: any) {
      console.log('Error al guardar cambios:', err.message);
      Alert.alert('Error al guardar', err.message);
    } finally {
      setSaving(false);
    }
  };

  /**
   * Elimina el activo permanentemente de la base de datos
   */
  const handleEliminarActivo = async () => {
    if (!token) return;

    setDeleting(true);
    try {
      await AssetsService.deleteAsset(token, activoId);
      setShowDeleteModal(false);
      
      // Redireccionar al listado inyectando deleted=true para ver el banner (Frame 33 style)
      router.replace({
        pathname: '/(tabs)/activos',
        params: { deleted: 'true' },
      });
    } catch (err: any) {
      console.log('Error al eliminar activo:', err.message);
      Alert.alert('Error al eliminar', err.message);
    } finally {
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando activo...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* HEADER DE ALTA FIDELIDAD CON BOTÓN ELIMINAR */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.replace('/(tabs)/activos')} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Editar activo</Text>
          
          <TouchableOpacity
            style={styles.deleteHeaderButton}
            onPress={() => setShowDeleteModal(true)}
          >
            <Text style={styles.deleteHeaderText}>Eliminar</Text>
          </TouchableOpacity>
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>
          
          {/* CAMPO: NOMBRE DEL ACTIVO */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Nombre del activo</Text>
            <TextInput
              style={styles.textInput}
              value={nombre}
              onChangeText={(text) => setNombre(text)}
              placeholder="Ej: Cuenta Santander pesos"
              placeholderTextColor="#8A9E95"
            />
          </View>

          {/* CAMPO: TIPO DE ACTIVO (LECTURA / DESHABILITADO - Frame 7) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Tipo de activo</Text>
            <View style={styles.disabledInput}>
              <Text style={styles.disabledInputText}>{mapearTipoString(tipoVal)}</Text>
            </View>
          </View>

          {/* CAMPO: INSTRUCCIONES / DESCRIPCIÓN */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Instrucciones para el beneficiario</Text>
            <TextInput
              style={[styles.textInput, styles.textArea]}
              value={descripcion}
              onChangeText={(text) => setDescripcion(text)}
              placeholder="Instrucciones para reclamar el activo..."
              placeholderTextColor="#8A9E95"
              multiline
              numberOfLines={4}
            />
          </View>

          {/* CAMPO: NIVEL DE PRIORIDAD (DROPDOWN INLINE) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Nivel de prioridad</Text>
            <TouchableOpacity
              style={styles.dropdownButton}
              activeOpacity={0.8}
              onPress={() => {
                setShowPrioridadDropdown(!showPrioridadDropdown);
                setShowBeneficiarioDropdown(false);
              }}
            >
              <Text style={styles.dropdownButtonText}>{prioridad}</Text>
              <ChevronDown size={20} color="#1a2e2e" />
            </TouchableOpacity>

            {showPrioridadDropdown && (
              <View style={styles.dropdownInlineList}>
                {(['Alta', 'Media', 'Baja'] as const).map((p) => (
                  <TouchableOpacity
                    key={p}
                    style={[
                      styles.dropdownInlineOption,
                      prioridad === p && styles.dropdownInlineOptionSelected,
                    ]}
                    onPress={() => {
                      setPrioridad(p);
                      setShowPrioridadDropdown(false);
                    }}
                  >
                    <Text
                      style={[
                        styles.dropdownInlineOptionText,
                        prioridad === p && styles.dropdownInlineOptionTextSelected,
                      ]}
                    >
                      {p}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </View>

          {/* CAMPO: SELECCIONAR BENEFICIARIO (DROPDOWN INLINE) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Seleccionar relacion</Text>
            <TouchableOpacity
              style={styles.dropdownButton}
              activeOpacity={0.8}
              onPress={() => {
                setShowBeneficiarioDropdown(!showBeneficiarioDropdown);
                setShowPrioridadDropdown(false);
              }}
            >
              <Text style={styles.dropdownButtonText}>
                {selectedBeneficiarioId
                  ? beneficiarios.find((b) => b.id === selectedBeneficiarioId)?.nombre || 'Seleccionar'
                  : 'Seleccionar'}
              </Text>
              <ChevronDown size={20} color="#1a2e2e" />
            </TouchableOpacity>

            {showBeneficiarioDropdown && (
              <View style={styles.dropdownInlineList}>
                {beneficiarios.length === 0 ? (
                  <View style={styles.dropdownEmptyContainer}>
                    <Text style={styles.dropdownEmptyText}>No tenés beneficiarios agregados.</Text>
                  </View>
                ) : (
                  beneficiarios.map((b) => (
                    <TouchableOpacity
                      key={b.id}
                      style={[
                        styles.dropdownInlineOption,
                        selectedBeneficiarioId === b.id && styles.dropdownInlineOptionSelected,
                      ]}
                      onPress={() => {
                        setSelectedBeneficiarioId(b.id);
                        setShowBeneficiarioDropdown(false);
                      }}
                    >
                      <Text
                        style={[
                          styles.dropdownInlineOptionText,
                          selectedBeneficiarioId === b.id && styles.dropdownInlineOptionTextSelected,
                        ]}
                      >
                        {b.nombre} ({b.parentesco})
                      </Text>
                    </TouchableOpacity>
                  ))
                )}
              </View>
            )}
          </View>

          {/* BOTÓN GUARDAR CAMBIOS */}
          <TouchableOpacity
            style={styles.saveButton}
            onPress={handleGuardarCambios}
            disabled={saving}
          >
            {saving ? (
              <ActivityIndicator size="small" color="#FFFFFF" />
            ) : (
              <Text style={styles.saveButtonText}>Guardar cambios</Text>
            )}
          </TouchableOpacity>

        </View>
      </ScrollView>

      {/* MODAL DE CONFIRMACIÓN DE BORRADO (Frame 19) */}
      <Modal
        visible={showDeleteModal}
        transparent
        animationType="fade"
        onRequestClose={() => setShowDeleteModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>¿Estas seguro?</Text>
            <Text style={styles.modalText}>
              Se borraran todos los datos del activo. Esta accion no se puede deshacer.
            </Text>

            <View style={styles.modalButtonsRow}>
              <TouchableOpacity
                style={styles.modalCancelButton}
                onPress={() => setShowDeleteModal(false)}
                disabled={deleting}
              >
                <Text style={styles.modalCancelText}>Cancelar</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.modalConfirmButton}
                onPress={handleEliminarActivo}
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
  deleteHeaderButton: {
    backgroundColor: '#D32F2F', // Fondo rojo botón Eliminar header (Frame 7)
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 16,
  },
  deleteHeaderText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 12,
  },
  scrollContent: {
    padding: 24,
    paddingBottom: 40,
  },
  centeredWrapper: {
    width: '100%',
    maxWidth: 600,
    alignSelf: 'center',
    gap: 20,
  },
  inputGroup: {
    width: '100%',
    gap: 8,
  },
  inputLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#5E746A',
  },
  textInput: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    height: 48,
    paddingHorizontal: 16,
    color: '#1a2e2e',
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
  },
  textArea: {
    height: 100,
    paddingVertical: 12,
    textAlignVertical: 'top', // Para Android multiline
  },
  disabledInput: {
    backgroundColor: '#CCD3CE', // Fondo gris de input deshabilitado (Frame 7)
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#BCC2BE',
    height: 48,
    paddingHorizontal: 16,
    justifyContent: 'center',
  },
  disabledInputText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
    color: '#5E746A',
  },
  dropdownButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    height: 48,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  dropdownButtonText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
    color: '#1a2e2e',
  },
  dropdownInlineList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 6,
    gap: 2,
    maxHeight: 180,
  },
  dropdownInlineOption: {
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  dropdownInlineOptionSelected: {
    backgroundColor: '#DAF8BD',
  },
  dropdownInlineOptionText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#1a2e2e',
  },
  dropdownInlineOptionTextSelected: {
    fontFamily: 'MPLUS2-Bold',
    color: '#2E7D32',
  },
  dropdownEmptyContainer: {
    padding: 12,
    alignItems: 'center',
  },
  dropdownEmptyText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  saveButton: {
    backgroundColor: '#39C55C', // Verde guardar cambios mockup (Frame 7)
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    marginTop: 12,
  },
  saveButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
  },
  // Confirmación de Borrado Modal (Frame 19)
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
    backgroundColor: '#D32F2F', // Fondo rojo sólido (Frame 19)
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
