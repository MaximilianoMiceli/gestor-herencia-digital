import { api } from './api';

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

  /** Llama a: POST /api/invitaciones/{token}/procesar (público, no requiere sesión). */
  static async procesar(token: string, accion: 'aceptar' | 'rechazar'): Promise<{ mensaje: string }> {
    const response = await api.post<{ mensaje: string }>(`/invitaciones/${token}/procesar`, { accion });
    return response.data;
  }
}
