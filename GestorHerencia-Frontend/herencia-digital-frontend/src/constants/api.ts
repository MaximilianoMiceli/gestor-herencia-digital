/**
 * @file api.ts
 * @description Punto único de configuración de la URL base del backend.
 *
 * Antes esta URL estaba hardcodeada acá mismo (una IP de LAN fija para desarrollo
 * y un dominio placeholder para producción). Ahora se lee de una variable de
 * entorno para que cada desarrollador/entorno pueda apuntar a su propio backend
 * sin tener que tocar código fuente ni arriesgarse a commitear por error la IP
 * de otra persona.
 *
 * Expo (a través de su integración con Metro/Babel) reemplaza, en tiempo de
 * build, cualquier referencia a "process.env.EXPO_PUBLIC_*" por el valor literal
 * leído de los archivos ".env"/".env.local" en la raíz del proyecto. Solo las
 * variables con el prefijo "EXPO_PUBLIC_" quedan expuestas al bundle del
 * cliente (JavaScript que corre en el teléfono): cualquier otra variable de
 * entorno sin ese prefijo es invisible acá a propósito, para no exponer
 * secretos de servidor por accidente en una app que cualquiera puede
 * descompilar.
 */
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ?? 'https://tu-dominio.com/api';
