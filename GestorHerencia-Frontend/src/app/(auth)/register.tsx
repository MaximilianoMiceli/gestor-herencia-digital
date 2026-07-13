/**
 * @file register.tsx
 * @description Pantalla pública de registro de cuentas (Register Screen).
 * 
 * Permite a los nuevos usuarios crear una cuenta en el sistema ingresando
 * su nombre, dirección de correo electrónico y contraseña.
 * Tras un registro exitoso, se notifica al usuario y se le redirige a la
 * pantalla de inicio de sesión (`/login`) para completar el flujo.
 */

import React, { useState } from 'react';
import { View, StyleSheet, Text, Alert, ScrollView, KeyboardAvoidingView, Platform } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { AuthService } from '../../services/auth.service';
import { parsearFechaDDMMAAAA, calcularEdad } from '../../utils/fecha';

const DNI_REGEX = /^\d{7,8}$/;

export default function RegisterScreen() {
  const router = useRouter();
  const { email: initialEmail, acceptInvitationId } = useLocalSearchParams<{ email?: string; acceptInvitationId?: string }>();

  const [nombre, setNombre] = useState('');
  const [email, setEmail] = useState(initialEmail || '');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [dni, setDni] = useState('');
  const [fechaNacimientoTexto, setFechaNacimientoTexto] = useState('');
  const [loading, setLoading] = useState(false);

  /**
   * Valida los campos localmente y realiza la llamada de registro al servidor.
   */
  const handleRegister = async () => {
    if (!nombre || !email || !password || !confirmPassword || !dni || !fechaNacimientoTexto) {
      Alert.alert('Error', 'Todos los campos son obligatorios.');
      return;
    }

    if (password !== confirmPassword) {
      Alert.alert('Error', 'Las contraseñas no coinciden.');
      return;
    }

    // Mismo criterio que UsuarioService.CrearUsuarioAsync en el backend: se valida acá
    // también para avisar sin esperar el viaje de red.
    if (!DNI_REGEX.test(dni.trim())) {
      Alert.alert('Error', 'El DNI debe tener 7 u 8 dígitos numéricos.');
      return;
    }

    // Misma regla que ValidarFechaNacimiento en el backend (fecha de calendario real +
    // mayoría de edad).
    const fechaNacimiento = parsearFechaDDMMAAAA(fechaNacimientoTexto);
    if (!fechaNacimiento) {
      Alert.alert('Error', 'Ingresá una fecha de nacimiento válida (DD/MM/AAAA).');
      return;
    }
    if (calcularEdad(fechaNacimiento) < 18) {
      Alert.alert('Error', 'Debés ser mayor de edad (18 años) para registrarte.');
      return;
    }

    setLoading(true);
    try {
      // Se arma el string "AAAA-MM-DD" a mano desde los componentes locales, NUNCA con
      // `fechaNacimiento.toISOString()`: ese método convierte a UTC primero, y en
      // Argentina (UTC-3) eso puede correr la fecha un día hacia atrás (ej. medianoche
      // del 01/01/1990 local cae en 1989-12-31 UTC).
      const anio = fechaNacimiento.getFullYear();
      const mes = String(fechaNacimiento.getMonth() + 1).padStart(2, '0');
      const dia = String(fechaNacimiento.getDate()).padStart(2, '0');

      await AuthService.register({
        nombre,
        email,
        password,
        dni: dni.trim(),
        fechaNacimiento: `${anio}-${mes}-${dia}`,
      });

      Alert.alert('Éxito', 'Cuenta creada correctamente. Por favor, inicia sesión.', [
        { 
          text: 'OK', 
          onPress: () => router.replace({
            pathname: '/(auth)/login',
            params: { acceptInvitationId }
          }) 
        }
      ]);
    } catch (error: any) {
      Alert.alert('Error al registrar', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    // behavior "padding" solo en iOS: es la plataforma donde el teclado tapa los inputs
    // sin ese ajuste (Android ya lo resuelve con el modo de ventana por defecto).
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView contentContainerStyle={styles.scrollContainer}>
        <View style={styles.container}>
          <View style={styles.logoContainer}>
            <Text style={styles.titlePrefix}>Gestor de</Text>
            <GradientText text="Herencia Digital" style={styles.titleGradient} />
            <View style={styles.lockWrapper}>
              <LockLogo size={70} />
            </View>
          </View>

          <View style={styles.formContainer}>
            <AuthInput 
              placeholder="Nombre completo" 
              value={nombre}
              onChangeText={setNombre}
            />
            <AuthInput
              placeholder="Email"
              keyboardType="email-address"
              value={email}
              onChangeText={setEmail}
            />
            <AuthInput
              placeholder="DNI"
              keyboardType="number-pad"
              maxLength={8}
              value={dni}
              onChangeText={setDni}
            />
            <AuthInput
              placeholder="Fecha de nacimiento (DD/MM/AAAA)"
              maxLength={10}
              value={fechaNacimientoTexto}
              onChangeText={setFechaNacimientoTexto}
            />
            <AuthInput
              placeholder="Contraseña"
              secureTextEntry
              value={password}
              onChangeText={setPassword}
            />
            <AuthInput 
              placeholder="Confirmar contraseña" 
              secureTextEntry
              value={confirmPassword}
              onChangeText={setConfirmPassword}
            />

            <View style={styles.buttonWrapper}>
              <AuthButton 
                title="Crear cuenta" 
                onPress={handleRegister} 
                loading={loading}
              />
            </View>
          </View>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  scrollContainer: {
    flexGrow: 1,
  },
  container: {
    flex: 1,
    paddingHorizontal: 24,
    paddingTop: 60,
    paddingBottom: 40,
  },
  logoContainer: {
    alignItems: 'center',
    marginBottom: 30,
  },
  titlePrefix: {
    fontSize: 16,
    fontFamily: 'MPLUS2-Regular',
    color: '#DF5173',
    marginBottom: -5,
  },
  titleGradient: {
    fontSize: 28,
    fontFamily: 'MPLUS2-Regular',
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    paddingTop: 8,
    marginTop: 8,
    marginBottom: 20,
  },
  lockWrapper: {
    shadowColor: '#874BE5',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 15,
    elevation: 4,
  },
  formContainer: {
    width: '100%',
  },
  buttonWrapper: {
    marginTop: 8,
  }
});
