/**
 * @file api.ts
 * @description Cliente HTTP centralizado (Axios) para toda la comunicación con el backend.
 *
 * Centraliza en un solo lugar tres cosas que antes se repetían a mano en cada pantalla:
 * inyección del JWT en cada request, manejo de un 401 (cierre de sesión), y normalización
 * del formato de error del backend a un `Error` de JS estándar.
 */

import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import * as SecureStore from 'expo-secure-store';
import { API_BASE_URL } from '../constants/api';

/**
 * Clave del JWT en el llavero seguro nativo (Keychain en iOS, Keystore en Android).
 * Se define una sola vez acá para que este archivo y AuthContext lean/escriban siempre
 * la misma clave y no puedan desincronizarse.
 */
export const TOKEN_KEY = 'herencia_jwt_token';

/**
 * Callback ejecutado cuando el interceptor de response detecta un 401. Solo avisa que
 * la sesión ya no es válida; no decide navegación (eso lo resuelve el guard de rutas
 * en `_layout.tsx`, que observa reactivamente el `token` del AuthContext).
 */
type ManejadorNoAutorizado = () => void;

// Referencia al callback que AuthContext registra al montarse (ver setManejadorNoAutorizado).
let manejadorNoAutorizado: ManejadorNoAutorizado | null = null;

/**
 * Permite que AuthContext registre qué hacer ante un 401 en cualquier request.
 * api.ts es una capa de infraestructura pura (sin React, sin hooks) y no puede llamar a
 * `signOut()` directamente; este registro de callback es el único punto de conexión
 * entre ambos mundos.
 */
export function setManejadorNoAutorizado(manejador: ManejadorNoAutorizado | null) {
  manejadorNoAutorizado = manejador;
}

// No se fija un "Content-Type" por defecto: Axios ya elige el correcto según el body
// (JSON para objetos planos; boundary multipart calculado por la plataforma cuando el
// body es un FormData, usado al subir archivos — ver assets.service.ts). Fijarlo a mano
// rompería esas subidas.
export const api = axios.create({
  baseURL: API_BASE_URL,
});

// Interceptor de request: inyecta el JWT guardado en SecureStore en el header
// "Authorization" de toda request saliente, para que ninguna pantalla tenga que hacerlo.
api.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const token = await SecureStore.getItemAsync(TOKEN_KEY);

    if (token) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }

    return config;
  },
  (error: AxiosError) => Promise.reject(error)
);

// Interceptor de response: maneja el 401 en un solo lugar y normaliza los errores del backend.
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<{ mensaje?: string; message?: string }>) => {
    // El backend no implementa refresh token (el JWT expira a los 30 min y no hay forma
    // de renovarlo sin loguearse de nuevo), así que ante un 401 la única acción correcta
    // es cerrar la sesión local.
    if (error.response?.status === 401) {
      // Se borra acá mismo (sin esperar a que AuthContext lo haga) para que el token
      // vencido no se siga reenviando aunque todavía no haya ningún manejador registrado.
      await SecureStore.deleteItemAsync(TOKEN_KEY);

      // Notifica a AuthContext para que limpie su estado en memoria; ese cambio de
      // `token` a null dispara reactivamente el guard de rutas en `_layout.tsx`.
      manejadorNoAutorizado?.();
    }

    // El backend responde los errores como `{ mensaje: "..." }`. Se extrae ese texto acá
    // para que el código que llama pueda hacer simplemente `catch (err) { err.message }`
    // sin conocer la forma de la respuesta HTTP subyacente.
    const mensaje =
      error.response?.data?.mensaje ??
      error.response?.data?.message ??
      error.message ??
      'Ocurrió un error de red inesperado.';

    return Promise.reject(new Error(mensaje));
  }
);
