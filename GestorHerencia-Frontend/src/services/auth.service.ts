import { api } from './api';

export interface LoginDTO {
  email: string;
  password: string;
}

export interface RegistroDTO {
  nombre: string;
  email: string;
  password: string;
  dni: string;
  /** Fecha de nacimiento en formato ISO ("AAAA-MM-DD"). El backend exige mayoría de edad. */
  fechaNacimiento: string;
}

// Con 2FA habilitado llega { token: "", requiereDobleFactor: true, usuarioId }: todavía
// no hay sesión, hay que llamar a verificarDobleFactor con el código recibido por email.
export interface TokenRespuestaDTO {
  token: string;
  requiereDobleFactor: boolean;
  usuarioId: number | null;
}

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
  /** Llama a: POST /api/auth/login. Este servicio solo transporta la respuesta: si
   *  requiereDobleFactor es true, el llamador decide navegar a "ingresar código". */
  static async login(data: LoginDTO): Promise<TokenRespuestaDTO> {
    const response = await api.post<TokenRespuestaDTO>('/auth/login', data);
    return response.data;
  }

  /** Llama a: POST /api/auth/verificar-doble-factor */
  static async verificarDobleFactor(usuarioId: number, codigo: string): Promise<TokenRespuestaDTO> {
    const response = await api.post<TokenRespuestaDTO>('/auth/verificar-doble-factor', {
      usuarioId,
      codigo,
    });
    return response.data;
  }

  /** Llama a: POST /api/auth/registro */
  static async register(data: RegistroDTO): Promise<UsuarioDTO> {
    const response = await api.post<UsuarioDTO>('/auth/registro', data);
    return response.data;
  }

  /** Llama a: POST /api/auth/olvide-password. Responde siempre el mismo mensaje
   *  genérico, exista o no la cuenta, para no permitir enumerar emails registrados. */
  static async olvidePassword(email: string): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>('/auth/olvide-password', { email });
    return response.data;
  }

  /** Llama a: POST /api/auth/resetear-password */
  static async resetearPassword(token: string, passwordNueva: string): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>('/auth/resetear-password', {
      token,
      passwordNueva,
    });
    return response.data;
  }
}
