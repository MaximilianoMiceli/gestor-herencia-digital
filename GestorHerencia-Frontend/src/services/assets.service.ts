// El backend no tiene una entidad "Beneficiario" propia: invitar a alguien y asignarle un
// activo son la misma operación. "BeneficiarioResumen" (más abajo) es un agregado armado
// en el cliente a partir de las asignaciones existentes, no un recurso real del servidor.
import { api } from './api';

export interface ActivoDigitalCreacionDTO {
  nombre: string;
  /** Tipo de activo digital según el enumerador TipoActivoDigital del backend. */
  tipo: number;
  descripcion: string;
}

export interface AsignacionHerenciaCreacionDTO {
  emailBeneficiario: string;
  /** Actualmente fijado al 100% para el único beneficiario. */
  porcentajeAsignado: number;
  condicionLiberacion: string;
}

export interface AsignacionDTO {
  id: number;
  activoDigitalId: number;
  /** Null si todavía no reclamó la invitación con una cuenta propia. */
  usuarioBeneficiarioId: number | null;
  emailInvitado: string;
  porcentajeAsignado: number;
  condicionLiberacion: string;
  /** 1 = Pendiente, 2 = Aceptado, 3 = Rechazado (EstadoBeneficiario del backend). */
  estado: number;
  usuarioOtorganteId: number;
  tokenInvitacion: string;
}

export interface AsignacionConActivo extends AsignacionDTO {
  activoNombre: string;
  activoTipo: number;
}

// Agrupado por email en el cliente a partir de todas las asignaciones que comparten
// "emailInvitado". No existe como recurso propio en el backend.
export interface BeneficiarioResumen {
  email: string;
  usuarioBeneficiarioId: number | null;
  /** Estado de la primera asignación encontrada, usado como representativo del beneficiario. */
  estado: number;
  asignaciones: AsignacionConActivo[];
}

export interface ActivoDigitalDTO {
  id: number;
  nombre: string;
  tipo: number;
  descripcion: string;
  usuarioId: number;
  nombreArchivoOriginal: string | null;
}

export interface MiHerenciaDTO {
  asignacionId: number;
  /** Necesario para pedir GET /api/activosdigitales/{id}/archivo. */
  activoDigitalId: number;
  titularId: number;
  titularNombre: string;
  activoNombre: string;
  activoTipo: number;
  porcentaje: number;
  condicionLiberacion: string;
  /** true solo cuando se aprobó el certificado de defunción del titular. */
  disponible: boolean;
  /** Nombre del enum EstadoBeneficiario del backend: "Pendiente" | "Aceptado" | "Rechazado". */
  estado: string;
  // El backend solo completa descripcion/nombreArchivoOriginal cuando "disponible" es
  // true: antes de confirmarse el fallecimiento, viajan null aunque ya hayas aceptado.
  descripcion: string | null;
  nombreArchivoOriginal: string | null;
}

export class AssetsService {
  // Crea el activo (POST /api/activosdigitales) y, si se provee email, encadena la
  // asignación de herencia del 100% (POST /api/activosdigitales/{id}/asignaciones).
  static async createAsset(
    asset: ActivoDigitalCreacionDTO,
    emailBeneficiario?: string,
    prioridad?: string
  ): Promise<ActivoDigitalDTO> {
    const assetResponse = await api.post<ActivoDigitalDTO>('/activosdigitales', asset);
    const createdAsset = assetResponse.data;

    if (emailBeneficiario) {
      const asignacionBody: AsignacionHerenciaCreacionDTO[] = [
        {
          emailBeneficiario,
          porcentajeAsignado: 100,
          condicionLiberacion: prioridad ? `Prioridad: ${prioridad}` : 'Asignado desde la app móvil',
        },
      ];

      await api.post(`/activosdigitales/${createdAsset.id}/asignaciones`, asignacionBody);
    }

    return createdAsset;
  }

  /** Llama a: GET /api/invitaciones/mis-herencias */
  static async getMisHerencias(): Promise<MiHerenciaDTO[]> {
    const response = await api.get<MiHerenciaDTO[]>('/invitaciones/mis-herencias');
    return response.data;
  }

  /** Llama a: GET /api/activosdigitales/{assetId}/asignaciones */
  static async getAssignmentsForAsset(assetId: number): Promise<AsignacionDTO[]> {
    const response = await api.get<AsignacionDTO[]>(`/activosdigitales/${assetId}/asignaciones`);
    return response.data;
  }

  /** Llama a: GET /api/activos (paginado; se pide una página grande para simplificar la UI) */
  static async getAssets(): Promise<ActivoDigitalDTO[]> {
    const response = await api.get<{ items?: ActivoDigitalDTO[]; elementos?: ActivoDigitalDTO[] }>(
      '/activos',
      { params: { pagina: 1, limite: 100 } }
    );
    return response.data.items ?? response.data.elementos ?? [];
  }

  /** Llama a: PUT /api/activosdigitales/{id} */
  static async updateAsset(
    id: number,
    asset: { nombre: string; tipo: number; descripcion: string }
  ): Promise<ActivoDigitalDTO> {
    const response = await api.put<ActivoDigitalDTO>(`/activosdigitales/${id}`, asset);
    return response.data;
  }

  /** Llama a: DELETE /api/activosdigitales/{id} */
  static async deleteAsset(id: number): Promise<void> {
    await api.delete(`/activosdigitales/${id}`);
  }

  /** Llama a: DELETE /api/asignaciones/{id} */
  static async deleteAssignment(id: number): Promise<void> {
    await api.delete(`/asignaciones/${id}`);
  }

  /** Llama a: POST /api/activosdigitales/{id}/asignaciones */
  static async createAssignments(
    assetId: number,
    assignments: AsignacionHerenciaCreacionDTO[]
  ): Promise<AsignacionDTO[]> {
    const response = await api.post<AsignacionDTO[]>(`/activosdigitales/${assetId}/asignaciones`, assignments);
    return response.data;
  }

  /** Llama a: PATCH /api/asignaciones/{id}/estado. 2 = Aceptado, 3 = Rechazado; el backend
   *  rechaza con 400 cualquier intento de volver a 1 = Pendiente. */
  static async actualizarEstadoAsignacion(id: number, nuevoEstado: 2 | 3): Promise<AsignacionDTO> {
    const response = await api.patch<AsignacionDTO>(`/asignaciones/${id}/estado`, { nuevoEstado });
    return response.data;
  }

  // Llama a: POST /api/activosdigitales/{id}/archivo. RN no tiene File/Blob nativo:
  // expo-document-picker da { uri, name, mimeType }, que es lo que FormData necesita
  // para leer el archivo del disco. El "as any" cubre que los tipos de FormData en RN
  // no contemplan esta forma (pensados para la spec web).
  static async subirArchivoActivo(
    id: number,
    archivo: { uri: string; name: string; mimeType: string }
  ): Promise<ActivoDigitalDTO> {
    const formData = new FormData();

    // El nombre de campo "archivo" debe coincidir con [FromForm] IFormFile archivo del controller.
    formData.append('archivo', {
      uri: archivo.uri,
      name: archivo.name,
      type: archivo.mimeType,
    } as any);

    const response = await api.post<ActivoDigitalDTO>(`/activosdigitales/${id}/archivo`, formData);
    return response.data;
  }

  // No existe un endpoint "/api/beneficiarios": se arma en el cliente recorriendo
  // GET /api/activos y, para cada uno, GET /api/activosdigitales/{id}/asignaciones.
  static async getMisBeneficiarios(): Promise<BeneficiarioResumen[]> {
    const activos = await this.getAssets();

    const listasPorActivo = await Promise.all(
      activos.map(async (activo): Promise<AsignacionConActivo[]> => {
        const asignaciones = await this.getAssignmentsForAsset(activo.id);
        return asignaciones.map((asignacion) => ({
          ...asignacion,
          activoNombre: activo.nombre,
          activoTipo: activo.tipo,
        }));
      })
    );

    const mapaPorEmail = new Map<string, BeneficiarioResumen>();

    for (const asignacion of listasPorActivo.flat()) {
      const existente = mapaPorEmail.get(asignacion.emailInvitado);
      if (existente) {
        existente.asignaciones.push(asignacion);
      } else {
        mapaPorEmail.set(asignacion.emailInvitado, {
          email: asignacion.emailInvitado,
          usuarioBeneficiarioId: asignacion.usuarioBeneficiarioId,
          estado: asignacion.estado,
          asignaciones: [asignacion],
        });
      }
    }

    return Array.from(mapaPorEmail.values());
  }

  // No es una entidad propia del backend: "eliminarlo" es borrar cada asignación que lo vincula.
  static async eliminarBeneficiario(asignacionIds: number[]): Promise<void> {
    await Promise.all(asignacionIds.map((id) => this.deleteAssignment(id)));
  }
}
