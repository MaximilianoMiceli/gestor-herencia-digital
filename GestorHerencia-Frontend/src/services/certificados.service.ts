import { api } from './api';

/** 1 = Pendiente, 2 = Aprobado, 3 = Rechazado, 4 = CanceladoPorActividad. */
export type EstadoCertificadoDefuncion = 1 | 2 | 3 | 4;

export interface CertificadoDefuncionDTO {
  id: number;
  usuarioTitularId: number;
  usuarioTitularNombre: string;
  subidoPorUsuarioId: number;
  subidoPorNombre: string;
  nombreArchivoOriginal: string;
  fechaSubida: string;
  estado: EstadoCertificadoDefuncion;
  revisadoPorUsuarioId: number | null;
  fechaRevision: string | null;
  motivoRechazo: string | null;
}

export class CertificadosService {
  /** Llama a: POST /api/certificados-defuncion. Solo puede completarse si quien llama
   *  ya es un heredero ACEPTADO de ese titular (el backend lo valida). */
  static async subirCertificado(
    usuarioTitularId: number,
    archivo: { uri: string; name: string; mimeType: string }
  ): Promise<CertificadoDefuncionDTO> {
    const formData = new FormData();

    // Viaja como campo de texto del mismo formulario multipart: el controller lo recibe
    // con [FromForm] int, igual que el archivo.
    formData.append('usuarioTitularId', String(usuarioTitularId));
    formData.append('archivo', {
      uri: archivo.uri,
      name: archivo.name,
      type: archivo.mimeType,
    } as any);

    const response = await api.post<CertificadoDefuncionDTO>('/certificados-defuncion', formData);
    return response.data;
  }

  /** Llama a: GET /api/certificados-defuncion/pendientes (solo rol Administrador) */
  static async obtenerPendientes(): Promise<CertificadoDefuncionDTO[]> {
    const response = await api.get<CertificadoDefuncionDTO[]>('/certificados-defuncion/pendientes');
    return response.data;
  }

  /** Llama a: PATCH /api/certificados-defuncion/{id}/aprobar. Libera TODOS los bienes
   *  aceptados del titular en una única transacción del servidor. Solo rol Administrador. */
  static async aprobar(id: number): Promise<CertificadoDefuncionDTO> {
    const response = await api.patch<CertificadoDefuncionDTO>(`/certificados-defuncion/${id}/aprobar`);
    return response.data;
  }

  /** Llama a: PATCH /api/certificados-defuncion/{id}/rechazar. No libera nada; se puede
   *  volver a subir otro certificado distinto. Solo rol Administrador. */
  static async rechazar(id: number, motivo: string): Promise<CertificadoDefuncionDTO> {
    const response = await api.patch<CertificadoDefuncionDTO>(`/certificados-defuncion/${id}/rechazar`, {
      motivo,
    });
    return response.data;
  }
}
