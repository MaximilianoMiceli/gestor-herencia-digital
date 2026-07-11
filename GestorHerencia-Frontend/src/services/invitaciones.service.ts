/**
 * @file invitaciones.service.ts
 * @description Servicio HTTP del flujo PÚBLICO de invitaciones (aceptar/rechazar una
 * herencia sin necesitar sesión iniciada). Antes esta lógica vivía duplicada, con
 * `fetch` directo, tanto en invitacion.tsx como en login.tsx (para el auto-aceptar tras
 * loguearse desde una invitación); ahora ambas pantallas llaman a este único servicio.
 */

import { api } from './api';

/** DTO devuelto por GET /api/invitaciones/{token} (ver InvitacionDTO en InvitacionesController.cs). */
export interface InvitacionDTO {
  token: string;
  emisorNombre: string;
  /** Vacío si la persona invitada todavía no reclamó la invitación con una cuenta propia. */
  beneficiarioNombre: string;
  beneficiarioEmail: string;
}

export class InvitacionesService {
  /** Llama a: GET /api/invitaciones/{token} (público, no requiere sesión) */
  static async obtener(token: string): Promise<InvitacionDTO> {
    const response = await api.get<InvitacionDTO>(`/invitaciones/${token}`);
    return response.data;
  }

  /**
   * Llama a: POST /api/invitaciones/{token}/procesar (público: si hay sesión iniciada,
   * el interceptor de request igual adjunta el Bearer token, pero el backend no lo exige).
   */
  static async procesar(token: string, accion: 'aceptar' | 'rechazar'): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>(`/invitaciones/${token}/procesar`, { accion });
    return response.data;
  }
}
