/**
 * @file _layout.tsx
 * @description Orquestador del enrutamiento raíz de la aplicación mediante Expo Router.
 * 
 * Implementa la estructura del proveedor global de fuentes de Google (M-PLUS-2),
 * el proveedor del contexto de sesión (AuthProvider), el overlay de carga animado de entrada,
 * y la lógica de protección de rutas privadas mediante redirecciones condicionales de estado.
 */

import { useEffect } from 'react';
import { DarkTheme, DefaultTheme, ThemeProvider, Slot, useRouter, useSegments } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme } from 'react-native';
import { useFonts, MPLUS2_400Regular, MPLUS2_700Bold } from '@expo-google-fonts/m-plus-2';

import { AnimatedSplashOverlay } from '../components/animated-icon';
import { AuthProvider, useAuth } from '../context/AuthContext';

// Evitamos que la pantalla de inicio nativa del SO se oculte sola, delegando el cierre
// al componente personalizado AnimatedSplashOverlay para lograr una transición fluida.
SplashScreen.preventAutoHideAsync();

/**
 * Sub-layout encargado de vigilar el estado de sesión activa de manera reactiva.
 * 
 * Evalúa en qué segmento de ruta se encuentra el puntero y redirige al grupo (auth) si
 * el usuario no posee token, o lo devuelve al grupo privado (tabs) si el usuario ya está autenticado.
 */
function InitialLayout() {
  const { token, isLoading } = useAuth();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    // Si la lectura inicial del SecureStore sigue en progreso, evitamos realizar decisiones de enrutado.
    if (isLoading) return;

    // Detecta si la pantalla solicitada forma parte del grupo de autenticación pública
    // o es la pantalla de invitación accesible para usuarios anónimos.
    const inAuthGroup = segments[0] === '(auth)';
    const isPublicScreen = inAuthGroup || (segments[0] as string) === 'invitacion';

    if (!token && !isPublicScreen) {
      // Caso 1: Usuario no logueado intenta acceder a pantallas protegidas -> Redirigir a welcome
      router.replace('/(auth)/welcome');
    } else if (token && inAuthGroup) {
      // Caso 2: Usuario autenticado intenta ver pantallas de login/registro -> Redirigir a la app principal
      router.replace('/');
    }
  }, [token, isLoading, segments]);

  // Retorna un <Slot /> que actúa como marcador de posición para renderizar la ruta hija coincidente.
  return <Slot />;
}

/**
 * Raíz del layout principal de la aplicación.
 * 
 * Inicializa el cargador de fuentes, aplica los colores del tema del sistema (oscuro/claro),
 * e inyecta el AuthProvider global.
 */
export default function RootLayout() {
  const colorScheme = useColorScheme();
  
  // Cargamos las tipografías personalizadas globales.
  const [fontsLoaded] = useFonts({
    'MPLUS2-Regular': MPLUS2_400Regular,
    'MPLUS2-Bold': MPLUS2_700Bold,
  });

  useEffect(() => {
    if (fontsLoaded) {
      // Una vez cargadas las tipografías, ocultamos la splash nativa.
      SplashScreen.hideAsync().catch(console.warn);
    }
  }, [fontsLoaded]);

  if (!fontsLoaded) {
    return null;
  }

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <AuthProvider>
        {/* Overlay con logo animado elástico de la splash-screen */}
        <AnimatedSplashOverlay />
        {/* Validador de rutas seguras basado en token */}
        <InitialLayout />
      </AuthProvider>
    </ThemeProvider>
  );
}
