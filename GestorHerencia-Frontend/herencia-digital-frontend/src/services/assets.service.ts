import { API_BASE_URL } from '../constants/api';

export interface ActivoDigitalCreacionDTO {
  nombre: string;
  tipo: number; // TipoActivoDigital enum
  descripcion: string;
}

export interface AsignacionHerenciaCreacionDTO {
  beneficiarioId: number;
  porcentajeAsignado: number;
  condicionLiberacion: string;
}

export interface BeneficiarioDTO {
  id: number;
  nombre: string;
  email: string;
  parentesco: string;
}

export class AssetsService {
  static async getBeneficiarios(token: string): Promise<BeneficiarioDTO[]> {
    const response = await fetch(`${API_BASE_URL}/beneficiarios`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Error al obtener beneficiarios');
    }

    return response.json();
  }

  static async createAsset(
    token: string,
    asset: ActivoDigitalCreacionDTO,
    beneficiarioId?: number,
    prioridad?: string
  ): Promise<any> {
    // 1. Crear el activo digital
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

    // 2. Si se asignó un beneficiario, crear la asignación
    if (beneficiarioId) {
      const asignacionBody: AsignacionHerenciaCreacionDTO[] = [
        {
          beneficiarioId,
          porcentajeAsignado: 100, // 100% asignado al único beneficiario por defecto en esta pantalla
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
}
