/**
 * @file assets.service.ts
 * @description Servicio de comunicación HTTP encargado de la gestión de activos digitales
 * y asignaciones de herencia contra la API de ASP.NET Core.
 *
 * Todas las llamadas pasan por el cliente Axios centralizado (ver api.ts): el interceptor
 * de request ya inyecta el header "Authorization: Bearer <token>" automáticamente, así
 * que NINGÚN método de acá recibe un parámetro "token" (a diferencia de la versión
 * anterior, que lo pedía y lo repetía a mano en cada `fetch`).
 *
 * IMPORTANTE: el backend no tiene ninguna entidad "Beneficiario" independiente. Invitar a
 * alguien y asignarle un activo son la MISMA operación (POST /activosdigitales/{id}/asignaciones
 * con su email); no existe ningún endpoint "/api/beneficiarios" ni un Id de beneficiario propio.
 * Por eso "BeneficiarioResumen" (más abajo) es un agregado armado en el cliente a partir de las
 * asignaciones reales, no un recurso que exista tal cual en el servidor.
 */

import { api } from './api';

/**
 * DTO para la creación de un nuevo activo digital.
 * Mapea directamente con el modelo de entrada 'ActivoDigitalCreacionDTO' del backend.
 */
export interface ActivoDigitalCreacionDTO {
  /** Nombre representativo del activo (ej: "Cuenta Banco Galicia", "Ethereum Wallet") */
  nombre: string;
  /** Tipo de activo digital según el enumerador TipoActivoDigital del backend */
  tipo: number;
  /** Detalles e instrucciones específicas del activo, serializados en texto legible o JSON */
  descripcion: string;
}

/**
 * DTO para la creación de una asignación de herencia.
 * Mapea directamente con 'AsignacionHerenciaCreacionDTO' del backend: el beneficiario se
 * identifica por email (no por un Id propio), ya que puede no tener cuenta todavía.
 */
export interface AsignacionHerenciaCreacionDTO {
  /** Email de la persona invitada como beneficiaria del activo */
  emailBeneficiario: string;
  /** Porcentaje de herencia asignado (actualmente fijado al 100% para el único beneficiario) */
  porcentajeAsignado: number;
  /** Nivel de prioridad u otras condiciones bajo las cuales se liberará el activo en el futuro */
  condicionLiberacion: string;
}

/**
 * DTO que representa una asignación de herencia devuelta por el backend.
 * Mapea directamente con 'AsignacionHerenciaDTO' (ver Herencia.Business/Dtos).
 */
export interface AsignacionDTO {
  id: number;
  activoDigitalId: number;
  /** Id del Usuario beneficiario, o null si todavía no reclamó la invitación con una cuenta */
  usuarioBeneficiarioId: number | null;
  /** Email con el que se invitó a esta persona, exista o no cuenta todavía */
  emailInvitado: string;
  porcentajeAsignado: number;
  condicionLiberacion: string;
  /** 1 = Pendiente, 2 = Aceptado, 3 = Rechazado (EstadoBeneficiario del backend) */
  estado: number;
  usuarioOtorganteId: number;
  tokenInvitacion: string;
}

/**
 * Una asignación puntual, enriquecida con los datos del activo al que pertenece.
 * Se arma en el cliente combinando AsignacionDTO con el activo que la originó.
 */
export interface AsignacionConActivo extends AsignacionDTO {
  activoNombre: string;
  activoTipo: number;
}

/**
 * Resumen de un beneficiario agrupado por email, construido en el cliente a partir de
 * todas las asignaciones (posiblemente sobre varios activos distintos) que comparten el
 * mismo 'emailInvitado'. No existe como recurso propio en el backend.
 */
export interface BeneficiarioResumen {
  email: string;
  usuarioBeneficiarioId: number | null;
  /** Estado de la primera asignación encontrada; se usa como estado representativo del beneficiario */
  estado: number;
  asignaciones: AsignacionConActivo[];
}

/**
 * DTO de un activo digital devuelto por el backend (ver ActivoDigitalDTO.cs).
 */
export interface ActivoDigitalDTO {
  id: number;
  nombre: string;
  tipo: number;
  descripcion: string;
  usuarioId: number;
  /** Nombre de exhibición del archivo adjunto, o null si no tiene ninguno */
  nombreArchivoOriginal: string | null;
}

/** DTO que representa una herencia recibida (ver InvitacionesController.MiHerenciaDTO). */
export interface MiHerenciaDTO {
  asignacionId: number;
  titularId: number;
  titularNombre: string;
  activoNombre: string;
  activoTipo: number;
  porcentaje: number;
  condicionLiberacion: string;
  /** true solo cuando se aprobó el certificado de defunción del titular (FechaLiberacion != null). */
  disponible: boolean;
  /** El backend lo serializa como el NOMBRE del enum EstadoBeneficiario: "Pendiente" | "Aceptado" | "Rechazado". */
  estado: string;
}

export class AssetsService {
  /**
   * Registra un nuevo activo digital y, si se provee el email de un beneficiario, realiza
   * inmediatamente la asignación de herencia del 100% al mismo mediante una llamada
   * transaccional encadenada.
   *
   * Llama a:
   * 1. POST /api/activosdigitales (Crea el activo)
   * 2. POST /api/activosdigitales/{id}/asignaciones (Invita y asigna al beneficiario por email)
   */
  static async createAsset(
    asset: ActivoDigitalCreacionDTO,
    emailBeneficiario?: string,
    prioridad?: string
  ): Promise<ActivoDigitalDTO> {
    // 1. Crear el activo digital principal.
    const assetResponse = await api.post<ActivoDigitalDTO>('/activosdigitales', asset);
    const createdAsset = assetResponse.data;

    // 2. Si se invitó a un beneficiario por email, crear la asignación en la tabla intermedia.
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

  /**
   * PATCH /api/asignaciones/{id}/estado — el BENEFICIARIO acepta o rechaza una herencia
   * pendiente. "nuevoEstado" usa los mismos valores numéricos que el enum EstadoBeneficiario
   * del backend: 2 = Aceptado, 3 = Rechazado (1 = Pendiente no es una transición válida,
   * el backend la rechaza con 400 si se intenta volver atrás).
   */
  static async actualizarEstadoAsignacion(id: number, nuevoEstado: 2 | 3): Promise<AsignacionDTO> {
    const response = await api.patch<AsignacionDTO>(`/asignaciones/${id}/estado`, { nuevoEstado });
    return response.data;
  }

  /**
   * Adjunta (o reemplaza) el archivo de un activo digital ya creado.
   * Llama a: POST /api/activosdigitales/{id}/archivo
   *
   * --- ¿Cómo funciona FormData con archivos en React Native? ---
   * A diferencia del navegador (donde `FormData` se arma a partir de un `<input type="file">`
   * y un `File`/`Blob` real), en React Native no existe un objeto `File` nativo: lo que
   * `expo-document-picker` devuelve es un objeto plano `{ uri, name, mimeType, size }`, donde
   * `uri` es una ruta local al archivo temporal (ej: "file:///data/.../documento.pdf" en
   * Android, o un "content://..." en algunos casos). Para adjuntarlo a un FormData, React
   * Native reconoce una convención especial: en vez de pasar un Blob, se le pasa un objeto
   * `{ uri, name, type }` y el propio motor nativo (no JavaScript) se encarga de LEER el
   * archivo del disco y transmitirlo como parte del cuerpo multipart en el momento de hacer
   * la request real. Por eso "as any" es necesario acá: el tipo `FormDataValue` de la
   * librería de tipos de React Native no contempla esta forma (fue pensada mirando la spec
   * web), pero es el único formato que el runtime nativo de React Native sabe interpretar.
   */
  static async subirArchivoActivo(
    id: number,
    archivo: { uri: string; name: string; mimeType: string }
  ): Promise<ActivoDigitalDTO> {
    // FormData es la única forma de mandar un archivo binario + eventuales campos de texto
    // en una request "multipart/form-data" (el formato que el backend espera en
    // ActivosDigitalesController.SubirArchivo, vía [FromForm] IFormFile).
    const formData = new FormData();

    // El nombre del campo ("archivo") DEBE coincidir exactamente con el parámetro
    // `[FromForm] IFormFile archivo` del controller: ASP.NET Core arma el binding a partir
    // de ese nombre de campo, no del nombre del archivo en sí.
    formData.append('archivo', {
      uri: archivo.uri,
      name: archivo.name,
      type: archivo.mimeType,
    } as any);

    // No se pasa ningún header "Content-Type" manual acá: axios (y, por debajo, el XHR de
    // React Native) detecta que el body es un FormData y arma automáticamente
    // "multipart/form-data; boundary=----XXXX" con el boundary correcto. Fijarlo a mano
    // rompería el request (faltaría el boundary real que separa cada parte del formulario).
    const response = await api.post<ActivoDigitalDTO>(`/activosdigitales/${id}/archivo`, formData);
    return response.data;
  }

  /**
   * Arma el listado de "mis beneficiarios" agregando, en el cliente, todas las asignaciones
   * de todos mis activos y agrupándolas por email invitado.
   *
   * No existe un endpoint "/api/beneficiarios": un beneficiario es, en el modelo real del
   * backend, simplemente la persona (identificada por email) a la que le asigné uno o más
   * de mis activos. Por eso se arma recorriendo GET /api/activos y, para cada uno,
   * GET /api/activosdigitales/{id}/asignaciones.
   */
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

  /**
   * "Elimina" a un beneficiario eliminando todas sus asignaciones de herencia.
   * Como el beneficiario no es una entidad propia del backend, quitarlo del todo equivale
   * a borrar cada AsignacionHerencia que lo vincula con alguno de mis activos.
   */
  static async eliminarBeneficiario(asignacionIds: number[]): Promise<void> {
    await Promise.all(asignacionIds.map((id) => this.deleteAssignment(id)));
  }
}
