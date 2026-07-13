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

  const handleEnviar = async () => {
    if (!email.trim()) {
      Alert.alert('Error', 'Ingresá tu email para poder enviarte el enlace de recuperación.');
      return;
    }

    setLoading(true);
    try {
      // El backend responde siempre el mismo mensaje genérico exista o no la cuenta,
      // para no permitir "adivinar" emails registrados.
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
