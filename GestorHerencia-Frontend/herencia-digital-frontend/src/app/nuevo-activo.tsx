/**
 * @file nuevo-activo.tsx
 * @description Pantalla de creación y registro de nuevos activos digitales.
 * 
 * Implementa un formulario modular e interactivo fiel a las maquetas de Figma.
 * Utiliza selectores dropdown con diseño idéntico al de Verificación de Vida (Frame 11):
 * - Botón de cabecera en forma de cápsula con borde negro fino (#1A202C).
 * - Borde verde destacado (#2E7D32) cuando el selector está abierto.
 * - Tarjeta de opciones flotante con bordes redondeados (borderRadius: 20) y borde verde suave.
 * - Opción seleccionada con fondo verde pastel (#DAF8BD) y texto verde negrita (#2E7D32).
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

type AssetType = {
  id: number;
  label: string;
  description: string;
  icon: any;
};

const ASSET_TYPES: AssetType[] = [
  { id: 2, label: 'Cripto', description: 'Wallets, Bitcoin, Ethereum..', icon: Bitcoin },
  { id: 0, label: 'Cuenta bancaria', description: 'CBU, alias, numero de cu..', icon: Landmark },
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
  const [beneficiario, setBeneficiario] = useState<BeneficiarioDTO | null>(null);

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

  // Archivo:
  const [archivoAdjunto, setArchivoAdjunto] = useState<string | null>(null);
  const [loadingArchivo, setLoadingArchivo] = useState(false);

  // AUXILIARES
  const [beneficiariosList, setBeneficiariosList] = useState<BeneficiarioDTO[]>([]);
  const [loadingBeneficiarios, setLoadingBeneficiarios] = useState(true);
  const [saving, setSaving] = useState(false);

  // Control de expansión
  const [tipoExpanded, setTipoExpanded] = useState(false);
  const [prioridadExpanded, setPrioridadExpanded] = useState(false);
  const [beneficiarioExpanded, setBeneficiarioExpanded] = useState(false);

  const [showValidationError, setShowValidationError] = useState(false);

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

  const handleAttachFile = () => {
    setLoadingArchivo(true);
    setTimeout(() => {
      setArchivoAdjunto('contrato_digital.pdf');
      setLoadingArchivo(false);
    }, 1000);
  };

  const handleSave = async () => {
    if (!nombre || !tipo || !beneficiario) {
      setShowValidationError(true);
      return;
    }

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

      let descripcionFinal = '';
      if (tipo.label === 'Cripto') {
        descripcionFinal = `[CRIPTO] Blockchain: ${blockchain} | Wallet: ${wallet} | Clave Privada: ${clavePrivada}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Cuenta bancaria') {
        descripcionFinal = `[BANCO] Banco: ${banco} | Cuenta: ${numeroCuenta} | CBU/Alias: ${cbuAlias} | Tipo: ${tipoCuenta}\n\nInstrucciones:\n${instrucciones}`;
      } else if (tipo.label === 'Archivo') {
        descripcionFinal = `[ARCHIVO] Adjunto: ${archivoAdjunto}\n\nInstrucciones:\n${instrucciones}`;
      }

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
      {/* HEADER DE ALTA FIDELIDAD CON GRADIENTE */}
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
                  setBeneficiarioExpanded(false);
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
                  setBeneficiarioExpanded(false);
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

            {/* BENEFICIARIO SELECT */}
            <Text style={styles.fieldLabel}>Beneficiario asignado</Text>
            <View style={styles.dropdownContainer}>
              <TouchableOpacity
                style={[
                  styles.dropdownHeader,
                  beneficiarioExpanded && styles.dropdownHeaderActive
                ]}
                activeOpacity={0.8}
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
                  <Text style={[
                    styles.selectText,
                    beneficiarioExpanded && styles.selectTextActive,
                    !beneficiario && styles.placeholderText
                  ]}>
                    {beneficiario ? beneficiario.nombre : 'Seleccionar beneficiario'}
                  </Text>
                )}
                {beneficiarioExpanded ? (
                  <ChevronUp color="#2E7D32" size={20} />
                ) : (
                  <ChevronDown color="#000000" size={20} />
                )}
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
                    beneficiariosList.map((item) => {
                      const isSelected = beneficiario?.id === item.id;
                      return (
                        <TouchableOpacity
                          key={item.id}
                          style={[
                            styles.beneficiarioOptionRow,
                            isSelected && styles.optionSelected
                          ]}
                          onPress={() => {
                            setBeneficiario(item);
                            setBeneficiarioExpanded(false);
                            if (showValidationError) setShowValidationError(false);
                          }}
                        >
                          <View style={styles.optionTextWrapper}>
                            <Text style={[
                              styles.optionTitle,
                              isSelected && styles.optionSelectedText
                            ]}>{item.nombre}</Text>
                            <Text style={styles.optionSubtitle}>{item.email}</Text>
                          </View>
                        </TouchableOpacity>
                      );
                    })
                  )}
                </View>
              )}
            </View>

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
    backgroundColor: '#DAF8BD', // Fondo verde claro pastel Figma
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
    borderColor: '#C1E3A4', // Borde verde claro suave
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
    marginBottom: 16,
  },
  dropdownHeader: {
    height: 52,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado (rectangular suave)
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
  },
  dropdownHeaderActive: {
    borderColor: '#2E7D32',
    borderWidth: 1.5,
  },
  dropdownOptionsList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
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
    borderRadius: 8, // Esquinas más suaves para la selección interna
  },
  simpleOptionRow: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8, // Esquinas más suaves
    height: 48,
    justifyContent: 'center',
  },
  beneficiarioOptionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8, // Esquinas más suaves
    justifyContent: 'center',
  },
  optionSelected: {
    backgroundColor: '#DAF8BD', // Fondo verde claro de selección
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
  emptyOptionRow: {
    alignItems: 'center',
    paddingVertical: 16,
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
    borderColor: '#C1E3A4', // Borde verde claro suave
  },
  nestedDropdownHeaderActive: {
    borderColor: '#2E7D32',
    borderWidth: 1.5,
  },
  nestedDropdownList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    marginTop: 4,
    overflow: 'hidden',
  },
  nestedDropdownOption: {
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderRadius: 8, // Esquinas más suaves para selección interna
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
