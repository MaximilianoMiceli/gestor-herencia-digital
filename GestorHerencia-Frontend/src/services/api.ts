import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import * as SecureStore from 'expo-secure-store';
import { API_BASE_URL } from '../constants/api';

// Se define una sola vez para que este archivo y AuthContext lean/escriban la misma clave.
export const TOKEN_KEY = 'herencia_jwt_token';

// Solo avisa que la sesión ya no es válida; la navegación la resuelve el guard de
// rutas en _layout.tsx, que observa reactivamente el token del AuthContext.
type ManejadorNoAutorizado = () => void;

let manejadorNoAutorizado: ManejadorNoAutorizado | null = null;

// api.ts es infraestructura pura (sin React) y no puede llamar a signOut() directamente:
// este registro de callback es el único punto de conexión con AuthContext.
export function setManejadorNoAutorizado(manejador: ManejadorNoAutorizado | null) {
  manejadorNoAutorizado = manejador;
}

// No se fija un "Content-Type" por defecto: Axios ya elige el correcto según el body
// (JSON u boundary multipart para FormData). Fijarlo a mano rompería la subida de archivos.
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
    // El backend no implementa refresh token: ante un 401 la única acción correcta es
    // cerrar la sesión local.
    if (error.response?.status === 401) {
      // Se borra acá mismo para que el token vencido no se siga reenviando aunque
      // todavía no haya ningún manejador registrado.
      await SecureStore.deleteItemAsync(TOKEN_KEY);
      manejadorNoAutorizado?.();
    }

    // Se extrae el mensaje acá para que el llamador pueda hacer `catch (err) { err.message }`.
    const mensaje =
      error.response?.data?.mensaje ??
      error.response?.data?.message ??
      error.message ??
      'Ocurrió un error de red inesperado.';

    return Promise.reject(new Error(mensaje));
  }
);
