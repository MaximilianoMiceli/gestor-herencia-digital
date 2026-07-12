import React from 'react';
import { Stack } from 'expo-router';

/**
 * Layout del grupo de rutas "(auth)" (expo-router): define la pila de pantallas del flujo
 * de autenticación (bienvenida, login, registro, recuperación de contraseña y 2FA) que se
 * muestra a los usuarios que todavía no iniciaron sesión.
 */
export default function AuthLayout() {
  return (
    // Se ocultan los headers nativos y se fija el color de fondo común a todo el flujo
    // para que la transición entre pantallas de auth no muestre parpadeos de color.
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
