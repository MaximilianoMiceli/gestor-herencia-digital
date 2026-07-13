import React from 'react';
import { Stack } from 'expo-router';

export default function AuthLayout() {
  return (
    // Fondo común para evitar parpadeos de color al navegar entre pantallas de auth.
    <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: '#E0F8CA' } }}>
      <Stack.Screen name="welcome" />
      <Stack.Screen name="login" />
      <Stack.Screen name="register" />
      <Stack.Screen name="olvide-password" />
      <Stack.Screen name="resetear-password" />
      <Stack.Screen name="verificar-2fa" />
    </Stack>
  );
}
