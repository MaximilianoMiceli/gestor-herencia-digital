/**
 * @file assets.service.ts
 * @description Servicio de comunicación HTTP encargado de la gestión de activos digitales
 * y asignaciones de herencia contra la API de ASP.NET Core.
 * 
 * Implementa la lógica de consumo de endpoints protegidos mediante token JWT, integrando
 * la creación transaccional de un activo y su inmediata vinculación a un beneficiario asignado.
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
 * Mapea directamente con el modelo esperado en el endpoint 'POST {id}/asignaciones' del backend.
 */
export interface AsignacionHerenciaCreacionDTO {
  /** ID único del beneficiario al cual se le transfiere el activo */
  beneficiarioId: number;
  /** Porcentaje de herencia asignado (actualmente fijado al 100% por defecto para el primer beneficiario) */
  porcentajeAsignado: number;
  /** Nivel de prioridad u otras condiciones bajo las cuales se liberará el activo en el futuro */
  condicionLiberacion: string;
}

/**
 * DTO que representa a un beneficiario en el sistema.
 * Coincide con la respuesta del endpoint 'GET /api/beneficiarios' del backend.
 */
export interface BeneficiarioDTO {
  id: number;
  nombre: string;
  email: string;
  parentesco: string;
  estado?: number;
}

/**
 * Clase de servicio que encapsula las llamadas fetch relativas a activos digitales y beneficiarios.
 */
export class AssetsService {
  /**
   * Obtiene la lista completa de beneficiarios asociados al usuario autenticado.
   * Llama a: GET /api/beneficiarios
   * 
   * @param token Token JWT del usuario autenticado actual.
   * @returns Promesa con el listado de beneficiarios.
   * @throws Error si el servidor responde con un código no exitoso (401, 404, 500).
   */
  static async getBeneficiarios(token: string): Promise<BeneficiarioDTO[]> {
    const response = await fetch(`${API_BASE_URL}/beneficiarios`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al obtener beneficiarios';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Obtiene un beneficiario por su ID (Frame 9).
   * Llama a: GET /api/beneficiarios/{id}
   */
  static async getBeneficiarioPorId(token: string, id: number): Promise<BeneficiarioDTO> {
    const response = await fetch(`${API_BASE_URL}/beneficiarios/${id}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al obtener detalles del beneficiario';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Registra un nuevo activo digital y, si se provee un beneficiario, realiza inmediatamente
   * la asignación de herencia del 100% al mismo mediante una llamada transaccional encadenada.
   * 
   * Llama a:
   * 1. POST /api/activosdigitales (Crea el activo)
   * 2. POST /api/activosdigitales/{id}/asignaciones (Asigna el beneficiario al activo)
   * 
   * @param token Token JWT del usuario autenticado actual.
   * @param asset Datos de creación del activo.
   * @param beneficiarioId ID del beneficiario a asignar (opcional).
   * @param prioridad Prioridad asignada (opcional, se guarda dentro de la condición de liberación).
   * @returns Promesa con los datos del activo digital creado.
   * @throws Error con el mensaje devuelto por la API en caso de fallar cualquiera de los dos pasos.
   */
  static async createAsset(
    token: string,
    asset: ActivoDigitalCreacionDTO,
    beneficiarioId?: number,
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

    // 2. Si se asignó un beneficiario, crear la asignación en la tabla intermedia
    if (beneficiarioId) {
      // El backend recibe un array de asignaciones para permitir divisiones múltiples.
      // Aquí, por simplicidad de la pantalla, asignamos el 100% a un único beneficiario.
      const asignacionBody: AsignacionHerenciaCreacionDTO[] = [
        {
          beneficiarioId,
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
   * Registra un nuevo beneficiario asociado al usuario autenticado (Frame 10).
   * Llama a: POST /api/beneficiarios
   * 
   * @param token Token JWT del usuario autenticado actual.
   * @param beneficiario Datos de creación del beneficiario (nombre, email, parentesco).
   */
  static async createBeneficiario(
    token: string,
    beneficiario: { nombre: string; email: string; parentesco: string }
  ): Promise<BeneficiarioDTO> {
    const response = await fetch(`${API_BASE_URL}/beneficiarios`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(beneficiario),
    });

    if (!response.ok) {
      let errorMsg = 'Error al crear el beneficiario';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }

    return response.json();
  }

  /**
   * Elimina un beneficiario de la base de datos (Frame 9 / 28).
   * Llama a: DELETE /api/beneficiarios/{id}
   */
  static async deleteBeneficiario(token: string, id: number): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/beneficiarios/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      let errorMsg = 'Error al eliminar el beneficiario';
      try {
        const errorData = await response.json();
        errorMsg = errorData.mensaje || errorData.message || errorMsg;
      } catch (e) {}
      throw new Error(errorMsg);
    }
  }

  /**
   * Obtiene la lista de asignaciones de herencia asociadas a un activo (Frame 9).
   * Llama a: GET /api/activos/{assetId}/asignaciones
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
}

/**
 * DTO que representa una asignación de herencia.
 */
export interface AsignacionDTO {
  id: number;
  activoDigitalId: number;
  beneficiarioId: number;
  porcentajeAsignado: number;
  condicionLiberacion: string;
  usuarioId: number;
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
