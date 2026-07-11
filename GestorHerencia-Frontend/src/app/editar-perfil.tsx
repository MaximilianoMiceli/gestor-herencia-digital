/**
 * @file editar-perfil.tsx
 * @description Pantalla de "Editar Perfil / Seguridad" de la cuenta.
 *
 * Antes no existía ninguna pantalla para esto, pese a que el backend ya exponía
 * PUT /api/usuarios/{id} (editar nombre/email) y PUT /api/usuarios/{id}/password
 * (cambiar contraseña). Ahora también incluye el toggle real de 2FA por email
 * (PUT /api/usuarios/{id}/doble-factor), reemplazando la fila puramente decorativa
 * que mostraba el Dashboard.
 */

import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  Switch,
  ActivityIndicator,
  Alert,
  ScrollView,
  LayoutChangeEvent,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { ArrowLeft } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { UsuariosService } from '../services/usuarios.service';
import { parsearFechaDDMMAAAA, formatearFechaDDMMAAAA, calcularEdad } from '../utils/fecha';

const DNI_REGEX = /^\d{7,8}$/;

export default function EditarPerfilScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { userId } = useAuth();
  const { focus } = useLocalSearchParams<{ focus?: string }>();

  // Referencia para poder saltar directo a la sección de 2FA cuando se llega acá desde
  // el Dashboard con "?focus=2fa" (ver dashboard.tsx): sin esto, el usuario aterrizaba
  // siempre arriba de todo y tenía que scrollear a mano pasando "Datos personales" y
  // "Cambiar contraseña" para encontrar el toggle real de 2FA.
  const scrollRef = useRef<ScrollView>(null);

  const [loading, setLoading] = useState(true);

  // Sección 1: datos de perfil
  const [nombre, setNombre] = useState('');
  const [email, setEmail] = useState('');
  const [dni, setDni] = useState('');
  const [fechaNacimientoTexto, setFechaNacimientoTexto] = useState('');
  const [savingPerfil, setSavingPerfil] = useState(false);

  // Sección 2: cambio de contraseña
  const [passwordActual, setPasswordActual] = useState('');
  const [passwordNueva, setPasswordNueva] = useState('');
  const [confirmarPassword, setConfirmarPassword] = useState('');
  const [savingPassword, setSavingPassword] = useState(false);

  // Sección 3: 2FA
  const [dobleFactorHabilitado, setDobleFactorHabilitado] = useState(false);
  const [savingDobleFactor, setSavingDobleFactor] = useState(false);

  useEffect(() => {
    if (!userId) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarPerfil = async () => {
      try {
        const usuario = await UsuariosService.obtenerPorId(userId);
        setNombre(usuario.nombre);
        setEmail(usuario.email);
        setDni(usuario.dni);
        setFechaNacimientoTexto(formatearFechaDDMMAAAA(new Date(usuario.fechaNacimiento)));
        setDobleFactorHabilitado(usuario.dobleFactorHabilitado);
      } catch (err: any) {
        Alert.alert('Error', err.message || 'No se pudo cargar el perfil.');
      } finally {
        setLoading(false);
      }
    };

    cargarPerfil();
  }, [userId]);

  const handleGuardarPerfil = async () => {
    if (!userId) return;
    if (!nombre.trim() || !email.trim() || !dni.trim() || !fechaNacimientoTexto.trim()) {
      Alert.alert('Campos requeridos', 'Nombre, email, DNI y fecha de nacimiento no pueden estar vacíos.');
      return;
    }

    // Mismas reglas que register.tsx y que UsuarioService.ActualizarUsuarioAsync en el
    // backend: se valida acá también para dar feedback inmediato sin esperar la red.
    if (!DNI_REGEX.test(dni.trim())) {
      Alert.alert('Error', 'El DNI debe tener 7 u 8 dígitos numéricos.');
      return;
    }

    const fechaNacimiento = parsearFechaDDMMAAAA(fechaNacimientoTexto);
    if (!fechaNacimiento) {
      Alert.alert('Error', 'Ingresá una fecha de nacimiento válida (DD/MM/AAAA).');
      return;
    }
    if (calcularEdad(fechaNacimiento) < 18) {
      Alert.alert('Error', 'Debés ser mayor de edad (18 años).');
      return;
    }

    // Se arma el ISO "AAAA-MM-DD" a mano (no con toISOString(), que convierte a UTC
    // primero y puede correr la fecha un día: ver el comentario detallado en
    // register.tsx, donde se resolvió el mismo problema).
    const anio = fechaNacimiento.getFullYear();
    const mes = String(fechaNacimiento.getMonth() + 1).padStart(2, '0');
    const dia = String(fechaNacimiento.getDate()).padStart(2, '0');

    setSavingPerfil(true);
    try {
      await UsuariosService.actualizarPerfil(userId, nombre.trim(), email.trim(), dni.trim(), `${anio}-${mes}-${dia}`);
      Alert.alert('Perfil actualizado', 'Tus datos se guardaron con éxito.');
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo actualizar el perfil.');
    } finally {
      setSavingPerfil(false);
    }
  };

  const handleCambiarPassword = async () => {
    if (!userId) return;
    if (!passwordActual || !passwordNueva || !confirmarPassword) {
      Alert.alert('Campos requeridos', 'Completá tu contraseña actual y la nueva (dos veces).');
      return;
    }
    if (passwordNueva !== confirmarPassword) {
      Alert.alert('Error', 'La nueva contraseña y su confirmación no coinciden.');
      return;
    }

    setSavingPassword(true);
    try {
      // 204 No Content en éxito: no hay body que leer, solo confirmar que no lanzó.
      await UsuariosService.cambiarPassword(userId, passwordActual, passwordNueva);
      Alert.alert('Contraseña actualizada', 'Tu contraseña se cambió con éxito.');
      setPasswordActual('');
      setPasswordNueva('');
      setConfirmarPassword('');
    } catch (err: any) {
      // El backend responde acá, por ejemplo, "La contraseña actual ingresada es incorrecta."
      Alert.alert('Error', err.message || 'No se pudo cambiar la contraseña.');
    } finally {
      setSavingPassword(false);
    }
  };

  const handleToggleDobleFactor = async (value: boolean) => {
    if (!userId) return;

    setSavingDobleFactor(true);
    try {
      const usuarioActualizado = await UsuariosService.actualizarDobleFactor(userId, value);
      setDobleFactorHabilitado(usuarioActualizado.dobleFactorHabilitado);
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo actualizar la verificación en dos pasos.');
    } finally {
      setSavingDobleFactor(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando perfil...</Text>
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
          <Text style={styles.headerTitle}>Editar perfil</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView ref={scrollRef} contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>

          {/* SECCIÓN: DATOS DE PERFIL */}
          <Text style={styles.sectionTitle}>DATOS PERSONALES</Text>
          <View style={styles.card}>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Nombre completo</Text>
              <TextInput style={styles.textInput} value={nombre} onChangeText={setNombre} />
            </View>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Email</Text>
              <TextInput
                style={styles.textInput}
                value={email}
                onChangeText={setEmail}
                keyboardType="email-address"
                autoCapitalize="none"
              />
            </View>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>DNI</Text>
              <TextInput
                style={styles.textInput}
                value={dni}
                onChangeText={setDni}
                keyboardType="number-pad"
                maxLength={8}
              />
            </View>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Fecha de nacimiento</Text>
              <TextInput
                style={styles.textInput}
                value={fechaNacimientoTexto}
                onChangeText={setFechaNacimientoTexto}
                placeholder="DD/MM/AAAA"
                placeholderTextColor="#8A9E95"
                maxLength={10}
              />
            </View>
            <TouchableOpacity style={styles.saveButton} onPress={handleGuardarPerfil} disabled={savingPerfil}>
              {savingPerfil ? (
                <ActivityIndicator size="small" color="#FFFFFF" />
              ) : (
                <Text style={styles.saveButtonText}>Guardar datos</Text>
              )}
            </TouchableOpacity>
          </View>

          {/* SECCIÓN: CAMBIAR CONTRASEÑA */}
          <Text style={styles.sectionTitle}>CAMBIAR CONTRASEÑA</Text>
          <View style={styles.card}>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Contraseña actual</Text>
              <TextInput
                style={styles.textInput}
                value={passwordActual}
                onChangeText={setPasswordActual}
                secureTextEntry
              />
            </View>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Nueva contraseña</Text>
              <TextInput
                style={styles.textInput}
                value={passwordNueva}
                onChangeText={setPasswordNueva}
                secureTextEntry
              />
            </View>
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Confirmar nueva contraseña</Text>
              <TextInput
                style={styles.textInput}
                value={confirmarPassword}
                onChangeText={setConfirmarPassword}
                secureTextEntry
              />
            </View>
            <TouchableOpacity style={styles.saveButton} onPress={handleCambiarPassword} disabled={savingPassword}>
              {savingPassword ? (
                <ActivityIndicator size="small" color="#FFFFFF" />
              ) : (
                <Text style={styles.saveButtonText}>Cambiar contraseña</Text>
              )}
            </TouchableOpacity>
          </View>

          {/* SECCIÓN: VERIFICACIÓN EN DOS PASOS (2FA). El "onLayout" mide en qué posición
              Y quedó esta sección dentro del ScrollView para poder saltar directo acá
              cuando se llega con "?focus=2fa" desde el Dashboard (ver más abajo). */}
          <View
            onLayout={(e: LayoutChangeEvent) => {
              if (focus === '2fa') {
                scrollRef.current?.scrollTo({ y: e.nativeEvent.layout.y, animated: true });
              }
            }}
          >
            <Text style={styles.sectionTitle}>SEGURIDAD DE LA CUENTA</Text>
            <View style={styles.card}>
            <View style={styles.switchRow}>
              <View style={{ flex: 1 }}>
                <Text style={styles.switchLabel}>Verificación en dos pasos por email</Text>
                <Text style={styles.switchHelper}>
                  {dobleFactorHabilitado
                    ? 'Activa: te vamos a pedir un código por email en cada inicio de sesión.'
                    : 'Inactiva: iniciás sesión solo con tu contraseña.'}
                </Text>
              </View>
              {savingDobleFactor ? (
                <ActivityIndicator size="small" color="#23856C" />
              ) : (
                <Switch
                  value={dobleFactorHabilitado}
                  onValueChange={handleToggleDobleFactor}
                  trackColor={{ false: '#CCD3CE', true: '#39C55C' }}
                  thumbColor="#FFFFFF"
                  ios_backgroundColor="#CCD3CE"
                />
              )}
            </View>
          </View>
          </View>

        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
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
    gap: 12,
  },
  sectionTitle: {
    fontSize: 13,
    fontFamily: 'MPLUS2-Bold',
    color: '#5E746A',
    letterSpacing: 0.8,
    marginTop: 12,
    marginBottom: 4,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 20,
    gap: 16,
  },
  inputGroup: {
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
  saveButton: {
    backgroundColor: '#39C55C',
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  saveButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  switchLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#1a2e2e',
    marginBottom: 4,
  },
  switchHelper: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 12,
    color: '#8A9E95',
    lineHeight: 16,
  },
});
