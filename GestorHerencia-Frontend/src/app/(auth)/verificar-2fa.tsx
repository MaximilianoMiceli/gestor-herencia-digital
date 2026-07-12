/**
 * @file verificar-2fa.tsx
 * @description Segundo paso del login cuando el usuario tiene 2FA por email habilitado.
 *
 * login.tsx navega acá (en vez de completar la sesión directamente) cuando
 * POST /api/auth/login responde `requiereDobleFactor: true`. En ese punto el backend
 * YA generó un código de 6 dígitos y lo "envió" (simulado, impreso por consola del
 * SERVIDOR) al email del usuario; esta pantalla existe únicamente para juntar ese
 * código y completar el login llamando a POST /api/auth/verificar-doble-factor.
 */

import React, { useState } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { ArrowLeft } from 'lucide-react-native';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { useAuth } from '../../context/AuthContext';
import { AuthService } from '../../services/auth.service';
import { InvitacionesService } from '../../services/invitaciones.service';

export default function Verificar2FAScreen() {
  const router = useRouter();
  const { signIn } = useAuth();
  // "usuarioId" identifica a quién pertenece el código pendiente (login.tsx lo pasa por
  // parámetro porque todavía no hay sesión ni token con el que identificar al usuario).
  // "acceptInvitationId" viaja igual que en login.tsx: si el usuario llegó a loguearse
  // a partir de un link de invitación, se retoma acá para aceptarla automáticamente
  // apenas termine este segundo paso.
  const { usuarioId, acceptInvitationId } = useLocalSearchParams<{
    usuarioId?: string;
    acceptInvitationId?: string;
  }>();

  const [codigo, setCodigo] = useState('');
  const [loading, setLoading] = useState(false);

  /**
   * Envía el código de 6 dígitos al backend para completar el login iniciado en
   * login.tsx. Si falta el "usuarioId" (por ejemplo, se llegó a esta ruta directo,
   * sin pasar por el login), no hay con qué verificar nada y se manda de vuelta.
   */
  const handleVerificar = async () => {
    if (!usuarioId) {
      Alert.alert('Error', 'Faltan datos del login. Volvé a intentar iniciar sesión.');
      router.replace('/(auth)/login');
      return;
    }
    if (!codigo.trim()) {
      Alert.alert('Error', 'Ingresá el código de 6 dígitos que te enviamos por email.');
      return;
    }

    setLoading(true);
    try {
      const response = await AuthService.verificarDobleFactor(Number(usuarioId), codigo.trim());

      // Igual que en login.tsx: se persiste el token PRIMERO, para que el interceptor
      // de Axios (ver api.ts) ya pueda adjuntarlo al aceptar la invitación pendiente.
      await signIn(response.token);

      if (acceptInvitationId) {
        try {
          await InvitacionesService.procesar(acceptInvitationId, 'aceptar');
        } catch (e) {
          // No se interrumpe el login por esto: el usuario ya quedó autenticado y
          // puede aceptar la invitación más tarde a mano desde "Mis herencias".
          console.error('Error al auto-aceptar la invitación:', e);
        }
      }
    } catch (error: any) {
      // El backend responde acá, por ejemplo, si el código no coincide o si ya venció
      // (recordar el límite de 10 minutos mencionado en el texto de ayuda de abajo).
      Alert.alert('Código incorrecto', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <TouchableOpacity onPress={() => router.replace('/(auth)/login')} style={styles.backButton}>
        <ArrowLeft size={24} color="#02213D" />
      </TouchableOpacity>

      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Verificación</Text>
        <GradientText text="en dos pasos" style={styles.titleGradient} />
        <View style={styles.lockWrapper}>
          <LockLogo size={70} />
        </View>
      </View>

      <View style={styles.formContainer}>
        <Text style={styles.helperText}>
          Te enviamos un código de 6 dígitos por email. Ingresalo acá para terminar de
          iniciar sesión (vence en 10 minutos).
        </Text>

        <AuthInput
          placeholder="Código de 6 dígitos"
          keyboardType="number-pad"
          maxLength={6}
          value={codigo}
          onChangeText={setCodigo}
        />

        <AuthButton title="Verificar e ingresar" onPress={handleVerificar} loading={loading} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingHorizontal: 24,
    paddingTop: 60,
  },
  backButton: {
    marginBottom: 20,
  },
  logoContainer: {
    alignItems: 'center',
    marginBottom: 40,
  },
  titlePrefix: {
    fontSize: 16,
    fontFamily: 'MPLUS2-Regular',
    color: '#DF5173',
    marginBottom: -5,
  },
  titleGradient: {
    fontSize: 26,
    fontFamily: 'MPLUS2-Regular',
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    paddingTop: 8,
    marginTop: 8,
    marginBottom: 30,
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
  helperText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#445E51',
    lineHeight: 20,
    marginBottom: 20,
  },
});
