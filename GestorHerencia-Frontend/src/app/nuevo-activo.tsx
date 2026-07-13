/**
 * @file nuevo-activo.tsx
 * @description Pantalla de creación y registro de nuevos activos digitales.
 *
 * Formulario con campos dinámicos según el TipoActivoDigital elegido (cripto, cuenta
 * bancaria, red social, correo o archivo) y asignación del 100% del activo a un único
 * beneficiario, que se invita por email en la misma operación de guardado.
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  Alert,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import * as DocumentPicker from 'expo-document-picker';
import {
  ArrowLeft,
  AlertTriangle,
  Bitcoin,
  Landmark,
  Paperclip,
  Share2,
  Mail,
  ChevronDown,
  ChevronUp,
} from 'lucide-react-native';
import { useAuth } from '../context/AuthContext';
import { AssetsService } from '../services/assets.service';

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Tipos de archivo que el backend acepta para adjuntos (ver ActivoDigitalService.TiposPermitidos). */
const TIPOS_MIME_PERMITIDOS = ['application/pdf', 'image/jpeg', 'image/png'];

/** Forma en la que expo-document-picker entrega el archivo elegido por el usuario. */
type ArchivoSeleccionado = { uri: string; name: string; mimeType: string };

type AssetType = {
  id: number;
  label: string;
  description: string;
  icon: any;
};

// Los 5 valores del enum TipoActivoDigital del backend (Herencia.Data.Models):
// 0=CuentaBancaria, 1=RedSocial, 2=BilleteraCripto, 3=CorreoElectronico, 4=Otro.
// Antes solo se ofrecían 3 de los 5: Red Social y Correo Electrónico existían en el
// enum y en el mapeo de lectura (activos.tsx, editar-activo.tsx) pero no eran
// seleccionables acá, la única pantalla que realmente los crea.
const ASSET_TYPES: AssetType[] = [
  { id: 2, label: 'Cripto', description: 'Wallets, Bitcoin, Ethereum..', icon: Bitcoin },
  { id: 0, label: 'Cuenta bancaria', description: 'CBU, alias, numero de cu..', icon: Landmark },
  { id: 1, label: 'Red social', description: 'Instagram, Facebook, X..', icon: Share2 },
  { id: 3, label: 'Correo electrónico', description: 'Gmail, Outlook, etc..', icon: Mail },
  { id: 4, label: 'Archivo', description: 'PDFs, imagenes, docume..', icon: Paperclip },
];

const PRIORITIES = ['Alta', 'Media', 'Baja'];
const TIPO_CUENTAS = ['Caja de ahorro', 'Cuenta corriente'];

export default function NuevoActivoScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  // ESTADOS DEL FORMULARIO GENERAL
  const [nombre, setNombre] = useState('');
  const [tipo, setTipo] = useState<AssetType | null>(null);
  const [instrucciones, setInstrucciones] = useState('');
  const [prioridad, setPrioridad] = useState('Media');
  const [beneficiarioEmail, setBeneficiarioEmail] = useState('');

  // ESTADOS DINÁMICOS POR TIPO DE ACTIVO
  const [blockchain, setBlockchain] = useState('');
  const [wallet, setWallet] = useState('');
  const [clavePrivada, setClavePrivada] = useState('');

  // Cuenta bancaria:
  const [banco, setBanco] = useState('');
  const [numeroCuenta, setNumeroCuenta] = useState('');
  const [cbuAlias, setCbuAlias] = useState('');
  const [tipoCuenta, setTipoCuenta] = useState('Caja de ahorro');
  const [tipoCuentaExpanded, setTipoCuentaExpanded] = useState(false);

  // Red social:
  const [plataformaRed, setPlataformaRed] = useState('');
  const [usuarioRed, setUsuarioRed] = useState('');

  // Correo electrónico:
  const [proveedorCorreo, setProveedorCorreo] = useState('');
  const [direccionCorreo, setDireccionCorreo] = useState('');

  // Archivo: se guarda el objeto completo que devuelve expo-document-picker (no solo
  // el nombre), porque es lo que necesita FormData para poder leer el archivo real del
  // disco al subirlo (ver AssetsService.subirArchivoActivo).
  const [archivoSeleccionado, setArchivoSeleccionado] = useState<ArchivoSeleccionado | null>(null);
  const [loadingArchivo, setLoadingArchivo] = useState(false);

  // AUXILIARES
  const [saving, setSaving] = useState(false);

  // Control de expansión
  const [tipoExpanded, setTipoExpanded] = useState(false);
  const [prioridadExpanded, setPrioridadExpanded] = useState(false);

  const [showValidationError, setShowValidationError] = useState(false);
  const [emailError, setEmailError] = useState(false);

  /** Abre el selector de archivos nativo (PDF, JPG o PNG) para el activo tipo "Archivo". */
  const handleAttachFile = async () => {
    setLoadingArchivo(true);
    try {
      // Cancelar el diálogo resuelve la promesa con `canceled: true`, no la rechaza:
      // no hay que tratarlo como error.
      const resultado = await DocumentPicker.getDocumentAsync({
        // Mismos tipos que acepta el backend (ActivoDigitalService.TiposPermitidos),
        // para no dejar elegir un archivo que el servidor rechazaría después.
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
        // mimeType puede venir undefined según la plataforma; se usa un valor genérico
        // de respaldo (igual será rechazado por el backend si no es uno de los 3 permitidos).
        mimeType: archivo.mimeType ?? 'application/octet-stream',
      });
    } catch (err) {
      console.error('Error al seleccionar el archivo:', err);
      Alert.alert('Error', 'No se pudo abrir el selector de archivos.');
    } finally {
      setLoadingArchivo(false);
    }
  };

  /**
   * Valida y crea el activo, con sus campos requeridos dependiendo del tipo elegido,
   * y lo asigna 100% al beneficiario indicado (invitándolo por email si no existe).
   */
  const handleSave = async () => {
    setEmailError(false);

    if (!nombre || !tipo || !beneficiarioEmail.trim()) {
      setShowValidationError(true);
      return;
    }

    if (!EMAIL_REGEX.test(beneficiarioEmail.trim())) {
      setEmailError(true);
      return;
    }

    // Cada tipo de activo tiene su propio set de campos obligatorios (ver el bloque
    // "INFORMACIÓN DINÁMICA DEL ACTIVO" más abajo, donde se renderizan)
    if (tipo.label === 'Cripto' && (!blockchain || !wallet || !clavePrivada)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Cuenta bancaria' && (!banco || !numeroCuenta || !cbuAlias || !tipoCuenta)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Red social' && (!plataformaRed || !usuarioRed)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Correo electrónico' && (!proveedorCorreo || !direccionCorreo)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Archivo' && !archivoSeleccionado) {
      setShowValidationError(true);
      return;
    }

    setShowValidationError(false);
    setSaving(true);

    try {
      if (!token) throw new Error('Usuario no autenticado.');

      // Los campos estructurados se serializan dentro de "descripcion" con un formato fijo
      // por tipo (el backend no tiene columnas propias para ellos). editar-activo.tsx
      // parsea este mismo formato con regex para poder editarlos por separado: si se
      // cambia el formato acá, hay que actualizar esas regex también.
      let descripcionFinal = '';
      if (tipo.label === 'Cripto') {
        descripcionFinal = `[CRIPTO] Blockchain: ${blockchain} | Wallet: ${wallet} | Clave Privada: ${clavePrivada}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Cuenta bancaria') {
        descripcionFinal = `[BANCO] Banco: ${banco} | Cuenta: ${numeroCuenta} | CBU/Alias: ${cbuAlias} | Tipo: ${tipoCuenta}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Red social') {
        descripcionFinal = `[RED SOCIAL] Plataforma: ${plataformaRed} | Usuario: ${usuarioRed}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Correo electrónico') {
        descripcionFinal = `[CORREO] Proveedor: ${proveedorCorreo} | Dirección: ${direccionCorreo}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Archivo') {
        descripcionFinal = `[ARCHIVO] Adjunto: ${archivoSeleccionado?.name}\n\nInstrucciones:\n${instrucciones}`;
      }

      const activoCreado = await AssetsService.createAsset(
        {
          nombre,
          tipo: tipo.id,
          descripcion: descripcionFinal,
        },
        beneficiarioEmail.trim().toLowerCase(),
        prioridad
      );

      // El archivo se sube recién después de crear el activo: el endpoint de subida
      // necesita el Id que la base de datos le asigna, inexistente hasta este punto.
      if (tipo.label === 'Archivo' && archivoSeleccionado) {
        await AssetsService.subirArchivoActivo(activoCreado.id, archivoSeleccionado);
      }

      Alert.alert(
        'Activo guardado',
        'El activo digital ha sido registrado y asignado exitosamente.',
        [{ text: 'Entendido', onPress: () => router.replace('/(tabs)?success=true' as any) }]
      );
    } catch (err: any) {
      console.error('Error al guardar activo:', err);
      Alert.alert('Error', err.message || 'No se pudo guardar el activo.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={styles.container}>
      {/* Header con gradiente */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity
            onPress={() => router.replace('/')}
            style={styles.backButton}
            activeOpacity={0.7}
          >
            <ArrowLeft color="#FFFFFF" size={24} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Agregar nuevo activo</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={{ flex: 1 }}
      >
        <ScrollView contentContainerStyle={styles.scrollContent}>
          <View style={styles.form}>
            
            {/* NOMBRE */}
            <Text style={styles.fieldLabel}>Nombre del activo</Text>
            <TextInput
              style={styles.input}
              placeholder="Ethereum o Cuenta Galicia"
              placeholderTextColor="#999"
              value={nombre}
              onChangeText={(text) => {
                setNombre(text);
                if (showValidationError) setShowValidationError(false);
              }}
            />

            {/* TIPO DE ACTIVO SELECT */}
            <Text style={styles.fieldLabel}>Tipo de activo</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={[
                  styles.dropdownHeader,
                  tipoExpanded && styles.dropdownHeaderActive
                ]}
                activeOpacity={0.8}
                onPress={() => {
                  setTipoExpanded(!tipoExpanded);
                  setPrioridadExpanded(false);
                }}
              >
                <Text style={[
                  styles.selectText,
                  tipoExpanded && styles.selectTextActive,
                  !tipo && styles.placeholderText
                ]}>
                  {tipo ? tipo.label : 'Seleccionar tipo'}
                </Text>
                {tipoExpanded ? (
                  <ChevronUp color="#2E7D32" size={20} />
                ) : (
                  <ChevronDown color="#000000" size={20} />
                )}
              </TouchableOpacity>

              {tipoExpanded && (
                <View style={styles.dropdownOptionsList}>
                  {ASSET_TYPES.map((item) => {
                    const IconComp = item.icon;
                    const isSelected = tipo?.id === item.id;
                    return (
                      <TouchableOpacity
                        key={item.id}
                        style={[
                          styles.tipoOptionRow,
                          isSelected && styles.optionSelected
                        ]}
                        onPress={() => {
                          setTipo(item);
                          setTipoExpanded(false);
                          if (showValidationError) setShowValidationError(false);
                        }}
                      >
                        <View style={styles.optionIconWrapper}>
                          <IconComp color={isSelected ? '#2E7D32' : '#000000'} size={24} />
                        </View>
                        <View style={styles.optionTextWrapper}>
                          <Text style={[
                            styles.optionTitle,
                            isSelected && styles.optionSelectedText
                          ]}>{item.label}</Text>
                          <Text style={styles.optionSubtitle}>{item.description}</Text>
                        </View>
                      </TouchableOpacity>
                    );
                  })}
                </View>
              )}
            </View>

            {/* INFORMACIÓN DINÁMICA DEL ACTIVO */}
            {tipo && (
              <View style={styles.dynamicInfoWrapper}>
                <Text style={styles.sectionHeader}>INFORMACIÓN DEL ACTIVO</Text>
                <View style={styles.infoCard}>
                  
                  {tipo.label === 'Cripto' && (
                    <View>
                      <Text style={styles.infoFieldLabel}>Red de blockchain</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="Ethereum mainnet"
                        placeholderTextColor="#999"
                        value={blockchain}
                        onChangeText={setBlockchain}
                      />

                      <Text style={styles.infoFieldLabel}>Direccion de wallet</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="0x1a2b3c......"
                        placeholderTextColor="#999"
                        value={wallet}
                        onChangeText={setWallet}
                      />

                      <Text style={styles.infoFieldLabel}>Clave privada</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="..............."
                        placeholderTextColor="#999"
                        secureTextEntry
                        value={clavePrivada}
                        onChangeText={setClavePrivada}
                      />
                    </View>
                  )}

                  {tipo.label === 'Cuenta bancaria' && (
                    <View>
                      <Text style={styles.infoFieldLabel}>Nombre del banco</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="Banco Galicia"
                        placeholderTextColor="#999"
                        value={banco}
                        onChangeText={setBanco}
                      />

                      <Text style={styles.infoFieldLabel}>Número de cuenta</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="0000-0000-0000"
                        placeholderTextColor="#999"
                        value={numeroCuenta}
                        onChangeText={setNumeroCuenta}
                      />

                      <Text style={styles.infoFieldLabel}>CBU / Alias</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="galicia.carlos.gomez"
                        placeholderTextColor="#999"
                        value={cbuAlias}
                        onChangeText={setCbuAlias}
                      />

                      <Text style={styles.infoFieldLabel}>Tipo de cuenta</Text>
                      <View style={{ width: '100%', marginBottom: 14 }}>
                        <TouchableOpacity
                          style={[
                            styles.nestedDropdownHeader,
                            tipoCuentaExpanded && styles.nestedDropdownHeaderActive
                          ]}
                          activeOpacity={0.8}
                          onPress={() => setTipoCuentaExpanded(!tipoCuentaExpanded)}
                        >
                          <Text style={[
                            styles.selectText,
                            tipoCuentaExpanded && styles.selectTextActive
                          ]}>{tipoCuenta}</Text>
                          {tipoCuentaExpanded ? (
                            <ChevronUp color="#2E7D32" size={20} />
                          ) : (
                            <ChevronDown color="#000000" size={20} />
                          )}
                        </TouchableOpacity>

                        {tipoCuentaExpanded && (
                          <View style={styles.nestedDropdownList}>
                            {TIPO_CUENTAS.map((tc) => {
                              const isSelected = tipoCuenta === tc;
                              return (
                                <TouchableOpacity
                                  key={tc}
                                  style={[
                                    styles.nestedDropdownOption,
                                    isSelected && styles.optionSelected
                                  ]}
                                  onPress={() => {
                                    setTipoCuenta(tc);
                                    setTipoCuentaExpanded(false);
                                  }}
                                >
                                  <Text style={[
                                    styles.optionTitle,
                                    isSelected && styles.optionSelectedText
                                  ]}>{tc}</Text>
                                </TouchableOpacity>
                              );
                            })}
                          </View>
                        )}
                      </View>
                    </View>
                  )}

                  {tipo.label === 'Red social' && (
                    <View>
                      <Text style={styles.infoFieldLabel}>Plataforma</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="Instagram, Facebook, X.."
                        placeholderTextColor="#999"
                        value={plataformaRed}
                        onChangeText={setPlataformaRed}
                      />

                      <Text style={styles.infoFieldLabel}>Usuario o link de perfil</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="@usuario o URL del perfil"
                        placeholderTextColor="#999"
                        autoCapitalize="none"
                        value={usuarioRed}
                        onChangeText={setUsuarioRed}
                      />
                    </View>
                  )}

                  {tipo.label === 'Correo electrónico' && (
                    <View>
                      <Text style={styles.infoFieldLabel}>Proveedor</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="Gmail, Outlook, Yahoo.."
                        placeholderTextColor="#999"
                        value={proveedorCorreo}
                        onChangeText={setProveedorCorreo}
                      />

                      <Text style={styles.infoFieldLabel}>Dirección de correo</Text>
                      <TextInput
                        style={styles.infoInput}
                        placeholder="nombre@ejemplo.com"
                        placeholderTextColor="#999"
                        keyboardType="email-address"
                        autoCapitalize="none"
                        value={direccionCorreo}
                        onChangeText={setDireccionCorreo}
                      />
                    </View>
                  )}

                  {tipo.label === 'Archivo' && (
                    <View style={styles.fileBoxBorder}>
                      <Paperclip color="#777" size={32} style={styles.fileIcon} />
                      <Text style={styles.fileText}>
                        {archivoSeleccionado ? archivoSeleccionado.name : 'Tocá para adjuntar un archivo (PDF, JPG o PNG)'}
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
                            {archivoSeleccionado ? 'Cambiar archivo' : 'Adjuntar archivo'}
                          </Text>
                        )}
                      </TouchableOpacity>
                    </View>
                  )}
                </View>
              </View>
            )}

            {/* INSTRUCCIONES PARA EL BENEFICIARIO */}
            <Text style={styles.fieldLabel}>Instrucciones para el beneficiario</Text>
            <TextInput
              style={[styles.input, styles.textArea]}
              placeholder="Escribe aquí las instrucciones de acceso..."
              placeholderTextColor="#999"
              multiline
              numberOfLines={4}
              textAlignVertical="top"
              value={instrucciones}
              onChangeText={(text) => {
                setInstrucciones(text);
                if (showValidationError) setShowValidationError(false);
              }}
            />

            {/* PRIORIDAD SELECT */}
            <Text style={styles.fieldLabel}>Nivel de prioridad</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={[
                  styles.dropdownHeader,
                  prioridadExpanded && styles.dropdownHeaderActive
                ]}
                activeOpacity={0.8}
                onPress={() => {
                  setPrioridadExpanded(!prioridadExpanded);
                  setTipoExpanded(false);
                }}
              >
                <Text style={[
                  styles.selectText,
                  prioridadExpanded && styles.selectTextActive
                ]}>{prioridad}</Text>
                {prioridadExpanded ? (
                  <ChevronUp color="#2E7D32" size={20} />
                ) : (
                  <ChevronDown color="#000000" size={20} />
                )}
              </TouchableOpacity>

              {prioridadExpanded && (
                <View style={styles.dropdownOptionsList}>
                  {PRIORITIES.map((p) => {
                    const isSelected = prioridad === p;
                    return (
                      <TouchableOpacity
                        key={p}
                        style={[
                          styles.simpleOptionRow,
                          isSelected && styles.optionSelected
                        ]}
                        onPress={() => {
                          setPrioridad(p);
                          setPrioridadExpanded(false);
                        }}
                      >
                        <Text style={[
                          styles.optionTitle,
                          isSelected && styles.optionSelectedText
                        ]}>{p}</Text>
                      </TouchableOpacity>
                    );
                  })}
                </View>
              )}
            </View>

            {/* BENEFICIARIO: EMAIL DE INVITACIÓN */}
            {/* El backend invita y asigna en la misma operación (POST .../asignaciones con un
                email): no existe una lista de "beneficiarios registrados" de la que elegir. */}
            <Text style={styles.fieldLabel}>Email del beneficiario</Text>
            <TextInput
              style={[styles.input, emailError && styles.inputError]}
              placeholder="beneficiario@email.com"
              placeholderTextColor="#999"
              keyboardType="email-address"
              autoCapitalize="none"
              value={beneficiarioEmail}
              onChangeText={(text) => {
                setBeneficiarioEmail(text);
                if (showValidationError) setShowValidationError(false);
                if (emailError) setEmailError(false);
              }}
            />
            {emailError && (
              <Text style={styles.emailErrorText}>Ingresá un email válido.</Text>
            )}

            {/* ERROR DE VALIDACIÓN */}
            {showValidationError && (
              <View style={styles.validationBox}>
                <AlertTriangle color="#A83232" size={20} style={styles.validationIcon} />
                <Text style={styles.validationText}>Por favor, completa todos los campos requeridos.</Text>
              </View>
            )}

            {/* BOTÓN GUARDAR */}
            <TouchableOpacity
              style={styles.saveButton}
              onPress={handleSave}
              disabled={saving}
              activeOpacity={0.8}
            >
              {saving ? (
                <ActivityIndicator color="#FFFFFF" />
              ) : (
                <Text style={styles.saveButtonText}>Guardar activo</Text>
              )}
            </TouchableOpacity>

          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
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
  form: {
    width: '100%',
    maxWidth: 600,
    alignSelf: 'center',
  },
  fieldLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1A202C',
    marginBottom: 8,
    marginLeft: 4,
  },
  input: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingHorizontal: 16,
    height: 52,
    fontSize: 15,
    fontFamily: 'MPLUS2-Regular',
    color: '#333',
    marginBottom: 16,
  },
  textArea: {
    height: 100,
    paddingTop: 16,
  },
  inputError: {
    borderColor: '#C53929',
    borderWidth: 1.5,
  },
  emailErrorText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#C53929',
    marginTop: -12,
    marginBottom: 16,
    marginLeft: 4,
  },
  dropdownContainer: {
    marginBottom: 16,
  },
  dropdownHeader: {
    height: 52,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
  },
  dropdownHeaderActive: {
    borderColor: '#2E7D32',
    borderWidth: 1.5,
  },
  dropdownOptionsList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 10,
    marginTop: 6,
    gap: 2,
    overflow: 'hidden',
  },
  selectText: {
    fontSize: 15,
    fontFamily: 'MPLUS2-Bold',
    color: '#000000',
  },
  selectTextActive: {
    color: '#2E7D32',
  },
  placeholderText: {
    color: '#999',
  },
  tipoOptionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
  },
  simpleOptionRow: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    height: 48,
    justifyContent: 'center',
  },
  optionSelected: {
    backgroundColor: '#DAF8BD',
  },
  optionSelectedText: {
    color: '#2E7D32',
    fontFamily: 'MPLUS2-Bold',
  },
  optionIconWrapper: {
    width: 36,
    height: 36,
    borderRadius: 6,
    backgroundColor: '#F5F5F5',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  optionTextWrapper: {
    flex: 1,
  },
  optionTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#333',
  },
  optionSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 12,
    color: '#777',
    marginTop: 1,
  },
  dynamicInfoWrapper: {
    marginBottom: 16,
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
  fileIcon: {
    marginBottom: 8,
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
  validationBox: {
    backgroundColor: '#EAD9C2',
    borderWidth: 1,
    borderColor: '#D8A657',
    borderRadius: 8,
    padding: 12,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  validationIcon: {
    marginRight: 8,
  },
  validationText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#A83232',
  },
  saveButton: {
    backgroundColor: '#42C167',
    height: 52,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    marginTop: 8,
  },
  saveButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
  },
});
