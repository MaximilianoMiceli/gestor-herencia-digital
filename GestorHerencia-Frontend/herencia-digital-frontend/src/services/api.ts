/**
 * @file api.ts
 * @description Cliente HTTP centralizado (Axios) para toda la comunicación con el backend.
 *
 * Este archivo reemplaza el patrón anterior (cada pantalla/servicio llamando a `fetch`
 * directamente, repitiendo a mano el header "Authorization: Bearer <token>" y su propio
 * parseo de errores). Con Axios centralizado:
 *
 *   1. El TOKEN JWT se inyecta automáticamente en cada request saliente (interceptor de
 *      REQUEST), sin que ninguna pantalla tenga que leer SecureStore por su cuenta.
 *   2. Un 401 (token vencido/inválido) se detecta en UN SOLO LUGAR (interceptor de
 *      RESPONSE) y dispara el cierre de sesión, en vez de que cada pantalla repita su
 *      propio `if (err.message.includes('401'))`.
 *   3. El mensaje de error que arma el backend (`{ mensaje: "..." }`) se normaliza a un
 *      `Error` de JavaScript estándar, para que el código que llama pueda seguir
 *      escribiendo simplemente `catch (err) { Alert.alert('Error', err.message) }`.
 */

// AxiosError e InternalAxiosRequestConfig son TIPOS (no valores en tiempo de ejecución):
// se usan únicamente para que TypeScript sepa qué forma tiene el objeto de error/config
// dentro de los interceptores, sin agregar ningún costo al bundle final.
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import * as SecureStore from 'expo-secure-store';
import { API_BASE_URL } from '../constants/api';

/**
 * Clave bajo la cual se guarda el JWT en el llavero seguro nativo (Keychain en iOS,
 * Keystore en Android). Se define UNA SOLA VEZ acá (antes vivía duplicada, hardcodeada
 * también dentro de AuthContext.tsx) para que ambos archivos lean/escriban siempre la
 * misma clave y nunca puedan desincronizarse.
 */
export const TOKEN_KEY = 'herencia_jwt_token';

/**
 * Tipo de la función que se ejecuta cuando el interceptor de respuesta detecta un 401.
 * No recibe argumentos: su única responsabilidad es "avisale a quien corresponda que
 * la sesión ya no es válida", nunca decidir A DÓNDE navegar (eso lo resuelve el guard
 * de rutas en `_layout.tsx`, que ya observa reactivamente el `token` del AuthContext).
 */
type ManejadorNoAutorizado = () => void;

// Variable de módulo (no es un estado de React): guarda la referencia a la función que
// AuthContext registra mediante `setManejadorNoAutorizado` al montarse. Es "null" hasta
// que el AuthProvider termina de montar, y vuelve a "null" si alguna vez se desmontara
// (ver el `return` de limpieza en el useEffect de AuthContext).
let manejadorNoAutorizado: ManejadorNoAutorizado | null = null;

/**
 * Permite que una parte de la app FUERA de este archivo (en la práctica, siempre
 * AuthContext) registre qué hacer cuando el servidor responde 401 en cualquier request.
 *
 * ¿Por qué no llamar directamente a `signOut()` desde acá? Porque este archivo (api.ts)
 * es una capa de infraestructura pura, sin ninguna dependencia de React (no es un
 * componente, no puede usar hooks). AuthContext, en cambio, SÍ es quien posee el estado
 * de sesión (`token`) y el método `signOut()` que lo limpia. Este "registro de
 * callback" es el punto de conexión mínimo entre ambos mundos, sin que api.ts necesite
 * importar React ni AuthContext necesite importar Axios.
 */
export function setManejadorNoAutorizado(manejador: ManejadorNoAutorizado | null) {
  manejadorNoAutorizado = manejador;
}

/**
 * Instancia de Axios configurada con la URL base del backend (ver constants/api.ts).
 * Deliberadamente NO se fija acá un header "Content-Type" por defecto: Axios ya decide
 * el correcto automáticamente según el tipo de `body` que se le pase en cada request:
 *   - Si se pasa un objeto JS plano (ej: `{ email, password }`), Axios lo serializa a
 *     JSON y agrega "Content-Type: application/json" solo.
 *   - Si se pasa una instancia de `FormData` (ver assets.service.ts / certificados.service.ts,
 *     usada para subir archivos con expo-document-picker), Axios NO debe forzar un
 *     "Content-Type" manual: el `boundary` (el separador único de cada "parte" del
 *     multipart) lo calcula la propia plataforma (XMLHttpRequest/fetch) en el momento
 *     de armar el body real. Fijar el Content-Type a mano en ese caso ROMPE el request
 *     (el servidor no puede separar las partes del formulario sin el boundary correcto).
 */
export const api = axios.create({
  baseURL: API_BASE_URL,
});

// --- INTERCEPTOR DE REQUEST: inyección automática del JWT ---
//
// Un interceptor de request es una función que Axios ejecuta SIEMPRE, para TODA
// request saliente, ANTES de que viaje por la red. Acá se aprovecha ese punto único
// para leer el token guardado en SecureStore y agregarlo al header "Authorization" de
// la request en curso, sin que el código de cada pantalla tenga que acordarse de
// hacerlo (ni de repetir la misma línea en cada servicio, como pasaba antes).
api.interceptors.request.use(
  // La función es "async" porque `SecureStore.getItemAsync` es una operación de E/S
  // (lee del almacén seguro nativo) y por lo tanto devuelve una Promise. Axios ESPERA
  // (await) a que esta Promise se resuelva antes de continuar enviando la request: es
  // decir, ninguna request sale "a medio camino" sin haber podido intentar adjuntar el
  // token primero.
  async (config: InternalAxiosRequestConfig) => {
    const token = await SecureStore.getItemAsync(TOKEN_KEY);

    // Si hay un token guardado (el usuario está logueado), se agrega el header
    // estándar "Authorization: Bearer <token>". Los endpoints públicos (login,
    // registro, invitaciones, etc.) simplemente ignoran este header si llega, así que
    // no hace falta ninguna lista de "excepciones": es seguro mandarlo siempre que
    // exista.
    if (token) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }

    return config;
  },
  // Si algo falla ANTES de que la request llegue a salir (raro, pero Axios exige un
  // manejador de error simétrico), simplemente se propaga el error tal cual.
  (error: AxiosError) => Promise.reject(error)
);

// --- INTERCEPTOR DE RESPONSE: manejo centralizado de 401 + normalización de errores ---
//
// Un interceptor de response tiene DOS funciones: la primera se ejecuta para toda
// respuesta EXITOSA (2xx), la segunda para toda respuesta que Axios considera un error
// (4xx/5xx, o un fallo de red). Acá no se necesita tocar las respuestas exitosas (se
// devuelven tal cual), pero sí centralizar qué pasa con los errores.
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<{ mensaje?: string; message?: string }>) => {
    // --- Caso 1: 401 Unauthorized (token vencido, inválido, o ausente) ---
    // El backend NO implementa renovación de sesión (no existe ningún endpoint de
    // "refresh token": el JWT emitido por TokenService.CrearToken simplemente expira a
    // los 30 minutos y no hay forma de renovarlo sin loguearse de nuevo). Por eso, ante
    // un 401, la única acción correcta es cerrar la sesión LOCAL (borrar el token
    // guardado, ya inválido igual) y dejar que el usuario vuelva a loguearse: no hay
    // ningún reintento silencioso posible.
    if (error.response?.status === 401) {
      // Se borra acá mismo (no se espera a que AuthContext lo haga) para que, incluso
      // si por algún motivo nadie registró un manejador todavía, el token vencido no
      // quede persistido y se siga reenviando en requests futuras.
      await SecureStore.deleteItemAsync(TOKEN_KEY);

      // Avisa a AuthContext (si ya se registró, ver setManejadorNoAutorizado) para que
      // limpie su estado en memoria (`token` -> null). Ese cambio de estado es lo que
      // dispara, reactivamente, el guard de rutas en `_layout.tsx` (que ya observa
      // `token` y redirige a "/(auth)/welcome" apenas queda en null): este archivo NO
      // navega directamente, solo notifica.
      manejadorNoAutorizado?.();
    }

    // --- Normalización del mensaje de error ---
    // El backend, en TODOS sus controllers, responde los errores con la forma
    // `{ mensaje: "texto amigable en español" }` (ver, por ejemplo,
    // AuthController: `return Unauthorized(new { mensaje = "Credenciales invalidas." })`).
    // Acá se extrae ese texto UNA SOLA VEZ y se arma un `Error` de JS estándar con ese
    // mensaje, para que el código que llama (cualquier pantalla) pueda seguir
    // escribiendo simplemente `catch (err: any) { Alert.alert('Error', err.message) }`,
    // sin tener que saber nada sobre la forma de la respuesta HTTP subyacente.
    const mensaje =
      error.response?.data?.mensaje ??
      error.response?.data?.message ??
      error.message ??
      'Ocurrió un error de red inesperado.';

    return Promise.reject(new Error(mensaje));
  }
);
