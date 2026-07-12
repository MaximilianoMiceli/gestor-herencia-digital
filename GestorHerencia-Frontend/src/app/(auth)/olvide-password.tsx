/**
 * @file olvide-password.tsx
 * @description Primer paso del flujo real de "olvidé mi contraseña".
 *
 * Antes el link "¿Olvidaste tu contraseña?" de login.tsx era puramente decorativo (sin
 * `onPress`). Esta pantalla le da vida llamando a POST /api/auth/olvide-password: el
 * backend genera un token de reseteo de un solo uso (válido 1 hora) y lo "envía"
 * (simulado, impreso por consola del lado del SERVIDOR, igual que el resto de los
 * flujos de email de este proyecto) al email ingresado.
 *
 * El backend SIEMPRE responde el mismo mensaje de éxito genérico, exista o no una
 * cuenta con ese email (para no permitir que alguien use este formulario para
 * "adivinar" qué emails están registrados): por eso acá tampoco se distingue el caso,
 * simplemente se muestra el mensaje que el propio backend devuelve.
 */

import React, { useState } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft } from 'lucide-react-native';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { AuthService } from '../../services/auth.service';

export default function OlvidePasswordScreen() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);

  /**
   * Dispara el pedido de reseteo de contraseña. No hay nada que validar además de
   * "no vacío": el formato del email y si corresponde o no a una cuenta lo resuelve
   * el backend, que además responde siempre igual (ver comentario del encabezado).
   */
  const handleEnviar = async () => {
    if (!email.trim()) {
      Alert.alert('Error', 'Ingresá tu email para poder enviarte el enlace de recuperación.');
      return;
    }

    setLoading(true);
    try {
      // La respuesta de este endpoint es SIEMPRE el mismo mensaje genérico (ver el
      // comentario de arriba): no hay ningún dato adicional que distinguir acá.
      const { mensaje } = await AuthService.olvidePassword(email.trim());

      Alert.alert('Listo', mensaje, [
        {
          text: 'Ya tengo el código',
          onPress: () => router.push('/(auth)/resetear-password'),
        },
      ]);
    } catch (error: any) {
      Alert.alert('Error', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <TouchableOpacity onPress={() => router.back()} style={styles.backButton}>
        <ArrowLeft size={24} color="#02213D" />
      </TouchableOpacity>

      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Recuperar</Text>
        <GradientText text="Contraseña" style={styles.titleGradient} />
        <View style={styles.lockWrapper}>
          <LockLogo size={70} />
        </View>
      </View>

      <View style={styles.formContainer}>
        <Text style={styles.helperText}>
          Ingresá el email de tu cuenta. Si corresponde a una cuenta registrada, te
          enviaremos un enlace para elegir una nueva contraseña.
        </Text>

        <AuthInput
          placeholder="Email"
          keyboardType="email-address"
          value={email}
          onChangeText={setEmail}
        />

        <AuthButton title="Enviar enlace de recuperación" onPress={handleEnviar} loading={loading} />

        <TouchableOpacity
          style={styles.linkButton}
          onPress={() => router.push('/(auth)/resetear-password')}
        >
          <Text style={styles.linkText}>Ya tengo un código, quiero resetear mi contraseña</Text>
        </TouchableOpacity>
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
    fontSize: 28,
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
  linkButton: {
    alignItems: 'center',
    marginTop: 4,
  },
  linkText: {
    color: '#02213D',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    textAlign: 'center',
  },
});
