/**
 * @file auth.service.ts
 * @description Servicio HTTP encargado de la autenticación de usuarios contra la API de .NET.
 * 
 * Expone las peticiones de registro de cuentas y de inicio de sesión, procesando las
 * respuestas de error estructuradas que el servidor retorna en formato JSON.
 */

import { API_BASE_URL } from '../constants/api';

/**
 * DTO para el inicio de sesión.
 * Mapea con el DTO 'UsuarioLoginDTO' esperado en el endpoint 'POST /api/auth/login'.
 */
export interface LoginDTO {
  email: string;
  password: string;
}

/**
 * DTO para el registro de una cuenta.
 * Mapea con el DTO 'UsuarioCreacionDTO' esperado en el endpoint 'POST /api/auth/registro'.
 */
export interface RegistroDTO {
  nombre: string;
  email: string;
  password: string;
}

/**
 * Respuesta del servidor que transporta el JWT token generado al autenticar con éxito.
 */
export interface TokenRespuestaDTO {
  token: string;
}

/**
 * Representación del usuario creado devuelto tras el registro.
 */
export interface UsuarioDTO {
  id: number;
  nombre: string;
  email: string;
  rol: string;
}

/**
 * Clase de servicio que encapsula las peticiones relativas a la autenticación de usuarios.
 */
export class AuthService {
  /**
   * Envía las credenciales del usuario al servidor para verificar e iniciar sesión.
   * Llama a: POST /api/auth/login
   * 
   * @param data Credenciales de inicio de sesión (email y contraseña).
   * @returns Promesa con el DTO de respuesta que contiene el JWT token emitido.
   * @throws Error indicando la causa específica devuelta por la API (ej: contraseña inválida, usuario no encontrado).
   */
  static async login(data: LoginDTO): Promise<TokenRespuestaDTO> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      let errorMsg = 'Error al iniciar sesión';
      try {
        // El controlador AuthController del backend responde con un objeto { mensaje = "..." }
        // ante excepciones de lógica de negocio o validaciones incorrectas.
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorMsg;
      } catch (e) {
        // Ignorar error de parsing si el cuerpo no es JSON (ej: si hay un crash 500 crudo)
      }
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Registra un nuevo usuario en la base de datos del sistema.
   * Llama a: POST /api/auth/registro
   * 
   * @param data Datos de creación de la cuenta (nombre, email y contraseña en texto plano).
   * @returns Promesa con los datos del usuario registrado.
   * @throws Error indicando la causa específica devuelta por la API (ej: email duplicado, formato inválido).
   */
  static async register(data: RegistroDTO): Promise<UsuarioDTO> {
    const response = await fetch(`${API_BASE_URL}/auth/registro`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      let errorMsg = 'Error al registrar la cuenta';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorMsg;
      } catch (e) {
        // Ignorar error de parsing si el cuerpo no es JSON
      }
      throw new Error(errorMsg);
    }

    return response.json();
  }
}
