import React from 'react';
import { TextInput, StyleSheet, TextInputProps } from 'react-native';

/**
 * Propiedades del componente AuthInput.
 * Extiende directamente TextInputProps para poder recibir cualquier prop nativa de
 * TextInput (value, onChangeText, keyboardType, secureTextEntry, etc.) sin redeclararla.
 */
interface AuthInputProps extends TextInputProps {}

/**
 * Campo de texto estándar para las pantallas de autenticación (login, registro,
 * recuperación de contraseña, etc.). Centraliza el estilo visual y el comportamiento
 * de capitalización para que todos los inputs de este flujo se vean y se comporten igual.
 */
export default function AuthInput(props: AuthInputProps) {
  return (
    <TextInput
      style={styles.input}
      placeholderTextColor="#999"
      // Los datos de estas pantallas (emails, contraseñas) no deben autocapitalizarse;
      // se define acá antes que "props" para que el consumidor pueda sobreescribirla
      // puntualmente si algún campo lo necesitara.
      autoCapitalize="none"
      {...props}
    />
  );
}

const styles = StyleSheet.create({
  input: {
    backgroundColor: '#FFFFFF',
    height: 52,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingHorizontal: 16,
    fontSize: 15,
    fontFamily: 'MPLUS2-Regular',
    color: '#333',
    marginBottom: 16,
  },
});
