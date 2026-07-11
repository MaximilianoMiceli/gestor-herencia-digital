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
import { ArrowLeft, ChevronDown, ChevronUp, Paperclip } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import * as DocumentPicker from 'expo-document-picker';
import { useAuth } from '../context/AuthContext';
import { AssetsService, AsignacionDTO } from '../services/assets.service';

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const TIPOS_MIME_PERMITIDOS = ['application/pdf', 'image/jpeg', 'image/png'];
type ArchivoSeleccionado = { uri: string; name: string; mimeType: string };
const TIPO_CUENTAS = ['Caja de ahorro', 'Cuenta corriente'];

// Mismos formatos de texto que nuevo-activo.tsx usa para serializar la "descripcion":
// se parsean acá para poder mostrar y editar los campos estructurados por separado, en
// vez de exponer el blob completo (ej: "[CRIPTO] Blockchain: X | Wallet: Y...") como un
// único TextInput de texto libre.
const REGEX_CRIPTO = /^\[CRIPTO\] Blockchain: (.*?) \| Wallet: (.*?) \| Clave Privada: (.*?)\n\nInstrucciones:\n([\s\S]*)$/;
const REGEX_BANCO = /^\[BANCO\] Banco: (.*?) \| Cuenta: (.*?) \| CBU\/Alias: (.*?) \| Tipo: (.*?)\n\nInstrucciones:\n([\s\S]*)$/;
const REGEX_RED_SOCIAL = /^\[RED SOCIAL\] Plataforma: (.*?) \| Usuario: (.*?)\n\nInstrucciones:\n([\s\S]*)$/;
const REGEX_CORREO = /^\[CORREO\] Proveedor: (.*?) \| Dirección: (.*?)\n\nInstrucciones:\n([\s\S]*)$/;
const REGEX_ARCHIVO = /^\[ARCHIVO\] Adjunto: .*?\n\nInstrucciones:\n([\s\S]*)$/;

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
  const [beneficiarioEmail, setBeneficiarioEmail] = useState('');
  const [emailError, setEmailError] = useState(false);

  // Nombre del archivo YA adjunto al activo (llega del backend), y el archivo NUEVO
  // que el usuario eventualmente elige para reemplazarlo (todavía no subido).
  const [nombreArchivoActual, setNombreArchivoActual] = useState<string | null>(null);
  const [archivoSeleccionado, setArchivoSeleccionado] = useState<ArchivoSeleccionado | null>(null);
  const [loadingArchivo, setLoadingArchivo] = useState(false);

  const [asignacionesExistentes, setAsignacionesExistentes] = useState<AsignacionDTO[]>([]);

  // Campos estructurados por tipo de activo (mismos que nuevo-activo.tsx): se rellenan
  // parseando la "descripcion" guardada al cargar el activo (ver cargarDatos) y se
  // vuelven a serializar al mismo formato al guardar (ver handleGuardarCambios).
  const [blockchain, setBlockchain] = useState('');
  const [wallet, setWallet] = useState('');
  const [clavePrivada, setClavePrivada] = useState('');
  const [banco, setBanco] = useState('');
  const [numeroCuenta, setNumeroCuenta] = useState('');
  const [cbuAlias, setCbuAlias] = useState('');
  const [tipoCuenta, setTipoCuenta] = useState('Caja de ahorro');
  const [tipoCuentaExpanded, setTipoCuentaExpanded] = useState(false);
  const [plataformaRed, setPlataformaRed] = useState('');
  const [usuarioRed, setUsuarioRed] = useState('');
  const [proveedorCorreo, setProveedorCorreo] = useState('');
  const [direccionCorreo, setDireccionCorreo] = useState('');

  // Control de dropdowns personalizados inline
  const [showPrioridadDropdown, setShowPrioridadDropdown] = useState(false);

  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarDatos = async () => {
      try {
        // 1. Obtener los activos para rellenar los datos (usamos la lista paginada filtrando por ID)
        const assets = await AssetsService.getAssets();
        const activo = assets.find(a => a.id === activoId);

        if (!activo) {
          Alert.alert('Error', 'No se encontró el activo digital solicitado.');
          router.back();
          return;
        }

        setNombre(activo.nombre);
        setTipoVal(activo.tipo);
        setNombreArchivoActual(activo.nombreArchivoOriginal);

        // Parsear la descripción guardada para separar los campos estructurados del
        // texto libre de instrucciones. Si no matchea el formato esperado (ej: un activo
        // viejo o de datos semilla, con una descripción en texto plano), se deja todo
        // el contenido en "instrucciones" y los campos estructurados quedan vacíos:
        // se completan recién en el próximo guardado, sin perder lo que ya había.
        const raw = activo.descripcion || '';
        switch (activo.tipo) {
          case 2: { // Cripto
            const m = raw.match(REGEX_CRIPTO);
            if (m) {
              setBlockchain(m[1]);
              setWallet(m[2]);
              setClavePrivada(m[3]);
              setDescripcion(m[4]);
            } else {
              setDescripcion(raw);
            }
            break;
          }
          case 0: { // Cuenta bancaria
            const m = raw.match(REGEX_BANCO);
            if (m) {
              setBanco(m[1]);
              setNumeroCuenta(m[2]);
              setCbuAlias(m[3]);
              setTipoCuenta(TIPO_CUENTAS.includes(m[4]) ? m[4] : 'Caja de ahorro');
              setDescripcion(m[5]);
            } else {
              setDescripcion(raw);
            }
            break;
          }
          case 1: { // Red social
            const m = raw.match(REGEX_RED_SOCIAL);
            if (m) {
              setPlataformaRed(m[1]);
              setUsuarioRed(m[2]);
              setDescripcion(m[3]);
            } else {
              setDescripcion(raw);
            }
            break;
          }
          case 3: { // Correo electrónico
            const m = raw.match(REGEX_CORREO);
            if (m) {
              setProveedorCorreo(m[1]);
              setDireccionCorreo(m[2]);
              setDescripcion(m[3]);
            } else {
              setDescripcion(raw);
            }
            break;
          }
          case 4: { // Archivo
            const m = raw.match(REGEX_ARCHIVO);
            setDescripcion(m ? m[1] : raw);
            break;
          }
          default:
            setDescripcion(raw);
        }

        // 2. Obtener asignaciones existentes para este activo
        const asigs = await AssetsService.getAssignmentsForAsset(activoId);
        setAsignacionesExistentes(asigs);

        if (asigs.length > 0) {
          const primerAsig = asigs[0];
          setBeneficiarioEmail(primerAsig.emailInvitado);

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

  /**
   * Abre el selector de archivos nativo para reemplazar el archivo adjunto del
   * activo (mismo flujo que "Nuevo activo"). El archivo elegido recién se sube al
   * confirmar "Guardar cambios" (ver handleGuardarCambios).
   */
  const handleAttachFile = async () => {
    setLoadingArchivo(true);
    try {
      const resultado = await DocumentPicker.getDocumentAsync({
        type: TIPOS_MIME_PERMITIDOS,
        copyToCacheDirectory: true,
      });

      if (resultado.canceled || resultado.assets.length === 0) {
        return;
      }

      const archivo = resultado.assets[0];
      setArchivoSeleccionado({
        uri: archivo.uri,
        name: archivo.name,
        mimeType: archivo.mimeType ?? 'application/octet-stream',
      });
    } catch (err) {
      console.error('Error al seleccionar el archivo:', err);
      Alert.alert('Error', 'No se pudo abrir el selector de archivos.');
    } finally {
      setLoadingArchivo(false);
    }
  };

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
   * Guarda las modificaciones del activo y recrea las asignaciones de herencia.
   * 
   * ¿Por qué recreamos las asignaciones en lugar de actualizarlas?:
   * El backend expone endpoints separados para actualizar los datos base del activo (Nombre, Tipo, 
   * Descripcion en PUT /api/activosdigitales/{id}) y para gestionar las asignaciones (POST 
   * /api/activosdigitales/{id}/asignaciones y DELETE /api/asignaciones/{asigId}).
   * Dado que la aplicación móvil simplifica el modelo de herencia asignando el 100% de un activo 
   * a un único beneficiario, la forma más limpia y transaccional de re-asignar un activo sin alterar 
   * el backend es:
   * 1. Actualizar los datos del activo.
   * 2. Eliminar todas las asignaciones previas asociadas a su ID.
   * 3. Crear una nueva asignación limpia vinculando al beneficiario seleccionado.
   * Esto previene colisiones con la validación de negocio del backend que prohíbe superar el 100% 
   * acumulado de herencia para un mismo activo.
   */
  const handleGuardarCambios = async () => {
    if (!token) return;
    setEmailError(false);

    if (!nombre.trim()) {
      Alert.alert('Campo requerido', 'El nombre del activo es obligatorio.');
      return;
    }
    if (!beneficiarioEmail.trim()) {
      Alert.alert('Campo requerido', 'Debes ingresar el email del beneficiario.');
      return;
    }
    if (!EMAIL_REGEX.test(beneficiarioEmail.trim())) {
      setEmailError(true);
      return;
    }

    setSaving(true);
    try {
      // Reconstruye la descripción en el MISMO formato con el que nuevo-activo.tsx la
      // serializa originalmente, ahora con los campos estructurados que se acaban de
      // editar (en vez de reenviar el blob de texto plano tal cual llegó).
      let descripcionFinal = descripcion.trim();
      if (tipoVal === 2) {
        descripcionFinal = `[CRIPTO] Blockchain: ${blockchain} | Wallet: ${wallet} | Clave Privada: ${clavePrivada}\n\nInstrucciones:\n${descripcion}`;
      } else if (tipoVal === 0) {
        descripcionFinal = `[BANCO] Banco: ${banco} | Cuenta: ${numeroCuenta} | CBU/Alias: ${cbuAlias} | Tipo: ${tipoCuenta}\n\nInstrucciones:\n${descripcion}`;
      } else if (tipoVal === 1) {
        descripcionFinal = `[RED SOCIAL] Plataforma: ${plataformaRed} | Usuario: ${usuarioRed}\n\nInstrucciones:\n${descripcion}`;
      } else if (tipoVal === 3) {
        descripcionFinal = `[CORREO] Proveedor: ${proveedorCorreo} | Dirección: ${direccionCorreo}\n\nInstrucciones:\n${descripcion}`;
      } else if (tipoVal === 4) {
        const nombreArchivo = archivoSeleccionado?.name ?? nombreArchivoActual ?? 'Sin archivo';
        descripcionFinal = `[ARCHIVO] Adjunto: ${nombreArchivo}\n\nInstrucciones:\n${descripcion}`;
      }

      // 1. Actualizar el activo principal (PUT)
      await AssetsService.updateAsset(activoId, {
        nombre: nombre.trim(),
        tipo: tipoVal,
        descripcion: descripcionFinal,
      });

      // 2. Eliminar las asignaciones de herencia viejas del activo
      const deletePromises = asignacionesExistentes.map(asig =>
        AssetsService.deleteAssignment(asig.id)
      );
      await Promise.all(deletePromises);

      // 3. Crear la nueva asignación para el beneficiario indicado
      await AssetsService.createAssignments(activoId, [
        {
          emailBeneficiario: beneficiarioEmail.trim().toLowerCase(),
          porcentajeAsignado: 100,
          condicionLiberacion: `Prioridad: ${prioridad}`,
        }
      ]);

      // 4. Si se eligió un archivo nuevo, reemplaza el adjunto existente (si había uno).
      if (archivoSeleccionado) {
        await AssetsService.subirArchivoActivo(activoId, archivoSeleccionado);
      }

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
      await AssetsService.deleteAsset(activoId);
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

          {/* INFORMACIÓN ESTRUCTURADA DEL ACTIVO (mismos campos que nuevo-activo.tsx,
              parseados de la descripción guardada): antes esto solo se podía editar como
              un único bloque de texto libre, perdiendo la estructura original. */}
          {tipoVal === 2 && (
            <View style={styles.dynamicInfoWrapper}>
              <Text style={styles.sectionHeader}>INFORMACIÓN DEL ACTIVO</Text>
              <View style={styles.infoCard}>
                <Text style={styles.infoFieldLabel}>Red de blockchain</Text>
                <TextInput style={styles.infoInput} value={blockchain} onChangeText={setBlockchain} placeholder="Ethereum mainnet" placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Dirección de wallet</Text>
                <TextInput style={styles.infoInput} value={wallet} onChangeText={setWallet} placeholder="0x1a2b3c......" placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Clave privada</Text>
                <TextInput style={styles.infoInput} value={clavePrivada} onChangeText={setClavePrivada} secureTextEntry placeholder="..............." placeholderTextColor="#999" />
              </View>
            </View>
          )}

          {tipoVal === 0 && (
            <View style={styles.dynamicInfoWrapper}>
              <Text style={styles.sectionHeader}>INFORMACIÓN DEL ACTIVO</Text>
              <View style={styles.infoCard}>
                <Text style={styles.infoFieldLabel}>Nombre del banco</Text>
                <TextInput style={styles.infoInput} value={banco} onChangeText={setBanco} placeholder="Banco Galicia" placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Número de cuenta</Text>
                <TextInput style={styles.infoInput} value={numeroCuenta} onChangeText={setNumeroCuenta} placeholder="0000-0000-0000" placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>CBU / Alias</Text>
                <TextInput style={styles.infoInput} value={cbuAlias} onChangeText={setCbuAlias} placeholder="galicia.carlos.gomez" placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Tipo de cuenta</Text>
                <View style={{ width: '100%', marginBottom: 14 }}>
                  <TouchableOpacity
                    style={[styles.nestedDropdownHeader, tipoCuentaExpanded && styles.nestedDropdownHeaderActive]}
                    activeOpacity={0.8}
                    onPress={() => setTipoCuentaExpanded(!tipoCuentaExpanded)}
                  >
                    <Text style={[styles.selectText, tipoCuentaExpanded && styles.selectTextActive]}>{tipoCuenta}</Text>
                    {tipoCuentaExpanded ? <ChevronUp color="#2E7D32" size={20} /> : <ChevronDown color="#000000" size={20} />}
                  </TouchableOpacity>
                  {tipoCuentaExpanded && (
                    <View style={styles.nestedDropdownList}>
                      {TIPO_CUENTAS.map((tc) => (
                        <TouchableOpacity
                          key={tc}
                          style={[styles.nestedDropdownOption, tipoCuenta === tc && styles.optionSelected]}
                          onPress={() => { setTipoCuenta(tc); setTipoCuentaExpanded(false); }}
                        >
                          <Text style={[styles.optionTitle, tipoCuenta === tc && styles.optionSelectedText]}>{tc}</Text>
                        </TouchableOpacity>
                      ))}
                    </View>
                  )}
                </View>
              </View>
            </View>
          )}

          {tipoVal === 1 && (
            <View style={styles.dynamicInfoWrapper}>
              <Text style={styles.sectionHeader}>INFORMACIÓN DEL ACTIVO</Text>
              <View style={styles.infoCard}>
                <Text style={styles.infoFieldLabel}>Plataforma</Text>
                <TextInput style={styles.infoInput} value={plataformaRed} onChangeText={setPlataformaRed} placeholder="Instagram, Facebook, X.." placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Usuario o link de perfil</Text>
                <TextInput style={styles.infoInput} value={usuarioRed} onChangeText={setUsuarioRed} autoCapitalize="none" placeholder="@usuario o URL del perfil" placeholderTextColor="#999" />
              </View>
            </View>
          )}

          {tipoVal === 3 && (
            <View style={styles.dynamicInfoWrapper}>
              <Text style={styles.sectionHeader}>INFORMACIÓN DEL ACTIVO</Text>
              <View style={styles.infoCard}>
                <Text style={styles.infoFieldLabel}>Proveedor</Text>
                <TextInput style={styles.infoInput} value={proveedorCorreo} onChangeText={setProveedorCorreo} placeholder="Gmail, Outlook, Yahoo.." placeholderTextColor="#999" />
                <Text style={styles.infoFieldLabel}>Dirección de correo</Text>
                <TextInput style={styles.infoInput} value={direccionCorreo} onChangeText={setDireccionCorreo} keyboardType="email-address" autoCapitalize="none" placeholder="nombre@ejemplo.com" placeholderTextColor="#999" />
              </View>
            </View>
          )}

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

          {/* CAMPO: ARCHIVO ADJUNTO (solo si el activo es de tipo "Archivo") */}
          {tipoVal === 4 && (
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Archivo adjunto</Text>
              <View style={styles.fileBoxBorder}>
                <Paperclip color="#777" size={28} style={{ marginBottom: 6 }} />
                <Text style={styles.fileText}>
                  {archivoSeleccionado
                    ? archivoSeleccionado.name
                    : nombreArchivoActual ?? 'Sin archivo adjunto todavía'}
                </Text>
                <TouchableOpacity
                  style={styles.attachButton}
                  onPress={handleAttachFile}
                  disabled={loadingArchivo}
                >
                  {loadingArchivo ? (
                    <ActivityIndicator color="#FFFFFF" />
                  ) : (
                    <Text style={styles.attachButtonText}>
                      {nombreArchivoActual || archivoSeleccionado ? 'Cambiar archivo' : 'Adjuntar archivo'}
                    </Text>
                  )}
                </TouchableOpacity>
              </View>
            </View>
          )}

          {/* CAMPO: NIVEL DE PRIORIDAD (DROPDOWN INLINE) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Nivel de prioridad</Text>
            <TouchableOpacity
              style={[
                styles.dropdownButton,
                showPrioridadDropdown && styles.dropdownButtonActive,
              ]}
              activeOpacity={0.8}
              onPress={() => setShowPrioridadDropdown(!showPrioridadDropdown)}
            >
              <Text style={[
                styles.dropdownButtonText,
                showPrioridadDropdown && styles.dropdownButtonTextActive,
              ]}>{prioridad}</Text>
              {showPrioridadDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
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

          {/* CAMPO: EMAIL DEL BENEFICIARIO */}
          {/* El backend invita y asigna en la misma operación (POST .../asignaciones con un
              email): no hay una lista de "beneficiarios" separada de la que elegir. */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Email del beneficiario</Text>
            <TextInput
              style={[styles.textInput, emailError && styles.textInputError]}
              value={beneficiarioEmail}
              onChangeText={(text) => {
                setBeneficiarioEmail(text);
                if (emailError) setEmailError(false);
              }}
              placeholder="beneficiario@email.com"
              placeholderTextColor="#8A9E95"
              keyboardType="email-address"
              autoCapitalize="none"
            />
            {emailError && (
              <Text style={styles.emailErrorText}>Ingresá un email válido.</Text>
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
    borderColor: '#C1E3A4', // Borde verde claro suave
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
  textInputError: {
    borderColor: '#C53929',
    borderWidth: 1.5,
  },
  emailErrorText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#C53929',
    marginTop: -4,
  },
  dynamicInfoWrapper: {
    marginBottom: 4,
  },
  sectionHeader: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1a2e2e',
    marginBottom: 10,
    marginLeft: 4,
    letterSpacing: 0.5,
  },
  infoCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    padding: 16,
  },
  infoFieldLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    color: '#555',
    marginBottom: 6,
    marginLeft: 2,
  },
  infoInput: {
    backgroundColor: '#FFFFFF',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E2E2E2',
    paddingHorizontal: 12,
    height: 44,
    fontSize: 14,
    fontFamily: 'MPLUS2-Regular',
    color: '#333',
    marginBottom: 14,
  },
  selectText: {
    fontSize: 15,
    fontFamily: 'MPLUS2-Bold',
    color: '#000000',
  },
  selectTextActive: {
    color: '#2E7D32',
  },
  optionSelected: {
    backgroundColor: '#DAF8BD',
  },
  optionSelectedText: {
    color: '#2E7D32',
    fontFamily: 'MPLUS2-Bold',
  },
  optionTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#333',
  },
  nestedDropdownHeader: {
    height: 44,
    paddingHorizontal: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
  },
  nestedDropdownHeaderActive: {
    borderColor: '#2E7D32',
    borderWidth: 1.5,
  },
  nestedDropdownList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    marginTop: 4,
    overflow: 'hidden',
  },
  nestedDropdownOption: {
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderRadius: 8,
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
  fileBoxBorder: {
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    borderStyle: 'dashed',
    borderRadius: 8,
    padding: 20,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#FAFAFA',
  },
  fileText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#777',
    marginBottom: 12,
    textAlign: 'center',
  },
  attachButton: {
    backgroundColor: '#D97706',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 6,
  },
  attachButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
  },
  dropdownButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado (rectangular suave)
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    height: 48,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  dropdownButtonActive: {
    borderColor: '#2E7D32', // Borde verde oscuro activo
    borderWidth: 1.5,
  },
  dropdownButtonText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#000000',
  },
  dropdownButtonTextActive: {
    color: '#2E7D32', // Texto verde oscuro activo
  },
  dropdownInlineList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    padding: 10,
    marginTop: 6,
    gap: 2,
    maxHeight: 180,
  },
  dropdownInlineOption: {
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8, // Esquinas más suaves para selección interna
  },
  dropdownInlineOptionSelected: {
    backgroundColor: '#DAF8BD', // Fondo verde claro pastel de selección
  },
  dropdownInlineOptionText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
    color: '#333333',
  },
  dropdownInlineOptionTextSelected: {
    fontFamily: 'MPLUS2-Bold',
    color: '#2E7D32', // Texto verde oscuro para la opción seleccionada
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
