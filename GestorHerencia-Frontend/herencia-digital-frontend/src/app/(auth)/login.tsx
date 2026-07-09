/**
 * @file login.tsx
 * @description Pantalla pública de inicio de sesión (Login Screen).
 * 
 * Permite a los usuarios autenticarse mediante su correo electrónico y contraseña.
 * Tras un inicio de sesión exitoso, el JWT devuelto por el backend se persiste
 * en SecureStore a través del método `signIn` del AuthContext, lo que provoca
 * que el enrutador raíz cambie de flujo y cargue el Dashboard privado.
 */

import React, { useState } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Alert } from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';
import AuthInput from '../../components/AuthInput';
import { useAuth } from '../../context/AuthContext';
import { AuthService } from '../../services/auth.service';
import { API_BASE_URL } from '../../constants/api';

export default function LoginScreen() {
  const router = useRouter();
  const { signIn } = useAuth();
  const { acceptInvitationId } = useLocalSearchParams<{ acceptInvitationId?: string }>();
  
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  /**
   * Procesa la solicitud de inicio de sesión interactuando con la API y el llavero.
   */
  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert('Error', 'Por favor ingresa tu email y contraseña.');
      return;
    }

    setLoading(true);
    try {
      // 1. Enviamos credenciales al endpoint de login
      const response = await AuthService.login({ email, password });
      
      // Si el inicio de sesión fue disparado por una invitación, procesamos la aceptación primero
      if (acceptInvitationId) {
        try {
          await fetch(`${API_BASE_URL}/invitaciones/${acceptInvitationId}/procesar`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${response.token}`,
            },
            body: JSON.stringify({ accion: 'aceptar' }),
          });
        } catch (e) {
          console.error('Error al auto-aceptar la invitación:', e);
        }
      }

      // 2. Persistimos el JWT devuelto. Esto dispara reactivamente la redirección a /(tabs)
      // en InitialLayout gracias al hook useAuth()
      await signIn(response.token);
    } catch (error: any) {
      Alert.alert('Error de autenticación', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      {/* Cabecera del Branding */}
      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Gestor de</Text>
        <GradientText text="Herencia Digital" style={styles.titleGradient} />
        <View style={styles.lockWrapper}>
          <LockLogo size={80} />
        </View>
      </View>

      {/* Formulario de entradas del usuario */}
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
        
        {/* Acceso auxiliar para recuperar contraseña (vista simulada de maqueta) */}
        <TouchableOpacity style={styles.forgotPassword}>
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
