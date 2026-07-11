/**
 * @file resetear-password.tsx
 * @description Segundo y último paso del flujo de "olvidé mi contraseña".
 *
 * El backend "envía" (simulado, impreso por consola del SERVIDOR) un link con la forma
 * "http://localhost:8081/resetear-password?token=<64 caracteres hex>". Como este
 * proyecto no tiene un servidor de correo real, no hay forma de que ese link abra
 * automáticamente esta pantalla en un dispositivo físico: por eso el token también se
 * puede PEGAR a mano acá (quien lo generó puede leerlo directamente de la consola del
 * backend). Si la pantalla se abre a través del link real (mismo origen), el parámetro
 * de query "token" la precarga igual.
 */

import React, { useState } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { ArrowLeft } from 'lucide-react-native';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { AuthService } from '../../services/auth.service';

export default function ResetearPasswordScreen() {
  const router = useRouter();
  const { token: tokenDeLaUrl } = useLocalSearchParams<{ token?: string }>();

  const [token, setToken] = useState(tokenDeLaUrl ?? '');
  const [passwordNueva, setPasswordNueva] = useState('');
  const [confirmarPassword, setConfirmarPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleResetear = async () => {
    if (!token.trim() || !passwordNueva || !confirmarPassword) {
      Alert.alert('Error', 'Completá el código y la nueva contraseña en ambos campos.');
      return;
    }
    if (passwordNueva !== confirmarPassword) {
      Alert.alert('Error', 'Las contraseñas no coinciden.');
      return;
    }

    setLoading(true);
    try {
      const { mensaje } = await AuthService.resetearPassword(token.trim(), passwordNueva);
      Alert.alert('Contraseña actualizada', mensaje, [
        { text: 'Iniciar sesión', onPress: () => router.replace('/(auth)/login') },
      ]);
    } catch (error: any) {
      // El backend responde acá, por ejemplo, "El token de reseteo es invalido o ya expiro."
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
        <Text style={styles.titlePrefix}>Nueva</Text>
        <GradientText text="Contraseña" style={styles.titleGradient} />
        <View style={styles.lockWrapper}>
          <LockLogo size={70} />
        </View>
      </View>

      <View style={styles.formContainer}>
        <AuthInput
          placeholder="Código recibido por email"
          autoCapitalize="none"
          value={token}
          onChangeText={setToken}
        />
        <AuthInput
          placeholder="Nueva contraseña"
          secureTextEntry
          value={passwordNueva}
          onChangeText={setPasswordNueva}
        />
        <AuthInput
          placeholder="Confirmar nueva contraseña"
          secureTextEntry
          value={confirmarPassword}
          onChangeText={setConfirmarPassword}
        />

        <AuthButton title="Actualizar contraseña" onPress={handleResetear} loading={loading} />
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
});
