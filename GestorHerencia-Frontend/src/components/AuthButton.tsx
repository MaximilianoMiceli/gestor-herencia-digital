import React from 'react';
import { Text, TouchableOpacity, StyleSheet, ActivityIndicator } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';

/**
 * Propiedades del componente AuthButton.
 */
interface AuthButtonProps {
  /** Texto a mostrar dentro del botón. */
  title: string;
  /** Callback ejecutado al presionar el botón. */
  onPress: () => void;
  /** Estilo visual del botón: "primary" (degradé sólido) u "outline" (fondo blanco con borde). */
  variant?: 'primary' | 'outline';
  /** Reemplaza el texto por un spinner y deshabilita el botón mientras hay una acción en curso. */
  loading?: boolean;
}

/**
 * Botón reutilizable de las pantallas de autenticación (login, registro, recuperación, etc.).
 * Soporta dos variantes visuales para diferenciar la acción principal de una acción secundaria
 * dentro de la misma pantalla (ej: "Iniciar sesión" vs "Crear cuenta").
 */
export default function AuthButton({ title, onPress, variant = 'primary', loading = false }: AuthButtonProps) {
  // Variante secundaria: fondo blanco con borde, sin degradé, para no competir visualmente
  // con la acción principal de la pantalla.
  if (variant === 'outline') {
    return (
      <TouchableOpacity
        style={[styles.button, styles.outlineButton]}
        onPress={onPress}
        disabled={loading}
        activeOpacity={0.7}
      >
        {loading ? (
          <ActivityIndicator color="#0E4A4C" />
        ) : (
          <Text style={[styles.text, styles.outlineText]}>{title}</Text>
        )}
      </TouchableOpacity>
    );
  }

  // Variante principal ("primary"): degradé verde-a-azul que identifica la acción
  // predominante de la pantalla (login, registrarse, confirmar, etc.).
  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={loading}
      activeOpacity={0.8}
    >
      <LinearGradient
        colors={['#3AA98A', '#02213D']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0 }}
        style={styles.button}
      >
        {loading ? (
          <ActivityIndicator color="#FFFFFF" />
        ) : (
          <Text style={styles.text}>{title}</Text>
        )}
      </LinearGradient>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  button: {
    height: 52,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    marginBottom: 16,
  },
  text: {
    color: '#FFFFFF',
    fontSize: 16,
    fontFamily: 'MPLUS2-Bold',
  },
  outlineButton: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#3AA98A',
  },
  outlineText: {
    color: '#0E4A4C',
  },
});
