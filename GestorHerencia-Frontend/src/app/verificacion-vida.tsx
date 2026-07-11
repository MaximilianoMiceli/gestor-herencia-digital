/**
 * @file verificacion-vida.tsx
 * @description Pantalla de configuración del sistema de Verificación de Vida (Frames 11, 37 y 39).
 *
 * Antes esta pantalla guardaba toda la configuración en SecureStore LOCAL, sin llamar
 * nunca al backend: el switch, la frecuencia y el contacto no tenían ningún efecto real,
 * porque es el backend (vía VerificacionVidaBackgroundService, un job que corre cada 24h)
 * quien decide cuándo escalar recordatorios y liberar la herencia. Ahora esta pantalla:
 *   - Al montarse, hace GET /api/verificacion-vida/configuracion para traer el estado real.
 *   - Al guardar, hace PUT /api/verificacion-vida/configuracion.
 *   - Tiene un botón de "Confirmar que sigo con vida" que llama a POST .../check-in.
 *
 * El "contacto de confianza" del backend es un Id de Usuario (ContactoConfianzaId), NO un
 * email de texto libre: por eso se eligió de una lista de beneficiarios ya ACEPTADOS
 * (con cuenta propia) en vez de tipearlo a mano.
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Switch,
  ActivityIndicator,
  Alert,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, ChevronDown, ChevronUp, AlertCircle } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService, BeneficiarioResumen } from '../services/assets.service';
import {
  VerificacionVidaService,
  MetodoNotificacion,
  EstadoVerificacionVida,
} from '../services/verificacion-vida.service';

/** Textos y color por cada valor de EstadoVerificacionVida del backend (ver el DTO). */
const ESTADO_INFO: Record<EstadoVerificacionVida, { label: string; color: string }> = {
  1: { label: 'Activo', color: '#39C55C' },
  2: { label: 'Recordatorio enviado', color: '#E2A53C' },
  3: { label: 'Esperando certificado', color: '#D97706' },
  4: { label: 'Certificado en revisión', color: '#005B9A' },
  5: { label: 'Fallecimiento confirmado', color: '#C53929' },
  6: { label: 'Herencia liberada', color: '#8A4B0C' },
};

/** Las únicas 3 frecuencias que el backend acepta (validado también del lado del servidor). */
const OPCIONES_FRECUENCIA: { label: string; value: number }[] = [
  { label: 'Cada 3 meses', value: 3 },
  { label: 'Cada 6 meses', value: 6 },
  { label: 'Cada 12 meses', value: 12 },
];

/** 1 = Push, 2 = Email, 3 = Sms (MetodoNotificacion del backend). */
const OPCIONES_METODO: { label: string; value: MetodoNotificacion }[] = [
  { label: 'Notificación push', value: 1 },
  { label: 'Email', value: 2 },
  { label: 'SMS', value: 3 },
];

export default function VerificacionVidaScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [checkingIn, setCheckingIn] = useState(false);

  // Estados de configuración (ya en las UNIDADES que el backend espera: frecuencia en
  // meses, método como código numérico, contacto como Id de usuario).
  const [isActive, setIsActive] = useState(false);
  const [frecuenciaMeses, setFrecuenciaMeses] = useState(3);
  const [metodo, setMetodo] = useState<MetodoNotificacion>(1);
  const [contactoConfianzaId, setContactoConfianzaId] = useState<number | null>(null);
  const [ultimoCheckIn, setUltimoCheckIn] = useState<string | null>(null);

  // Estado granular real del monitoreo (ver VerificacionVidaBackgroundService en el
  // backend): antes esta pantalla solo mostraba un switch binario Activo/Inactivo, sin
  // ninguna forma de ver si ya se envió un recordatorio o si el protocolo de
  // fallecimiento ya se activó.
  const [estado, setEstado] = useState<EstadoVerificacionVida>(1);
  const [recordatoriosEnviados, setRecordatoriosEnviados] = useState(0);
  const [fechaUltimoRecordatorio, setFechaUltimoRecordatorio] = useState<string | null>(null);
  const [fechaProtocoloActivado, setFechaProtocoloActivado] = useState<string | null>(null);

  // Beneficiarios elegibles como contacto de confianza: el backend exige que ya tengan
  // cuenta propia (usuarioBeneficiarioId != null) Y que hayan aceptado (estado === 2)
  // al menos una herencia de este titular.
  const [contactosElegibles, setContactosElegibles] = useState<BeneficiarioResumen[]>([]);

  // Control de visibilidad de los dropdowns (estilo inline condicional)
  const [showFrecuenciaDropdown, setShowFrecuenciaDropdown] = useState(false);
  const [showMetodoDropdown, setShowMetodoDropdown] = useState(false);
  const [showContactoDropdown, setShowContactoDropdown] = useState(false);

  // Cargar configuración real del backend + la lista de contactos elegibles al montar.
  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarDatos = async () => {
      try {
        const [config, beneficiarios] = await Promise.all([
          VerificacionVidaService.obtenerConfiguracion(),
          AssetsService.getMisBeneficiarios(),
        ]);

        setIsActive(config.activo);
        setFrecuenciaMeses(config.frecuenciaMeses);
        setMetodo(config.metodo);
        setContactoConfianzaId(config.contactoConfianzaId);
        setUltimoCheckIn(config.ultimoCheckIn);
        setEstado(config.estado);
        setRecordatoriosEnviados(config.recordatoriosEnviados);
        setFechaUltimoRecordatorio(config.fechaUltimoRecordatorio);
        setFechaProtocoloActivado(config.fechaProtocoloActivado);

        setContactosElegibles(
          beneficiarios.filter((b) => b.usuarioBeneficiarioId !== null && b.estado === 2)
        );
      } catch (err: any) {
        console.log('Error cargando verificación de vida:', err.message);
        Alert.alert('Error', 'No se pudo cargar la configuración de verificación de vida.');
      } finally {
        setLoading(false);
      }
    };

    cargarDatos();
  }, [token]);

  /**
   * Activar el switch exige, del lado del cliente, tener ya elegido un contacto (la
   * misma regla que el backend aplica al guardar): evita un viaje de red innecesario
   * que el servidor rechazaría igual con un 400.
   */
  const handleToggleSwitch = (value: boolean) => {
    if (value && !contactoConfianzaId) {
      Alert.alert(
        'Contacto requerido',
        'Elegí un contacto de confianza (un beneficiario que ya haya aceptado tu invitación) antes de activar el monitoreo.'
      );
      return;
    }
    setIsActive(value);
  };

  const handleGuardarConfiguracion = async () => {
    if (isActive && !contactoConfianzaId) {
      Alert.alert('Contacto requerido', 'Debés elegir un contacto de confianza para activar el monitoreo.');
      return;
    }

    setSaving(true);
    try {
      await VerificacionVidaService.guardarConfiguracion({
        activo: isActive,
        frecuenciaMeses,
        metodo,
        contactoConfianzaId,
      });

      Alert.alert(
        'Configuración guardada',
        'El sistema de verificación de vida se ha configurado con éxito.',
        [{ text: 'OK', onPress: () => router.replace('/') }]
      );
    } catch (err: any) {
      Alert.alert('Error', err.message || 'Ocurrió un error al guardar la configuración.');
    } finally {
      setSaving(false);
    }
  };

  /**
   * Confirma actividad ante el backend ("todavía estoy vivo"): resetea el reloj de
   * vencimiento del lado del servidor. Es la acción real equivalente a lo que, en un
   * caso de uso real, dispararía una notificación push que el usuario toca para
   * confirmar que sigue activo.
   */
  const handleCheckIn = async () => {
    setCheckingIn(true);
    try {
      const config = await VerificacionVidaService.registrarCheckIn();
      setUltimoCheckIn(config.ultimoCheckIn);
      setEstado(config.estado);
      setRecordatoriosEnviados(config.recordatoriosEnviados);
      setFechaUltimoRecordatorio(config.fechaUltimoRecordatorio);
      setFechaProtocoloActivado(config.fechaProtocoloActivado);
      Alert.alert('¡Listo!', 'Se registró tu actividad. El reloj de verificación se reinició.');
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo registrar el check-in.');
    } finally {
      setCheckingIn(false);
    }
  };

  const contactoSeleccionado = contactosElegibles.find(
    (c) => c.usuarioBeneficiarioId === contactoConfianzaId
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando configuración...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* HEADER CON GRADIENTE */}
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
          <Text style={styles.headerTitle}>Verificación de vida</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>

          {/* CARD DE ESTADO (Frame 11/37/39) */}
          <View style={styles.statusCard}>
            <View style={styles.statusTextContainer}>
              <Text style={styles.statusLabel}>Estado</Text>
              <Text style={[styles.statusValue, { color: isActive ? ESTADO_INFO[estado].color : '#8A9E95' }]}>
                {isActive ? ESTADO_INFO[estado].label : 'Inactivo'}
              </Text>
              {ultimoCheckIn && (
                <Text style={styles.lastCheckInText}>
                  Último check-in: {new Date(ultimoCheckIn).toLocaleDateString('es-AR')}
                </Text>
              )}
            </View>
            <Switch
              value={isActive}
              onValueChange={handleToggleSwitch}
              trackColor={{ false: '#CCD3CE', true: '#39C55C' }}
              thumbColor="#FFFFFF"
              ios_backgroundColor="#CCD3CE"
            />
          </View>

          {/* DETALLE DEL PROCESO: solo tiene sentido mostrarlo si ya se envió al menos un
              recordatorio o si el protocolo de fallecimiento ya se activó. Antes esta
              información existía en el backend pero era invisible para el usuario, que
              solo veía "Activo/Inactivo" sin saber si ya estaba en escalamiento. */}
          {isActive && (recordatoriosEnviados > 0 || fechaProtocoloActivado) && (
            <View style={styles.detailCard}>
              <Text style={styles.detailRow}>
                Recordatorios enviados: <Text style={styles.detailBold}>{recordatoriosEnviados}</Text>
              </Text>
              {fechaUltimoRecordatorio && (
                <Text style={styles.detailRow}>
                  Último recordatorio: <Text style={styles.detailBold}>{new Date(fechaUltimoRecordatorio).toLocaleDateString('es-AR')}</Text>
                </Text>
              )}
              {fechaProtocoloActivado && (
                <Text style={[styles.detailRow, styles.detailWarning]}>
                  Protocolo de fallecimiento activado el{' '}
                  <Text style={styles.detailBold}>{new Date(fechaProtocoloActivado).toLocaleDateString('es-AR')}</Text>
                </Text>
              )}
            </View>
          )}

          {/* BOTÓN DE CHECK-IN: confirma actividad real contra el backend */}
          <TouchableOpacity style={styles.checkInButton} onPress={handleCheckIn} disabled={checkingIn}>
            {checkingIn ? (
              <ActivityIndicator color="#FFFFFF" />
            ) : (
              <Text style={styles.checkInButtonText}>Confirmar que sigo con vida</Text>
            )}
          </TouchableOpacity>

          {/* CAMPO: FRECUENCIA DE CHEQUEO (DROPDOWN CON HIGHLIGHT) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Frecuencia de chequeo</Text>
            <TouchableOpacity
              style={[styles.dropdownButton, showFrecuenciaDropdown && styles.dropdownButtonActive]}
              activeOpacity={0.8}
              onPress={() => {
                setShowFrecuenciaDropdown(!showFrecuenciaDropdown);
                setShowMetodoDropdown(false);
                setShowContactoDropdown(false);
              }}
            >
              <Text style={[styles.dropdownButtonText, showFrecuenciaDropdown && styles.dropdownButtonTextActive]}>
                {OPCIONES_FRECUENCIA.find((f) => f.value === frecuenciaMeses)?.label}
              </Text>
              {showFrecuenciaDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
            </TouchableOpacity>

            {showFrecuenciaDropdown && (
              <View style={styles.dropdownInlineList}>
                {OPCIONES_FRECUENCIA.map((f) => (
                  <TouchableOpacity
                    key={f.value}
                    style={[
                      styles.dropdownInlineOption,
                      frecuenciaMeses === f.value && styles.dropdownInlineOptionSelected,
                    ]}
                    onPress={() => {
                      setFrecuenciaMeses(f.value);
                      setShowFrecuenciaDropdown(false);
                    }}
                  >
                    <Text
                      style={[
                        styles.dropdownInlineOptionText,
                        frecuenciaMeses === f.value && styles.dropdownInlineOptionTextSelected,
                      ]}
                    >
                      {f.label}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </View>

          {/* CAMPO: MÉTODO DE VERIFICACIÓN (DROPDOWN CON HIGHLIGHT) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Metodo de verificacion</Text>
            <TouchableOpacity
              style={[styles.dropdownButton, showMetodoDropdown && styles.dropdownButtonActive]}
              activeOpacity={0.8}
              onPress={() => {
                setShowMetodoDropdown(!showMetodoDropdown);
                setShowFrecuenciaDropdown(false);
                setShowContactoDropdown(false);
              }}
            >
              <Text style={[styles.dropdownButtonText, showMetodoDropdown && styles.dropdownButtonTextActive]}>
                {OPCIONES_METODO.find((m) => m.value === metodo)?.label}
              </Text>
              {showMetodoDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
            </TouchableOpacity>

            {showMetodoDropdown && (
              <View style={styles.dropdownInlineList}>
                {OPCIONES_METODO.map((m) => (
                  <TouchableOpacity
                    key={m.value}
                    style={[
                      styles.dropdownInlineOption,
                      metodo === m.value && styles.dropdownInlineOptionSelected,
                    ]}
                    onPress={() => {
                      setMetodo(m.value);
                      setShowMetodoDropdown(false);
                    }}
                  >
                    <Text
                      style={[
                        styles.dropdownInlineOptionText,
                        metodo === m.value && styles.dropdownInlineOptionTextSelected,
                      ]}
                    >
                      {m.label}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </View>

          {/* CAMPO: CONTACTO DE CONFIANZA (elegido de la lista real de beneficiarios
              aceptados, no tipeado a mano: el backend espera un Id de Usuario) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Contacto de confianza</Text>
            <TouchableOpacity
              style={[styles.dropdownButton, showContactoDropdown && styles.dropdownButtonActive]}
              activeOpacity={0.8}
              onPress={() => {
                setShowContactoDropdown(!showContactoDropdown);
                setShowFrecuenciaDropdown(false);
                setShowMetodoDropdown(false);
              }}
            >
              <Text
                style={[
                  styles.dropdownButtonText,
                  showContactoDropdown && styles.dropdownButtonTextActive,
                  !contactoSeleccionado && styles.placeholderText,
                ]}
              >
                {contactoSeleccionado ? contactoSeleccionado.email : 'Seleccionar contacto'}
              </Text>
              {showContactoDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
            </TouchableOpacity>

            {showContactoDropdown && (
              <View style={styles.dropdownInlineList}>
                {contactosElegibles.length === 0 ? (
                  <View style={styles.dropdownEmptyContainer}>
                    <Text style={styles.dropdownEmptyText}>
                      Todavía no tenés beneficiarios que hayan aceptado tu invitación.
                    </Text>
                  </View>
                ) : (
                  contactosElegibles.map((c) => (
                    <TouchableOpacity
                      key={c.usuarioBeneficiarioId}
                      style={[
                        styles.dropdownInlineOption,
                        contactoConfianzaId === c.usuarioBeneficiarioId && styles.dropdownInlineOptionSelected,
                      ]}
                      onPress={() => {
                        setContactoConfianzaId(c.usuarioBeneficiarioId);
                        setShowContactoDropdown(false);
                      }}
                    >
                      <Text
                        style={[
                          styles.dropdownInlineOptionText,
                          contactoConfianzaId === c.usuarioBeneficiarioId && styles.dropdownInlineOptionTextSelected,
                        ]}
                      >
                        {c.email}
                      </Text>
                    </TouchableOpacity>
                  ))
                )}
              </View>
            )}
          </View>

          {/* MENSAJE DE ADVERTENCIA */}
          <View style={styles.infoCard}>
            <AlertCircle size={20} color="#0B4B5C" style={styles.infoIcon} />
            <Text style={styles.infoText}>
              Si no respondes en el plazo configurado, tu contacto de confianza sera notificado antes de activar la herencia.
            </Text>
          </View>

          {/* BOTÓN GUARDAR CONFIGURACIÓN */}
          <TouchableOpacity
            style={styles.saveButton}
            onPress={handleGuardarConfiguracion}
            disabled={saving}
          >
            {saving ? (
              <ActivityIndicator size="small" color="#FFFFFF" />
            ) : (
              <Text style={styles.saveButtonText}>Guardar configuración</Text>
            )}
          </TouchableOpacity>

        </View>
      </ScrollView>
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
  statusCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  statusTextContainer: {
    gap: 4,
  },
  statusLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  statusValue: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
  },
  statusActive: {
    color: '#39C55C', // Verde activo Figma
  },
  statusInactive: {
    color: '#8A9E95',
  },
  lastCheckInText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 11,
    color: '#8A9E95',
    marginTop: 2,
  },
  detailCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 16,
    gap: 6,
  },
  detailRow: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#5E746A',
  },
  detailBold: {
    fontFamily: 'MPLUS2-Bold',
    color: '#1a2e2e',
  },
  detailWarning: {
    color: '#C53929',
  },
  checkInButton: {
    backgroundColor: '#02213D',
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
  },
  checkInButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
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
  placeholderText: {
    color: '#8A9E95',
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
  dropdownButtonText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#000000',
  },
  dropdownButtonActive: {
    borderColor: '#2E7D32', // Borde verde oscuro activo
    borderWidth: 1.5,
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
  },
  dropdownInlineOption: {
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8, // Esquinas más suaves para la selección interna
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
  dropdownEmptyContainer: {
    padding: 12,
    alignItems: 'center',
  },
  dropdownEmptyText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
    textAlign: 'center',
  },
  infoCard: {
    backgroundColor: '#CDE5E9', // Fondo celeste/azul claro (Figma Frame 11)
    borderWidth: 1,
    borderColor: '#7FAAB5',
    borderRadius: 16,
    padding: 16,
    flexDirection: 'row',
    gap: 12,
    alignItems: 'flex-start',
  },
  infoIcon: {
    marginTop: 2,
  },
  infoText: {
    flex: 1,
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#0B4B5C',
    lineHeight: 18,
  },
  saveButton: {
    backgroundColor: '#39C55C', // Verde Figma
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
});
