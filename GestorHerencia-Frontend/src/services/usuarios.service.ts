/**
 * @file usuarios.service.ts
 * @description Servicio HTTP de gestión del perfil del usuario autenticado: editar
 * nombre/email, cambiar contraseña y activar/desactivar el segundo factor (2FA) por
 * email. Todas las rutas viven bajo /api/usuarios/{id}, y en TODAS ellas el backend
 * exige que "{id}" sea el propio usuario autenticado (ver el chequeo de "ownership"
 * en UsuariosController): pasar el Id de otro usuario devuelve 403 Forbidden.
 */

import { api } from './api';

/** DTO de un usuario devuelto por el backend (ver UsuarioDTO.cs). */
export interface UsuarioDTO {
  id: number;
  nombre: string;
  email: string;
  /** Documento Nacional de Identidad: 7 u 8 dígitos. */
  dni: string;
  /** Fecha de nacimiento en formato ISO ("AAAA-MM-DDTHH:mm:ss"). */
  fechaNacimiento: string;
  fechaCreacion: string;
  /** 0 = Usuario, 1 = Administrador (RolUsuario del backend) */
  rol: number;
  dobleFactorHabilitado: boolean;
}

export class UsuariosService {
  /** Llama a: GET /api/usuarios/{id} */
  static async obtenerPorId(id: number): Promise<UsuarioDTO> {
    const response = await api.get<UsuarioDTO>(`/usuarios/${id}`);
    return response.data;
  }

  /** Llama a: PUT /api/usuarios/{id} (actualiza Nombre/Email/Dni/FechaNacimiento) */
  static async actualizarPerfil(
    id: number,
    nombre: string,
    email: string,
    dni: string,
    fechaNacimiento: string
  ): Promise<UsuarioDTO> {
    const response = await api.put<UsuarioDTO>(`/usuarios/${id}`, { nombre, email, dni, fechaNacimiento });
    return response.data;
  }

  /**
   * Llama a: PUT /api/usuarios/{id}/password
   * El backend responde 204 No Content (sin body) cuando el cambio es exitoso.
   */
  static async cambiarPassword(id: number, passwordActual: string, passwordNueva: string): Promise<void> {
    await api.put(`/usuarios/${id}/password`, { passwordActual, passwordNueva });
  }

  /** Llama a: PUT /api/usuarios/{id}/doble-factor */
  static async actualizarDobleFactor(id: number, habilitado: boolean): Promise<UsuarioDTO> {
    const response = await api.put<UsuarioDTO>(`/usuarios/${id}/doble-factor`, { habilitado });
    return response.data;
  }
}
