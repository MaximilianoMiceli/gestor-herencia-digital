/**
 * @file auth.service.ts
 * @description Servicio HTTP encargado de la autenticación de usuarios contra la API de .NET.
 *
 * Expone registro, login (con soporte de segundo factor por email) y el flujo de
 * "olvidé mi contraseña". Todas las llamadas pasan por el cliente Axios centralizado
 * (ver api.ts): ya no hay que armar manualmente el header ni parsear el error a mano,
 * el interceptor de respuesta de api.ts ya deja cualquier error como un `Error` de JS
 * estándar con `.message` legible.
 */

import { api } from './api';

/** DTO para el inicio de sesión. Mapea con 'LoginDTO' del backend. */
export interface LoginDTO {
  email: string;
  password: string;
}

/** DTO para el registro de una cuenta. Mapea con 'RegistroDTO' del backend. */
export interface RegistroDTO {
  nombre: string;
  email: string;
  password: string;
  /** Documento Nacional de Identidad: 7 u 8 dígitos, validado también en el backend. */
  dni: string;
  /** Fecha de nacimiento en formato ISO ("AAAA-MM-DD"). El backend exige mayoría de edad. */
  fechaNacimiento: string;
}

/**
 * Respuesta del servidor al intentar loguearse. Mapea con 'TokenRespuestaDTO' del
 * backend (ver Herencia.Business/Dtos/TokenRespuestaDTO.cs).
 *
 * Puede llegar en DOS formas mutuamente excluyentes, según si el usuario tiene 2FA
 * habilitado o no:
 *   - Login SIN 2FA: `{ token: "eyJ...", requiereDobleFactor: false, usuarioId: null }`
 *   - Login CON 2FA: `{ token: "", requiereDobleFactor: true, usuarioId: 5 }` (todavía
 *     NO hay sesión: hay que llamar a `verificarDobleFactor` con el código recibido
 *     por email para recién ahí obtener un token real).
 */
export interface TokenRespuestaDTO {
  token: string;
  requiereDobleFactor: boolean;
  usuarioId: number | null;
}

/** Representación del usuario creado, devuelta tras el registro. */
export interface UsuarioDTO {
  id: number;
  nombre: string;
  email: string;
  dni: string;
  fechaNacimiento: string;
  rol: string;
  dobleFactorHabilitado: boolean;
}

export class AuthService {
  /**
   * Envía las credenciales del usuario al servidor para verificar e iniciar sesión.
   * Llama a: POST /api/auth/login
   *
   * Si la respuesta trae `requiereDobleFactor: true`, el LLAMADOR (la pantalla de
   * login) es quien decide navegar a la pantalla de "ingresar código" en vez de
   * considerar la sesión iniciada: este servicio solo transporta el dato, no toma
   * decisiones de navegación.
   */
  static async login(data: LoginDTO): Promise<TokenRespuestaDTO> {
    // Axios devuelve un objeto "AxiosResponse": los datos del body deserializados ya
    // vienen en la propiedad ".data" (a diferencia de `fetch`, que exige un
    // `await response.json()` manual). Si el servidor responde 4xx/5xx, Axios NO
    // llega a esta línea: lanza una excepción que ya queda atrapada y traducida por
    // el interceptor de respuesta de api.ts (ver ese archivo).
    const response = await api.post<TokenRespuestaDTO>('/auth/login', data);
    return response.data;
  }

  /**
   * Segundo y último paso del login cuando el usuario tiene 2FA habilitado.
   * Llama a: POST /api/auth/verificar-doble-factor
   */
  static async verificarDobleFactor(usuarioId: number, codigo: string): Promise<TokenRespuestaDTO> {
    const response = await api.post<TokenRespuestaDTO>('/auth/verificar-doble-factor', {
      usuarioId,
      codigo,
    });
    return response.data;
  }

  /**
   * Registra un nuevo usuario en la base de datos del sistema.
   * Llama a: POST /api/auth/registro
   */
  static async register(data: RegistroDTO): Promise<UsuarioDTO> {
    const response = await api.post<UsuarioDTO>('/auth/registro', data);
    return response.data;
  }

  /**
   * Primer paso de "olvidé mi contraseña": pide al backend que genere (y "envíe",
   * simulado por consola del lado del servidor) un link de reseteo.
   * Llama a: POST /api/auth/olvide-password
   *
   * El backend SIEMPRE responde 200 con el mismo mensaje genérico, exista o no la
   * cuenta (para no permitir enumerar emails registrados): no hay nada que
   * "distinguir" acá, solo se propaga el mensaje de éxito tal cual.
   */
  static async olvidePassword(email: string): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>('/auth/olvide-password', { email });
    return response.data;
  }

  /**
   * Segundo y último paso de "olvidé mi contraseña": consume el token recibido
   * (simulado, por consola del backend) para fijar una contraseña nueva.
   * Llama a: POST /api/auth/resetear-password
   */
  static async resetearPassword(token: string, passwordNueva: string): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>('/auth/resetear-password', {
      token,
      passwordNueva,
    });
    return response.data;
  }
}
