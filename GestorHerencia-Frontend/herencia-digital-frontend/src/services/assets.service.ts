/**
 * @file assets.service.ts
 * @description Servicio de comunicación HTTP encargado de la gestión de activos digitales
 * y asignaciones de herencia contra la API de ASP.NET Core.
 *
 * Implementa la lógica de consumo de endpoints protegidos mediante token JWT, integrando
 * la creación transaccional de un activo y su inmediata vinculación a un beneficiario invitado
 * por email.
 *
 * IMPORTANTE: el backend no tiene ninguna entidad "Beneficiario" independiente. Invitar a
 * alguien y asignarle un activo son la MISMA operación (POST /activosdigitales/{id}/asignaciones
 * con su email); no existe ningún endpoint "/api/beneficiarios" ni un Id de beneficiario propio.
 * Por eso "BeneficiarioResumen" (más abajo) es un agregado armado en el cliente a partir de las
 * asignaciones reales, no un recurso que exista tal cual en el servidor.
 */

import { API_BASE_URL } from '../constants/api';

/**
 * DTO para la creación de un nuevo activo digital.
 * Mapea directamente con el modelo de entrada 'ActivoDigitalCreacionDTO' del backend.
 */
export interface ActivoDigitalCreacionDTO {
  /** Nombre representativo del activo (ej: "Cuenta Banco Galicia", "Ethereum Wallet") */
  nombre: string;
  /** Tipo de activo digital según el enumerador TipoActivoDigital del backend (0=Banco, 2=Cripto, 4=Archivo) */
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
 * Clase de servicio que encapsula las llamadas fetch relativas a activos digitales,
 * asignaciones de herencia y su agregación en beneficiarios.
 */
export class AssetsService {
  /**
   * Registra un nuevo activo digital y, si se provee el email de un beneficiario, realiza
   * inmediatamente la asignación de herencia del 100% al mismo mediante una llamada
   * transaccional encadenada.
   *
   * Llama a:
   * 1. POST /api/activosdigitales (Crea el activo)
   * 2. POST /api/activosdigitales/{id}/asignaciones (Invita y asigna al beneficiario por email)
   *
   * @param token Token JWT del usuario autenticado actual.
   * @param asset Datos de creación del activo.
   * @param emailBeneficiario Email del beneficiario a invitar y asignar (opcional).
   * @param prioridad Prioridad asignada (opcional, se guarda dentro de la condición de liberación).
   * @returns Promesa con los datos del activo digital creado.
   * @throws Error con el mensaje devuelto por la API en caso de fallar cualquiera de los dos pasos.
   */
  static async createAsset(
    token: string,
    asset: ActivoDigitalCreacionDTO,
    emailBeneficiario?: string,
    prioridad?: string
  ): Promise<any> {
    // 1. Crear el activo digital principal
    const assetResponse = await fetch(`${API_BASE_URL}/activosdigitales`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(asset),
    });

    if (!assetResponse.ok) {
      let errorMsg = 'Error al crear el activo';
      try {
        const errorData = await assetResponse.json();
        errorMsg = errorData.mensaje || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    const createdAsset = await assetResponse.json();

    // 2. Si se invitó a un beneficiario por email, crear la asignación en la tabla intermedia
    if (emailBeneficiario) {
      // El backend recibe un array de asignaciones para permitir divisiones múltiples.
      // Aquí, por simplicidad de la pantalla, asignamos el 100% a un único beneficiario.
      const asignacionBody: AsignacionHerenciaCreacionDTO[] = [
        {
          emailBeneficiario,
          porcentajeAsignado: 100,
          // Guardamos la prioridad como metadato dentro del campo de condición de liberación
          condicionLiberacion: prioridad ? `Prioridad: ${prioridad}` : 'Asignado desde la app móvil',
        },
      ];

      const asignacionResponse = await fetch(
        `${API_BASE_URL}/activosdigitales/${createdAsset.id}/asignaciones`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(asignacionBody),
        }
      );

      if (!asignacionResponse.ok) {
        let errorMsg = 'Activo creado, pero falló la asignación al beneficiario';
        try {
          const errorData = await asignacionResponse.json();
          errorMsg = errorData.mensaje || errorMsg;
        } catch (e) {}
        throw new Error(errorMsg);
      }
    }

    return createdAsset;
  }

  /**
   * Obtiene la lista de herencias asignadas al usuario actual (Frame 24).
   * Llama a: GET /api/invitaciones/mis-herencias
   */
  static async getMisHerencias(token: string): Promise<MiHerenciaDTO[]> {
    const response = await fetch(`${API_BASE_URL}/invitaciones/mis-herencias`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al obtener las herencias';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Obtiene la lista de asignaciones de herencia asociadas a un activo.
   * Llama a: GET /api/activosdigitales/{assetId}/asignaciones
   */
  static async getAssignmentsForAsset(token: string, assetId: number): Promise<AsignacionDTO[]> {
    const response = await fetch(`${API_BASE_URL}/activosdigitales/${assetId}/asignaciones`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al obtener asignaciones del activo';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Obtiene todos los activos digitales creados por el usuario autenticado.
   * Llama a: GET /api/activosdigitales
   */
  static async getAssets(token: string): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/activos?pagina=1&limite=100`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = `Error al obtener los activos digitales (Status ${response.status})`;
      try {
        const errorText = await response.text();
        errorMsg += `: ${errorText}`;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    const data = await response.json();
    return data.items || data.Items || data.elementos || data.Elementos || [];
  }

  /**
   * Actualiza un activo digital existente.
   * Llama a: PUT /api/activosdigitales/{id}
   */
  static async updateAsset(
    token: string,
    id: number,
    asset: { nombre: string; tipo: number; descripcion: string }
  ): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/activosdigitales/${id}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(asset),
    });

    if (!response.ok) {
      let errorMsg = 'Error al actualizar el activo digital';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Elimina un activo digital por su ID (Frame 19).
   * Llama a: DELETE /api/activosdigitales/{id}
   */
  static async deleteAsset(token: string, id: number): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/activosdigitales/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al eliminar el activo digital';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }
  }

  /**
   * Elimina una asignación de herencia por su ID.
   * Llama a: DELETE /api/asignaciones/{id}
   */
  static async deleteAssignment(token: string, id: number): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/asignaciones/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al eliminar la asignación';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }
  }

  /**
   * Crea asignaciones de herencia en lote para un activo.
   * Llama a: POST /api/activosdigitales/{id}/asignaciones
   */
  static async createAssignments(
    token: string,
    assetId: number,
    assignments: AsignacionHerenciaCreacionDTO[]
  ): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/activosdigitales/${assetId}/asignaciones`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(assignments),
    });

    if (!response.ok) {
      let errorMsg = 'Error al asignar los beneficiarios';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
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
  static async getMisBeneficiarios(token: string): Promise<BeneficiarioResumen[]> {
    const activos = await this.getAssets(token);

    const listasPorActivo = await Promise.all(
      activos.map(async (activo): Promise<AsignacionConActivo[]> => {
        const asignaciones = await this.getAssignmentsForAsset(token, activo.id);
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
  static async eliminarBeneficiario(token: string, asignacionIds: number[]): Promise<void> {
    await Promise.all(asignacionIds.map((id) => this.deleteAssignment(token, id)));
  }
}

/**
 * DTO que representa una herencia recibida.
 */
export interface MiHerenciaDTO {
  asignacionId: number;
  titularNombre: string;
  parentesco: string;
  activoNombre: string;
  activoTipo: number;
  porcentaje: number;
  condicionLiberacion: string;
  disponible: boolean;
}
