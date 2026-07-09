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
      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Gestor de</Text>
        <GradientText 
          text="Herencia Digital" 
          style={styles.titleGradient} 
        />
        <View style={styles.lockWrapper}>
          <LockLogo size={100} />
        </View>
      </View>

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
