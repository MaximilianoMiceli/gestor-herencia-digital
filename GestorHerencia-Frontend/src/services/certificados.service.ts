/**
 * @file certificados.service.ts
 * @description Servicio HTTP del flujo de certificados de defunción: un heredero ya
 * ACEPTADO sube el certificado del titular fallecido (multipart, vía expo-document-picker),
 * y un Administrador lo aprueba (liberando todos los bienes del titular) o lo rechaza.
 */

import { api } from './api';

/** 1 = Pendiente, 2 = Aprobado, 3 = Rechazado, 4 = CanceladoPorActividad. */
export type EstadoCertificadoDefuncion = 1 | 2 | 3 | 4;

/** DTO devuelto por el backend (ver CertificadoDefuncionDTO.cs). */
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
  /**
   * Sube el certificado de defunción de un titular. Solo puede completarse con éxito
   * si quien llama (el usuario autenticado) ya es un heredero ACEPTADO de ese titular
   * (el backend lo valida; ver CertificadoDefuncionService.SubirCertificadoAsync).
   * Llama a: POST /api/certificados-defuncion
   *
   * Igual que en `AssetsService.subirArchivoActivo`, el archivo viaja como un objeto
   * `{ uri, name, mimeType }` (la forma que devuelve expo-document-picker) dentro de un
   * FormData: React Native lee el archivo del disco a partir de esa "uri" en el momento
   * de armar el body real de la request, no antes.
   */
  static async subirCertificado(
    usuarioTitularId: number,
    archivo: { uri: string; name: string; mimeType: string }
  ): Promise<CertificadoDefuncionDTO> {
    const formData = new FormData();

    // "usuarioTitularId" viaja como CAMPO DE TEXTO del mismo formulario multipart (no
    // como query string ni JSON): el controller lo recibe con [FromForm] int, igual que
    // el archivo. FormData admite mezclar campos de texto y binarios sin problema.
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

  /**
   * Aprueba el certificado: libera TODOS los bienes aceptados del titular en una única
   * transacción del lado del servidor. Solo rol Administrador.
   * Llama a: PATCH /api/certificados-defuncion/{id}/aprobar
   */
  static async aprobar(id: number): Promise<CertificadoDefuncionDTO> {
    const response = await api.patch<CertificadoDefuncionDTO>(`/certificados-defuncion/${id}/aprobar`);
    return response.data;
  }

  /**
   * Rechaza el certificado con un motivo obligatorio (no libera nada; el titular o
   * cualquier heredero puede volver a subir otro certificado distinto). Solo rol Administrador.
   * Llama a: PATCH /api/certificados-defuncion/{id}/rechazar
   */
  static async rechazar(id: number, motivo: string): Promise<CertificadoDefuncionDTO> {
    const response = await api.patch<CertificadoDefuncionDTO>(`/certificados-defuncion/${id}/rechazar`, {
      motivo,
    });
    return response.data;
  }
}
