/**
 * @file nuevo-activo.tsx
 * @description Pantalla de creación y registro de nuevos activos digitales.
 * 
 * Implementa un formulario modular e interactivo fiel a las maquetas de Figma.
 * 
 * ### Estrategia de Serialización en Cliente:
 * Debido a la estructura simplificada del esquema de base de datos relacional del backend
 * (que almacena un campo de texto genérico 'Descripcion'), esta pantalla realiza una
 * serialización o "empaquetamiento" previo de la información del activo:
 * - Cripto: Empaqueta blockchain, wallet y clave privada cifrada.
 * - Banco: Empaqueta banco, cuenta, CBU/Alias y tipo de cuenta.
 * - Archivo: Vincula el nombre del archivo simulado adjunto.
 * Todo esto se formatea y concatena en el campo 'descripcion' antes de enviarse mediante POST
 * a la API, permitiendo guardar activos complejos sin requerir cambios inmediatos en el esquema SQL.
 * 
 * ### Navegación y UI:
 * - Emplea acordeones colapsables en línea (dropdowns inline) en lugar de modales flotantes,
 *   preservando el foco visual y reduciendo el salto de pantallas.
 * - Tras un guardado exitoso, redirige directamente al Dashboard de pestañas (`/(tabs)`)
 *   pasando el parámetro `success=true` en la URL.
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
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import {
  ArrowLeft,
  AlertTriangle,
  Bitcoin,
  Landmark,
  Paperclip,
  ChevronDown,
  ChevronUp,
} from 'lucide-react-native';
import { useAuth } from '../context/AuthContext';
import { AssetsService, BeneficiarioDTO } from '../services/assets.service';

/**
 * Representa los tipos de activos disponibles para su registro, mapeando
 * su ID con el enumerador TipoActivoDigital del backend.
 */
type AssetType = {
  id: number; // Mapea con TipoActivoDigital (0=Banco, 2=Cripto, 4=Archivo)
  label: string;
  description: string;
  icon: any;
};

// Colección estática de tipos de activos soportados para renderizado en los selectores.
const ASSET_TYPES: AssetType[] = [
  { id: 2, label: 'Cripto', description: 'Wallets, Bitcoin, Ethereum..', icon: Bitcoin },
  { id: 0, label: 'Cuenta bancaria', description: 'CBU, alias, numero de cu..', icon: Landmark },
  { id: 4, label: 'Archivo', description: 'PDFs, imagenes, docume..', icon: Paperclip },
];

const PRIORITIES = ['Alta', 'Media', 'Baja'];
const TIPO_CUENTAS = ['Caja de ahorro', 'Cuenta corriente'];

export default function NuevoActivoScreen() {
  const router = useRouter();
  const { token } = useAuth();

  // ==========================================
  // ESTADOS DEL FORMULARIO GENERAL
  // ==========================================
  const [nombre, setNombre] = useState('');
  const [tipo, setTipo] = useState<AssetType | null>(null);
  const [instrucciones, setInstrucciones] = useState('');
  const [prioridad, setPrioridad] = useState('Media');
  const [beneficiario, setBeneficiario] = useState<BeneficiarioDTO | null>(null);

  // ==========================================
  // ESTADOS DINÁMICOS POR TIPO DE ACTIVO
  // ==========================================
  // Cripto:
  const [blockchain, setBlockchain] = useState('');
  const [wallet, setWallet] = useState('');
  const [clavePrivada, setClavePrivada] = useState('');

  // Cuenta bancaria:
  const [banco, setBanco] = useState('');
  const [numeroCuenta, setNumeroCuenta] = useState('');
  const [cbuAlias, setCbuAlias] = useState('');
  const [tipoCuenta, setTipoCuenta] = useState('Caja de ahorro');
  const [tipoCuentaExpanded, setTipoCuentaExpanded] = useState(false);

  // Archivo:
  const [archivoAdjunto, setArchivoAdjunto] = useState<string | null>(null);
  const [loadingArchivo, setLoadingArchivo] = useState(false);

  // ==========================================
  // ESTADOS AUXILIARES (APIs Y CONTROL DE UI)
  // ==========================================
  const [beneficiariosList, setBeneficiariosList] = useState<BeneficiarioDTO[]>([]);
  const [loadingBeneficiarios, setLoadingBeneficiarios] = useState(true);
  const [saving, setSaving] = useState(false);

  // Control de expansión de acordeones (evita que dos acordeones se desplieguen a la vez)
  const [tipoExpanded, setTipoExpanded] = useState(false);
  const [prioridadExpanded, setPrioridadExpanded] = useState(false);
  const [beneficiarioExpanded, setBeneficiarioExpanded] = useState(false);

  // Bandera para disparar el cartel flotante de error en validaciones locales
  const [showValidationError, setShowValidationError] = useState(false);

  // Cargar beneficiarios del backend al montar la pantalla.
  // El backend exige autenticación, por lo que requerimos el JWT token del AuthContext.
  useEffect(() => {
    const fetchBeneficiarios = async () => {
      if (!token) return;
      try {
        const data = await AssetsService.getBeneficiarios(token);
        setBeneficiariosList(data);
      } catch (error) {
        console.error('Error fetching beneficiaries:', error);
      } finally {
        setLoadingBeneficiarios(false);
      }
    };

    fetchBeneficiarios();
  }, [token]);

  /**
   * Simulación del proceso de carga asíncrona de un archivo adjunto.
   * Cambia el estado del loader y asocia un nombre de archivo mock.
   */
  const handleAttachFile = () => {
    setLoadingArchivo(true);
    setTimeout(() => {
      setArchivoAdjunto('contrato_digital.pdf');
      setLoadingArchivo(false);
    }, 1000);
  };

  /**
   * Ejecuta las validaciones locales de datos e inicia el proceso de persistencia.
   */
  const handleSave = async () => {
    // 1. Validación de campos obligatorios comunes
    if (!nombre || !tipo || !beneficiario) {
      setShowValidationError(true);
      return;
    }

    // 2. Validación de campos específicos por tipo de activo
    if (tipo.label === 'Cripto' && (!blockchain || !wallet || !clavePrivada)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Cuenta bancaria' && (!banco || !numeroCuenta || !cbuAlias || !tipoCuenta)) {
      setShowValidationError(true);
      return;
    }
    if (tipo.label === 'Archivo' && !archivoAdjunto) {
      setShowValidationError(true);
      return;
    }

    setShowValidationError(false);
    setSaving(true);

    try {
      if (!token) throw new Error('Usuario no autenticado.');

      // 3. Serializamos la información específica estructurada en un bloque legible
      // de texto plano para insertarlo en la columna genérica 'Descripcion'.
      let descripcionFinal = '';
      if (tipo.label === 'Cripto') {
        descripcionFinal = `[CRIPTO] Blockchain: ${blockchain} | Wallet: ${wallet} | Clave Privada: ${clavePrivada}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Cuenta bancaria') {
        descripcionFinal = `[BANCO] Banco: ${banco} | Cuenta: ${numeroCuenta} | CBU/Alias: ${cbuAlias} | Tipo: ${tipoCuenta}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Archivo') {
        descripcionFinal = `[ARCHIVO] Adjunto: ${archivoAdjunto}\n\nInstrucciones:\n${instrucciones}`;
      }

      // 4. Invocamos al AssetsService para persistir el activo y su asignación
      await AssetsService.createAsset(
        token,
        {
          nombre,
          tipo: tipo.id,
          descripcion: descripcionFinal,
        },
        beneficiario.id,
        prioridad
      );

      // 5. Redireccionamos exitosamente al Dashboard inyectando la bandera
      // 'success=true' en los parámetros locales.
      router.replace({
        pathname: '/(tabs)',
        params: { success: 'true' },
      });
    } catch (error: any) {
      Alert.alert('Error al guardar', error.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={styles.container}>
      {/* HEADER CON GRADIENTE */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={styles.header}
      >
        <SafeAreaView edges={['top']} style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.back()} style={styles.backButton}>
            <ArrowLeft color="#FFFFFF" size={24} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Agregar nuevo activo</Text>
          <View style={{ width: 24 }} />
        </SafeAreaView>
      </LinearGradient>

      {/* FORMULARIO */}
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

            {/* TIPO DE ACTIVO SELECT INLINE */}
            <Text style={styles.fieldLabel}>Tipo de activo</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={styles.dropdownHeader}
                onPress={() => {
                  setTipoExpanded(!tipoExpanded);
                  setPrioridadExpanded(false);
                  setBeneficiarioExpanded(false);
                }}
              >
                <Text style={[styles.selectText, !tipo && styles.placeholderText]}>
                  {tipo ? tipo.label : 'Seleccionar tipo'}
                </Text>
                {tipoExpanded ? <ChevronUp color="#000" size={20} /> : <ChevronDown color="#000" size={20} />}
              </TouchableOpacity>

              {tipoExpanded && (
                <View style={styles.dropdownOptionsList}>
                  {ASSET_TYPES.map((item) => {
                    const IconComp = item.icon;
                    return (
                      <TouchableOpacity
                        key={item.id}
                        style={styles.tipoOptionRow}
                        onPress={() => {
                          setTipo(item);
                          setTipoExpanded(false);
                          if (showValidationError) setShowValidationError(false);
                        }}
                      >
                        <View style={styles.optionIconWrapper}>
                          <IconComp color="#000" size={24} />
                        </View>
                        <View style={styles.optionTextWrapper}>
                          <Text style={styles.optionTitle}>{item.label}</Text>
                          <Text style={styles.optionSubtitle}>{item.description}</Text>
                        </View>
                      </TouchableOpacity>
                    );
                  })}
                </View>
              )}
            </View>

            {/* INFORMACIÓN DINÁMICA DEL ACTIVO (CARD CON BORDE VERDE) */}
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
                      <View style={styles.nestedDropdownContainer}>
                        <TouchableOpacity
                          style={styles.nestedDropdownHeader}
                          onPress={() => setTipoCuentaExpanded(!tipoCuentaExpanded)}
                        >
                          <Text style={styles.selectText}>{tipoCuenta}</Text>
                          {tipoCuentaExpanded ? <ChevronUp color="#000" size={20} /> : <ChevronDown color="#000" size={20} />}
                        </TouchableOpacity>

                        {tipoCuentaExpanded && (
                          <View style={styles.nestedDropdownList}>
                            {TIPO_CUENTAS.map((tc) => (
                              <TouchableOpacity
                                key={tc}
                                style={styles.nestedDropdownOption}
                                onPress={() => {
                                  setTipoCuenta(tc);
                                  setTipoCuentaExpanded(false);
                                }}
                              >
                                <Text style={styles.optionTitle}>{tc}</Text>
                              </TouchableOpacity>
                            ))}
                          </View>
                        )}
                      </View>
                    </View>
                  )}

                  {tipo.label === 'Archivo' && (
                    <View style={styles.fileBoxBorder}>
                      <Paperclip color="#777" size={32} style={styles.fileIcon} />
                      <Text style={styles.fileText}>
                        {archivoAdjunto ? archivoAdjunto : 'Tocá para adjuntar un archivo'}
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
                            {archivoAdjunto ? 'Cambiar archivo' : 'Adjuntar archivo'}
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

            {/* PRIORIDAD SELECT INLINE */}
            <Text style={styles.fieldLabel}>Nivel de prioridad</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={styles.dropdownHeader}
                onPress={() => {
                  setPrioridadExpanded(!prioridadExpanded);
                  setTipoExpanded(false);
                  setBeneficiarioExpanded(false);
                }}
              >
                <Text style={styles.selectText}>{prioridad}</Text>
                {prioridadExpanded ? <ChevronUp color="#000" size={20} /> : <ChevronDown color="#000" size={20} />}
              </TouchableOpacity>

              {prioridadExpanded && (
                <View style={styles.dropdownOptionsList}>
                  {PRIORITIES.map((p) => (
                    <TouchableOpacity
                      key={p}
                      style={styles.simpleOptionRow}
                      onPress={() => {
                        setPrioridad(p);
                        setPrioridadExpanded(false);
                      }}
                    >
                      <Text style={styles.optionTitle}>{p}</Text>
                    </TouchableOpacity>
                  ))}
                </View>
              )}
            </View>

            {/* BENEFICIARIO SELECT INLINE */}
            <Text style={styles.fieldLabel}>Beneficiario asignado</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={styles.dropdownHeader}
                onPress={() => {
                  setBeneficiarioExpanded(!beneficiarioExpanded);
                  setTipoExpanded(false);
                  setPrioridadExpanded(false);
                }}
                disabled={loadingBeneficiarios}
              >
                {loadingBeneficiarios ? (
                  <ActivityIndicator size="small" color="#0E4A4C" />
                ) : (
                  <Text style={[styles.selectText, !beneficiario && styles.placeholderText]}>
                    {beneficiario ? beneficiario.nombre : 'Seleccionar beneficiario'}
                  </Text>
                )}
                {beneficiarioExpanded ? <ChevronUp color="#000" size={20} /> : <ChevronDown color="#000" size={20} />}
              </TouchableOpacity>

              {beneficiarioExpanded && (
                <View style={styles.dropdownOptionsList}>
                  {beneficiariosList.length === 0 ? (
                    <View style={styles.emptyOptionRow}>
                      <Text style={styles.emptyText}>No tienes beneficiarios registrados.</Text>
                      <TouchableOpacity
                        onPress={() => {
                          setBeneficiarioExpanded(false);
                          router.push('/(tabs)/beneficiarios');
                        }}
                        style={styles.emptyButton}
                      >
                        <Text style={styles.emptyButtonText}>Crear un beneficiario</Text>
                      </TouchableOpacity>
                    </View>
                  ) : (
                    beneficiariosList.map((item) => (
                      <TouchableOpacity
                        key={item.id}
                        style={styles.beneficiarioOptionRow}
                        onPress={() => {
                          setBeneficiario(item);
                          setBeneficiarioExpanded(false);
                          if (showValidationError) setShowValidationError(false);
                        }}
                      >
                        <View style={styles.optionTextWrapper}>
                          <Text style={styles.optionTitle}>{item.nombre}</Text>
                          <Text style={styles.optionSubtitle}>{item.parentesco}</Text>
                        </View>
                      </TouchableOpacity>
                    ))
                  )}
                </View>
              )}
            </View>

            {/* VALIDACIÓN DE CAMPOS (MOCKUP FRAME 31) */}
            {showValidationError && (
              <View style={styles.validationBox}>
                <AlertTriangle color="#A83232" size={18} style={styles.validationIcon} />
                <Text style={styles.validationText}>Completa todos los campos</Text>
              </View>
            )}

            {/* BOTÓN GUARDAR */}
            <TouchableOpacity
              style={styles.saveButton}
              onPress={handleSave}
              disabled={saving}
            >
              {saving ? (
                <ActivityIndicator color="#FFFFFF" />
              ) : (
                <Text style={styles.saveButtonText}>Guardar en bóveda</Text>
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
    backgroundColor: '#DAF8BD', // Fondo general verde claro
  },
  header: {
    borderBottomLeftRadius: 16,
    borderBottomRightRadius: 16,
    paddingHorizontal: 16,
    paddingBottom: 16,
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 8,
  },
  backButton: {
    padding: 4,
  },
  headerTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#FFFFFF',
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: 24,
    paddingTop: 20,
    paddingBottom: 40,
  },
  form: {
    flex: 1,
  },
  fieldLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1a2e2e',
    marginBottom: 8,
    marginLeft: 4,
  },
  input: {
    backgroundColor: '#FFFFFF',
    borderRadius: 8,
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
  dropdownContainer: {
    backgroundColor: '#FFFFFF',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    marginBottom: 16,
    overflow: 'hidden',
  },
  dropdownHeader: {
    height: 52,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#FFFFFF',
  },
  dropdownOptionsList: {
    backgroundColor: '#FFFFFF',
  },
  selectText: {
    fontSize: 15,
    fontFamily: 'MPLUS2-Bold',
    color: '#333',
  },
  placeholderText: {
    color: '#999',
  },
  tipoOptionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
  },
  simpleOptionRow: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    height: 48,
    justifyContent: 'center',
  },
  beneficiarioOptionRow: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    justifyContent: 'center',
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
  emptyOptionRow: {
    alignItems: 'center',
    paddingVertical: 16,
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
  },
  emptyText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#777',
    marginBottom: 12,
  },
  emptyButton: {
    backgroundColor: '#23856C',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 6,
  },
  emptyButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
  },
  // Estilos de la tarjeta dinámica
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
  // Dropdown anidado para tipo de cuenta
  nestedDropdownContainer: {
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E2E2E2',
    overflow: 'hidden',
  },
  nestedDropdownHeader: {
    height: 44,
    paddingHorizontal: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#FFFFFF',
  },
  nestedDropdownList: {
    backgroundColor: '#FFFFFF',
  },
  nestedDropdownOption: {
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderTopWidth: 1,
    borderTopColor: '#F0F0F0',
  },
  // Caja de archivo adjunto
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
    backgroundColor: '#D97706', // Naranja del mockup
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 6,
    shadowColor: '#D97706',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 2,
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
    shadowColor: '#42C167',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 3,
    marginTop: 8,
  },
  saveButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
  },
});
