/**
 * @file seguridad.tsx
 * @description Pantalla de la pestaña de Seguridad del Tab Navigator.
 * 
 * Expone las opciones y configuraciones de seguridad del usuario.
 * Actualmente implementa la función crítica de "Cerrar sesión" (Log Out) para limpiar
 * tokens JWT expirados o corruptos en SecureStore, permitiendo a los usuarios volver
 * a ingresar en caso de purga de base de datos.
 */

import React from 'react';
import { StyleSheet, View, TouchableOpacity, Text } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { ThemedText } from '../../components/themed-text';
import { LinearGradient } from 'expo-linear-gradient';
import { useAuth } from '../../context/AuthContext';
import { LogOut } from 'lucide-react-native';

export default function SeguridadScreen() {
  // Consumimos el método signOut para limpiar SecureStore.
  const { signOut } = useAuth();

  return (
    <View style={styles.container}>
      {/* Cabecera estilizada con degradado en concordancia con el diseño del Dashboard */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={styles.header}
      >
        {/* SafeAreaView previene solapamiento con la barra de estado superior (notches) */}
        <SafeAreaView edges={['top']} style={styles.headerSafeArea}>
          <ThemedText style={styles.headerSubtitle}>Gestor de Herencia Digital</ThemedText>
          <ThemedText style={styles.headerTitle}>Seguridad</ThemedText>
        </SafeAreaView>
      </LinearGradient>

      {/* Contenido principal */}
      <View style={styles.content}>
        <ThemedText style={styles.placeholderText}>Opciones de Seguridad de la cuenta</ThemedText>
        
        {/* Botón de Cerrar Sesión: borra el token y redirige a welcome */}
        <TouchableOpacity style={styles.logoutButton} onPress={signOut}>
          <LogOut color="#A83232" size={20} style={{ marginRight: 8 }} />
          <Text style={styles.logoutText}>Cerrar sesión</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo verde claro unificado de la app
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
    gap: 20,
  },
  placeholderText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#1a2e2e',
  },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1.5,
    borderColor: '#A83232', // Borde rojo indicativo de acción destructiva
    borderRadius: 8,
    paddingVertical: 12,
    paddingHorizontal: 24,
    backgroundColor: '#FFFFFF',
    marginTop: 20,
    shadowColor: '#A83232',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 1,
  },
  logoutText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#A83232',
  },
});
