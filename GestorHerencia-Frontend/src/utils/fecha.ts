/**
 * @file fecha.ts
 * @description Utilidades compartidas para el campo "fecha de nacimiento" (register.tsx
 * y editar-perfil.tsx). No hay selector de fecha nativo instalado, así que se pide como
 * texto libre "DD/MM/AAAA" y se valida/convierte acá para que ambas pantallas apliquen
 * la misma regla que el backend (mayoría de edad).
 *
 * El `Date` que devuelve este módulo se arma siempre con el constructor numérico
 * (año, mes, día) en hora LOCAL, nunca parseando un string ISO. Es deliberado: los
 * llamadores reconstruyen el "AAAA-MM-DD" para el backend leyendo getFullYear/getMonth/
 * getDate (hora local) en vez de usar `Date.toISOString()`, que convierte a UTC primero.
 * En husos horarios negativos (Argentina, UTC-3) ese paso extra puede correr la fecha un
 * día hacia atrás — el bug real que rompió altas de usuarios con fecha de nacimiento
 * cercana a medianoche. Mantener todo en hora local evita esa clase de error.
 */

/** true si el string tiene la forma DD/MM/AAAA con separadores "/". No valida todavía
 *  que sea una fecha calendario real (eso lo hace parsearFechaDDMMAAAA). */
export function tieneFormatoDDMMAAAA(texto: string): boolean {
  return /^\d{2}\/\d{2}\/\d{4}$/.test(texto.trim());
}

/**
 * Convierte "DD/MM/AAAA" a un objeto Date real, o null si el string no representa una
 * fecha calendario válida (ej: "31/02/2000", donde febrero nunca tiene 31 días).
 *
 * ¿Por qué no alcanza con `new Date(...)`? El constructor de Date en JavaScript es
 * "permisivo": `new Date(2000, 1, 31)` no lanza ningún error para un 31 de febrero, sino
 * que "hace rollover" silenciosamente hacia el 2 o 3 de marzo. Sin la verificación
 * explícita de abajo (comparar los componentes de vuelta contra lo que el usuario
 * escribió), se aceptarían fechas imposibles sin que el usuario se entere.
 */
export function parsearFechaDDMMAAAA(texto: string): Date | null {
  if (!tieneFormatoDDMMAAAA(texto)) return null;

  const [diaStr, mesStr, anioStr] = texto.trim().split('/');
  const dia = Number(diaStr);
  const mes = Number(mesStr);
  const anio = Number(anioStr);

  // El mes se pasa "menos 1" porque el constructor de Date de JS indexa los meses
  // desde 0 (enero) hasta 11 (diciembre), a diferencia del formato humano DD/MM/AAAA.
  const fecha = new Date(anio, mes - 1, dia);

  const esFechaReal =
    fecha.getFullYear() === anio && fecha.getMonth() === mes - 1 && fecha.getDate() === dia;

  return esFechaReal ? fecha : null;
}

/** Formatea un Date a "DD/MM/AAAA" para mostrarlo en el input de texto. */
export function formatearFechaDDMMAAAA(fecha: Date): string {
  const dia = String(fecha.getDate()).padStart(2, '0');
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  return `${dia}/${mes}/${fecha.getFullYear()}`;
}

/**
 * Calcula la edad exacta en años de una fecha de nacimiento respecto de hoy, restando 1
 * si el cumpleaños de este año todavía no llegó (mismo criterio que
 * UsuarioService.ValidarFechaNacimiento en el backend, para que el mensaje de error se
 * dispare en el mismo caso de un lado y del otro).
 */
export function calcularEdad(fechaNacimiento: Date): number {
  const hoy = new Date();
  let edad = hoy.getFullYear() - fechaNacimiento.getFullYear();

  const todaviaNoCumplioEsteAnio =
    hoy.getMonth() < fechaNacimiento.getMonth() ||
    (hoy.getMonth() === fechaNacimiento.getMonth() && hoy.getDate() < fechaNacimiento.getDate());

  if (todaviaNoCumplioEsteAnio) {
    edad--;
  }

  return edad;
}
