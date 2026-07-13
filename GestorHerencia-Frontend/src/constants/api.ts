/**
 * @file api.ts
 * @description Punto único de configuración de la URL base del backend.
 *
 * Se lee de una variable de entorno (en vez de hardcodearla) para que cada
 * desarrollador/entorno apunte a su propio backend sin tocar código fuente ni
 * arriesgarse a commitear por error la IP de otra persona.
 *
 * Expo reemplaza en build cualquier "process.env.EXPO_PUBLIC_*" por el valor de
 * ".env"/".env.local". Solo las variables con ese prefijo quedan expuestas al bundle
 * del cliente; cualquier otra queda invisible a propósito, para no filtrar secretos
 * de servidor en una app que cualquiera puede descompilar.
 */
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ?? 'https://tu-dominio.com/api';
