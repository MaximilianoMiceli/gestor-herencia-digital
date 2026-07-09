/**
 * @file AuthContext.tsx
 * @description Contexto global y proveedor de estado de autenticación de la aplicación.
 * 
 * Orquesta la persistencia segura del JWT token del usuario utilizando expo-secure-store,
 * exponiendo estados reactivos de carga (isLoading), sesión activa (token) y nombre del usuario (userName)
 * decodificado directamente desde las claims del JWT de forma segura offline.
 */

import React, { createContext, useContext, useState, useEffect } from 'react';
import * as SecureStore from 'expo-secure-store';

/**
 * Nombre del identificador clave bajo el cual se guardará y leerá el JWT en el llavero
 * seguro nativo del sistema operativo (SecureStore).
 */
const TOKEN_KEY = 'herencia_jwt_token';

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
  /** Bandera reactiva que indica si la app está cargando y leyendo el token desde SecureStore */
  isLoading: boolean;
  /** Método para almacenar el token en SecureStore e iniciar la sesión */
  signIn: (token: string) => Promise<void>;
  /** Método para eliminar el token de SecureStore y cerrar la sesión */
  signOut: () => Promise<void>;
}

/** Creación del contexto inicial */
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

  // Extrae y actualiza reactivamente el nombre del usuario a partir del token actual
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
      } else {
        setUserName(null);
      }
    } else {
      setUserName(null);
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
   */
  const signOut = async () => {
    try {
      await SecureStore.deleteItemAsync(TOKEN_KEY);
      setToken(null);
    } catch (error) {
      console.error('Error deleting token', error);
    }
  };

  return (
    <AuthContext.Provider value={{ token, userName, isLoading, signIn, signOut }}>
      {children}
    </AuthContext.Provider>
  );
}
