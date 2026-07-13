// El Date que devuelve este módulo se arma siempre en hora LOCAL (constructor numérico),
// nunca parseando un string ISO: los llamadores leen getFullYear/getMonth/getDate en vez
// de toISOString(), que convierte a UTC y en Argentina (UTC-3) puede correr la fecha un día.

/** true si el string tiene la forma DD/MM/AAAA con separadores "/". No valida todavía
 *  que sea una fecha calendario real (eso lo hace parsearFechaDDMMAAAA). */
export function tieneFormatoDDMMAAAA(texto: string): boolean {
  return /^\d{2}\/\d{2}\/\d{4}$/.test(texto.trim());
}

// El constructor de Date es "permisivo": new Date(2000, 1, 31) no lanza error para un 31
// de febrero, sino que hace rollover silencioso a marzo. Por eso se compara de vuelta.
export function parsearFechaDDMMAAAA(texto: string): Date | null {
  if (!tieneFormatoDDMMAAAA(texto)) return null;

  const [diaStr, mesStr, anioStr] = texto.trim().split('/');
  const dia = Number(diaStr);
  const mes = Number(mesStr);
  const anio = Number(anioStr);

  // Date indexa los meses desde 0 (enero), a diferencia del formato humano DD/MM/AAAA.
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

// Mismo criterio que ValidarFechaNacimiento en el backend, para que el error se
// dispare en el mismo caso de un lado y del otro.
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
