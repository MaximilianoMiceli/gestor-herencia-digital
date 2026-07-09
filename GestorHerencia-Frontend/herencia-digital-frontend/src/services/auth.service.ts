import { API_BASE_URL } from '../constants/api';

export interface LoginDTO {
  email: string;
  password: string;
}

export interface RegistroDTO {
  nombre: string;
  email: string;
  password: string;
}

export interface TokenRespuestaDTO {
  token: string;
}

export interface UsuarioDTO {
  id: number;
  nombre: string;
  email: string;
  rol: string;
}

export class AuthService {
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
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorMsg;
      } catch (e) {
        // Ignorar error de parsing si no hay JSON
      }
      throw new Error(errorMsg);
    }

    return response.json();
  }

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
        // Ignorar error de parsing si no hay JSON
      }
      throw new Error(errorMsg);
    }

    return response.json();
  }
}
