import React from 'react';
import { TextInput, StyleSheet, TextInputProps } from 'react-native';

interface AuthInputProps extends TextInputProps {}

export default function AuthInput(props: AuthInputProps) {
  return (
    <TextInput
      style={styles.input}
      placeholderTextColor="#999"
      // Se define antes que "props" para que el consumidor pueda sobreescribirla si lo necesita.
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
