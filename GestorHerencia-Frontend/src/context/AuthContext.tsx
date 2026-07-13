import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import * as SecureStore from 'expo-secure-store';
import { TOKEN_KEY, setManejadorNoAutorizado } from '../services/api';

// Decodifica Base64 sin depender de "atob" (no disponible en todos los entornos de RN).
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

const decodeJwtPayload = (token: string): any => {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = base64Decode(base64);
    // Convierte a URI percent-encoding para poder decodificar UTF-8 (acentos, ñ, etc.).
    const percentEncoded = decoded.split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('');
    return JSON.parse(decodeURIComponent(percentEncoded));
  } catch (error) {
    console.error('Error decoding JWT payload:', error);
    return null;
  }
};

interface AuthContextData {
  token: string | null;
  userName: string | null;
  userEmail: string | null;
  // Id numérico (Claim NameIdentifier), necesario para llamar a "PUT /api/usuarios/{id}/...".
  userId: number | null;
  userRole: string | null;
  isLoading: boolean;
  signIn: (token: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextData>({} as AuthContextData);

export function useAuth() {
  return useContext(AuthContext);
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [userName, setUserName] = useState<string | null>(null);
  const [userEmail, setUserEmail] = useState<string | null>(null);
  const [userId, setUserId] = useState<number | null>(null);
  const [userRole, setUserRole] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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

        const firstName = fullName ? fullName.trim().split(/\s+/)[0] : null;
        setUserName(firstName);

        const email = payload.email ||
                      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
                      null;
        setUserEmail(email);

        // ClaimTypes.NameIdentifier se traduce, por el mapeo de claims cortos de .NET, a "nameid".
        const rawId = payload.nameid ||
                      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                      null;
        setUserId(rawId ? Number(rawId) : null);

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

  const signIn = async (newToken: string) => {
    try {
      await SecureStore.setItemAsync(TOKEN_KEY, newToken);
      setToken(newToken);
    } catch (error) {
      console.error('Error saving token', error);
    }
  };

  // useCallback sin dependencias: mantiene una referencia estable para el cliente Axios
  // (ver useEffect de abajo), evitando que el efecto de registro se dispare de más.
  const signOut = useCallback(async () => {
    try {
      await SecureStore.deleteItemAsync(TOKEN_KEY);
      setToken(null);
    } catch (error) {
      console.error('Error deleting token', error);
    }
  }, []);

  // api.ts no puede usar este contexto directamente (no tiene React): registra un
  // callback para que, ante un 401 en cualquier request, se ejecute signOut() acá.
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
