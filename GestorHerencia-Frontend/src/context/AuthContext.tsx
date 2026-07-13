/**
 * @file AuthContext.tsx
 * @description Contexto global y proveedor de estado de autenticación de la aplicación.
 * 
 * Orquesta la persistencia segura del JWT token del usuario utilizando expo-secure-store,
 * exponiendo estados reactivos de carga (isLoading), sesión activa (token) y nombre del usuario (userName)
 * decodificado directamente desde las claims del JWT de forma segura offline.
 */

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import * as SecureStore from 'expo-secure-store';
import { TOKEN_KEY, setManejadorNoAutorizado } from '../services/api';

/**
 * Decodifica Base64 en Javascript de forma segura e independiente de entornos (funciona en iOS, Android y Web).
 */
const base64Decode = (str: string): string => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';
  let output = '';
  str = String(str).replace(/=+$/, '');
  if (str.length % 4 === 1) {
    throw new Error("'atob' failed: The string to be decoded is not correctly encoded.");
  }
  for (
    let bc = 0, bs = 0, buffer, idx = 0;
    (buffer = str.charAt(idx++));
    ~buffer && ((bs = bc % 4 ? bs * 64 + buffer : buffer), bc++ % 4)
      ? (output += String.fromCharCode(255 & (bs >> ((-2 * bc) & 6))))
      : 0
  ) {
    buffer = chars.indexOf(buffer);
  }
  return output;
};

/**
 * Decodifica el payload (segunda sección) de un token JWT.
 */
const decodeJwtPayload = (token: string): any => {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = base64Decode(base64);
    // Convierte los bytes decodificados a URI percent-encoding para decodificar caracteres UTF-8 (acentos, ñ, etc.)
    const percentEncoded = decoded.split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('');
    return JSON.parse(decodeURIComponent(percentEncoded));
  } catch (error) {
    console.error('Error decoding JWT payload:', error);
    return null;
  }
};

/**
 * Interfaz que describe el estado expuesto por el Contexto de Autenticación.
 */
interface AuthContextData {
  /** El token de sesión del usuario logueado en texto plano, o null si está desconectado */
  token: string | null;
  /** Nombre del usuario autenticado, extraído del JWT de forma segura, o null */
  userName: string | null;
  /** Correo del usuario autenticado, extraído del JWT de forma segura, o null */
  userEmail: string | null;
  /** Id numérico del usuario autenticado (Claim NameIdentifier), o null. Necesario para
   *  llamar a los endpoints "PUT /api/usuarios/{id}/..." de la pantalla de perfil. */
  userId: number | null;
  /** Rol del usuario autenticado ("Administrador" o "Usuario"), extraído del Claim de Rol.
   *  Se usa para decidir si mostrar el panel de administración de certificados. */
  userRole: string | null;
  /** Bandera reactiva que indica si la app está cargando y leyendo el token desde SecureStore */
  isLoading: boolean;
  /** Método para almacenar el token en SecureStore e iniciar la sesión */
  signIn: (token: string) => Promise<void>;
  /** Método para eliminar el token de SecureStore y cerrar la sesión */
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextData>({} as AuthContextData);

/**
 * Hook de utilidad para consumir de forma abreviada el estado de autenticación.
 * 
 * @returns El estado y métodos expuestos por el proveedor de autenticación.
 */
export function useAuth() {
  return useContext(AuthContext);
}

/**
 * Proveedor global de autenticación que envuelve al árbol de componentes de la aplicación.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [userName, setUserName] = useState<string | null>(null);
  const [userEmail, setUserEmail] = useState<string | null>(null);
  const [userId, setUserId] = useState<number | null>(null);
  const [userRole, setUserRole] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Inicializar estado de autenticación al arrancar la app.
  // Lee el llavero seguro nativo antes de permitir que las rutas evalúen el flujo.
  useEffect(() => {
    const loadToken = async () => {
      try {
        const storedToken = await SecureStore.getItemAsync(TOKEN_KEY);
        if (storedToken) {
          setToken(storedToken);
        }
      } catch (error) {
        console.error('Error loading token from secure store', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadToken();
  }, []);

  // Deriva userName/userEmail/userId/userRole del token en cada cambio de sesión.
  useEffect(() => {
    if (token) {
      const payload = decodeJwtPayload(token);
      if (payload) {
        // En JWT emitido por .NET, ClaimTypes.Name se traduce a "unique_name", "name"
        // o al esquema URI completo del claim.
        const fullName = payload.unique_name || 
                         payload.name || 
                         payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || 
                         null;
        
        // Extraemos únicamente el primer nombre (ej: "Augusto" en lugar de "Augusto Miceli")
        const firstName = fullName ? fullName.trim().split(/\s+/)[0] : null;
        setUserName(firstName);

        // Extraemos el email del usuario logueado
        const email = payload.email ||
                      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
                      null;
        setUserEmail(email);

        // Extraemos el Id numérico del usuario (ClaimTypes.NameIdentifier en el backend
        // se traduce, por el mapeo de claims cortos de .NET, a "nameid").
        const rawId = payload.nameid ||
                      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                      null;
        setUserId(rawId ? Number(rawId) : null);

        // Extraemos el Rol del usuario (ClaimTypes.Role se traduce a "role").
        const role = payload.role ||
                     payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                     null;
        setUserRole(role);
      } else {
        setUserName(null);
        setUserEmail(null);
        setUserId(null);
        setUserRole(null);
      }
    } else {
      setUserName(null);
      setUserEmail(null);
      setUserId(null);
      setUserRole(null);
    }
  }, [token]);

  /**
   * Almacena de forma asíncrona un nuevo token JWT en el llavero de seguridad nativo
   * y actualiza el estado interno para reactivar los disparadores de navegación.
   * 
   * @param newToken El token de sesión emitido por el backend.
   */
  const signIn = async (newToken: string) => {
    try {
      await SecureStore.setItemAsync(TOKEN_KEY, newToken);
      setToken(newToken);
    } catch (error) {
      console.error('Error saving token', error);
    }
  };

  /**
   * Borra de forma asíncrona el token del llavero nativo del teléfono,
   * estableciendo el token a null para devolver al usuario a la pantalla de bienvenida.
   *
   * Se envuelve en `useCallback` (sin dependencias: no lee ningún valor externo que
   * cambie entre renders) para poder pasar una referencia ESTABLE de esta función al
   * cliente Axios (ver el useEffect de abajo): si `signOut` cambiara de identidad en
   * cada render, el efecto de registro/desregistro se dispararía sin necesidad.
   */
  const signOut = useCallback(async () => {
    try {
      await SecureStore.deleteItemAsync(TOKEN_KEY);
      setToken(null);
    } catch (error) {
      console.error('Error deleting token', error);
    }
  }, []);

  // --- Conexión con el interceptor de Axios (ver services/api.ts) ---
  // api.ts es una capa de infraestructura pura (sin React, sin hooks): no puede
  // "usar" este AuthContext directamente. En cambio, expone un registro de callback
  // (`setManejadorNoAutorizado`) para que, apenas el servidor responda un 401 en
  // CUALQUIER request, se ejecute `signOut()` acá, en el único lugar que realmente
  // posee el estado de sesión de React. Al desmontarse el Provider (en la práctica,
  // nunca ocurre mientras la app está viva, pero es la forma correcta de escribir un
  // efecto con "limpieza"), se desregistra pasando `null` para no dejar una referencia
  // colgada a un componente ya desmontado.
  useEffect(() => {
    setManejadorNoAutorizado(signOut);
    return () => setManejadorNoAutorizado(null);
  }, [signOut]);

  return (
    <AuthContext.Provider value={{ token, userName, userEmail, userId, userRole, isLoading, signIn, signOut }}>
      {children}
    </AuthContext.Provider>
  );
}
