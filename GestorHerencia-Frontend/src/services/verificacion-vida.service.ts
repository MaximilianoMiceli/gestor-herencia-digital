import { api } from './api';

/** 1 = Push, 2 = Email, 3 = Sms (MetodoNotificacion del backend). */
export type MetodoNotificacion = 1 | 2 | 3;

/**
 * 1 = Activo, 2 = RecordatorioEnviado, 3 = EsperandoCertificado,
 * 4 = CertificadoEnRevision, 5 = FallecimientoConfirmado, 6 = HerenciaLiberada.
 */
export type EstadoVerificacionVida = 1 | 2 | 3 | 4 | 5 | 6;

export interface ConfiguracionVerificacionVidaDTO {
  usuarioId: number;
  activo: boolean;
  frecuenciaMeses: number;
  metodo: MetodoNotificacion;
  /** Id de Usuario del contacto de confianza (NO un email): debe ser un beneficiario
   *  ya Aceptado y con cuenta propia (ver la regla de negocio en el backend). */
  contactoConfianzaId: number | null;
  /** Nombre del contacto, ya resuelto por el backend para no necesitar otra consulta. */
  contactoConfianzaNombre: string | null;
  ultimoCheckIn: string;
  estado: EstadoVerificacionVida;
  recordatoriosEnviados: number;
  fechaUltimoRecordatorio: string | null;
  fechaProtocoloActivado: string | null;
}

export interface ConfiguracionVerificacionVidaActualizacionDTO {
  activo: boolean;
  /** Solo se aceptan 3, 6 o 12 (validado también del lado del servidor). */
  frecuenciaMeses: number;
  metodo: MetodoNotificacion;
  /** Nullable solo si "activo" es false: para activar el monitoreo, el backend exige
   *  un contacto de confianza ya Aceptado. */
  contactoConfianzaId: number | null;
}

export class VerificacionVidaService {
  /** Llama a: GET /api/verificacion-vida/configuracion */
  static async obtenerConfiguracion(): Promise<ConfiguracionVerificacionVidaDTO> {
    const response = await api.get<ConfiguracionVerificacionVidaDTO>('/verificacion-vida/configuracion');
    return response.data;
  }

  /** Llama a: PUT /api/verificacion-vida/configuracion */
  static async guardarConfiguracion(
    configuracion: ConfiguracionVerificacionVidaActualizacionDTO
  ): Promise<ConfiguracionVerificacionVidaDTO> {
    const response = await api.put<ConfiguracionVerificacionVidaDTO>(
      '/verificacion-vida/configuracion',
      configuracion
    );
    return response.data;
  }

  /** Llama a: POST /api/verificacion-vida/check-in. Resetea el reloj de vencimiento y
   *  cancela cualquier certificado de defunción pendiente sobre esta cuenta. */
  static async registrarCheckIn(): Promise<ConfiguracionVerificacionVidaDTO> {
    const response = await api.post<ConfiguracionVerificacionVidaDTO>('/verificacion-vida/check-in');
    return response.data;
  }
}
