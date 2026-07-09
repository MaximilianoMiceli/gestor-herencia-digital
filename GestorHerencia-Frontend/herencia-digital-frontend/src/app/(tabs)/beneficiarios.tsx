import React from 'react';
import { StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { ThemedText } from '../../components/themed-text';
import { LinearGradient } from 'expo-linear-gradient';

/**
 * Pantalla para la asignación y gestión de beneficiarios.
 * Permite definir quiénes recibirán el acceso a los activos digitales tras la herencia y asociar contactos de confianza.
 */
export default function BeneficiariosScreen() {
  return (
    <View style={styles.container}>
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={styles.header}
      >
        <SafeAreaView edges={['top']} style={styles.headerSafeArea}>
          <ThemedText style={styles.headerSubtitle}>Gestor de Herencia Digital</ThemedText>
          <ThemedText style={styles.headerTitle}>Beneficiarios</ThemedText>
        </SafeAreaView>
      </LinearGradient>
      <View style={styles.content}>
        <ThemedText style={styles.placeholderText}>Pantalla de Beneficiarios</ThemedText>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
  },
  header: {
    paddingHorizontal: 20,
    paddingBottom: 20,
    borderBottomLeftRadius: 16,
    borderBottomRightRadius: 16,
  },
  headerSafeArea: {
    paddingTop: 10,
    gap: 4,
  },
  headerSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 12,
    color: '#ffffff',
    opacity: 0.7,
  },
  headerTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    color: '#ffffff',
  },
  content: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 20,
  },
  placeholderText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#1a2e2e',
  },
});
