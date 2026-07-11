/**
 * @file welcome.tsx
 * @description Pantalla pública de bienvenida (Welcome Screen).
 * 
 * Actúa como punto de entrada de la aplicación para usuarios no autenticados,
 * ofreciendo un diseño limpio y minimalista con accesos rápidos hacia las
 * pantallas de inicio de sesión o creación de cuenta.
 */

import React from 'react';
import { View, StyleSheet, Text } from 'react-native';
import { useRouter } from 'expo-router';
import LockLogo from '../../components/LockLogo';
import GradientText from '../../components/GradientText';
import AuthButton from '../../components/AuthButton';

export default function WelcomeScreen() {
  const router = useRouter();

  return (
    <View style={styles.container}>
      {/* Sección del Branding / Logotipo de la app */}
      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Gestor de</Text>
        <GradientText 
          text="Herencia Digital" 
          style={styles.titleGradient} 
        />
        {/* Envoltorio con sombra elástica alrededor del logo */}
        <View style={styles.lockWrapper}>
          <LockLogo size={150} />
        </View>
      </View>

      {/* Botonera de acciones iniciales */}
      <View style={styles.buttonContainer}>
        <AuthButton 
          title="Iniciar sesión" 
          onPress={() => router.push('/(auth)/login')} 
        />
        <AuthButton 
          title="Crear cuenta" 
          variant="outline"
          onPress={() => router.push('/(auth)/register')} 
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingHorizontal: 24,
    // Distribuye el espacio vertical dejando el logo arriba y los botones abajo
    justifyContent: 'space-between', 
    paddingTop: 100,
    paddingBottom: 60,
  },
  logoContainer: {
    alignItems: 'center',
    marginTop: 60,
  },
  titlePrefix: {
    fontSize: 18,
    fontFamily: 'MPLUS2-Regular',
    color: '#DF5173',
    marginBottom: -5,
  },
  titleGradient: {
    fontSize: 34,
    fontFamily: 'MPLUS2-Regular',
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    paddingTop: 10,
    marginTop: 10,
    marginBottom: 40,
  },
  lockWrapper: {
    // Sombra sutil morada detrás del logotipo de candado para dar sensación de profundidad
    shadowColor: '#874BE5',
    shadowOffset: { width: 0, height: 10 },
    shadowOpacity: 0.15,
    shadowRadius: 20,
    elevation: 5,
  },
  buttonContainer: {
    width: '100%',
  },
});
