import React, { useState } from 'react';
import { View, StyleSheet, Text, Alert, ScrollView, KeyboardAvoidingView, Platform } from 'react-native';
import { useRouter } from 'expo-router';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { AuthService } from '../../services/auth.service';

export default function RegisterScreen() {
  const router = useRouter();
  
  const [nombre, setNombre] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRegister = async () => {
    if (!nombre || !email || !password || !confirmPassword) {
      Alert.alert('Error', 'Todos los campos son obligatorios.');
      return;
    }
    
    if (password !== confirmPassword) {
      Alert.alert('Error', 'Las contraseñas no coinciden.');
      return;
    }

    setLoading(true);
    try {
      await AuthService.register({ nombre, email, password });
      Alert.alert('Éxito', 'Cuenta creada correctamente. Por favor, inicia sesión.', [
        { text: 'OK', onPress: () => router.replace('/(auth)/login') }
      ]);
    } catch (error: any) {
      Alert.alert('Error al registrar', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
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
