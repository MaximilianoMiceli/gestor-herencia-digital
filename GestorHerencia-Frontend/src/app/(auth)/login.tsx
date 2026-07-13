import React, { useState } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft } from 'lucide-react-native';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { useAuth } from '../../context/AuthContext';
import { AuthService } from '../../services/auth.service';
import { InvitacionesService } from '../../services/invitaciones.service';

export default function LoginScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { signIn } = useAuth();
  const { acceptInvitationId } = useLocalSearchParams<{ acceptInvitationId?: string }>();
  
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert('Error', 'Por favor ingresa tu email y contraseña.');
      return;
    }

    setLoading(true);
    try {
      const response = await AuthService.login({ email, password });

      // Con 2FA habilitado el backend no devuelve token todavía; se completa el login
      // recién cuando verificar-2fa confirme el código.
      if (response.requiereDobleFactor && response.usuarioId) {
        router.push({
          pathname: '/(auth)/verificar-2fa',
          params: { usuarioId: String(response.usuarioId), acceptInvitationId },
        });
        return;
      }

      // El token se persiste antes de aceptar la invitación: el interceptor de Axios
      // lo necesita en SecureStore para adjuntar el header Authorization.
      await signIn(response.token);

      if (acceptInvitationId) {
        try {
          await InvitacionesService.procesar(acceptInvitationId, 'aceptar');
        } catch (e) {
          console.error('Error al auto-aceptar la invitación:', e);
        }
      }
    } catch (error: any) {
      Alert.alert('Error de autenticación', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <TouchableOpacity
        style={[styles.backButton, { top: insets.top + 12 }]}
        onPress={() => (router.canGoBack() ? router.back() : router.replace('/(auth)/welcome'))}
      >
        <ArrowLeft size={24} color="#02213D" />
      </TouchableOpacity>

      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Gestor de</Text>
        <GradientText text="Herencia Digital" style={styles.titleGradient} />
        <View style={styles.lockWrapper}>
          <LockLogo size={80} />
        </View>
      </View>

      <View style={styles.formContainer}>
        <AuthInput 
          placeholder="Email" 
          keyboardType="email-address"
          value={email}
          onChangeText={setEmail}
        />
        <AuthInput 
          placeholder="Contraseña" 
          secureTextEntry
          value={password}
          onChangeText={setPassword}
        />

        <TouchableOpacity
          style={styles.forgotPassword}
          onPress={() => router.push('/(auth)/olvide-password')}
        >
          <Text style={styles.forgotText}>¿Olvidaste tu contraseña?</Text>
        </TouchableOpacity>

        <AuthButton 
          title="Iniciar sesión" 
          onPress={handleLogin} 
          loading={loading}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingHorizontal: 24,
    paddingTop: 80,
  },
  backButton: {
    position: 'absolute',
    left: 20,
    zIndex: 10,
    padding: 6,
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
  forgotPassword: {
    alignSelf: 'flex-end',
    marginBottom: 24,
  },
  forgotText: {
    color: '#02213D',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
  },
});
