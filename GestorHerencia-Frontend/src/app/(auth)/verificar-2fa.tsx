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
  // usuarioId identifica el código pendiente (aún no hay sesión ni token).
  const { usuarioId, acceptInvitationId } = useLocalSearchParams<{
    usuarioId?: string;
    acceptInvitationId?: string;
  }>();

  const [codigo, setCodigo] = useState('');
  const [loading, setLoading] = useState(false);

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

      await signIn(response.token);

      if (acceptInvitationId) {
        try {
          await InvitacionesService.procesar(acceptInvitationId, 'aceptar');
        } catch (e) {
          // No se interrumpe el login: el usuario puede aceptar la invitación después.
          console.error('Error al auto-aceptar la invitación:', e);
        }
      }
    } catch (error: any) {
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
