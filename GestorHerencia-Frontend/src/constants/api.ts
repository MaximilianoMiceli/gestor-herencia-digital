// Solo las variables con prefijo EXPO_PUBLIC_ quedan expuestas al bundle del cliente;
// cualquier otra queda invisible a propósito, para no filtrar secretos de servidor.
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ?? 'https://tu-dominio.com/api';
