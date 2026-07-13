import { useEffect } from 'react';
import { DarkTheme, DefaultTheme, ThemeProvider, Slot, useRouter, useSegments } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme } from 'react-native';
import { useFonts, MPLUS2_400Regular, MPLUS2_700Bold } from '@expo-google-fonts/m-plus-2';

import { AnimatedSplashOverlay } from '../components/animated-icon';
import { AuthProvider, useAuth } from '../context/AuthContext';

// El cierre del splash nativo se delega a AnimatedSplashOverlay para una transición fluida.
SplashScreen.preventAutoHideAsync();

function InitialLayout() {
  const { token, isLoading } = useAuth();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    // Mientras se lee el token de SecureStore no se sabe todavía si hay sesión: esperar
    // evita un redirect a welcome en falso durante ese instante.
    if (isLoading) return;

    // "invitacion" es accesible sin sesión: alguien puede recibir un link de invitación
    // antes de tener cuenta.
    const inAuthGroup = segments[0] === '(auth)';
    const isPublicScreen = inAuthGroup || (segments[0] as string) === 'invitacion';

    if (!token && !isPublicScreen) {
      router.replace('/(auth)/welcome');
    } else if (token && inAuthGroup) {
      // Ya autenticado pero viendo login/register (ej. volvió atrás): lo saca de ahí.
      router.replace('/');
    }
  }, [token, isLoading, segments]);

  return <Slot />;
}

export default function RootLayout() {
  const colorScheme = useColorScheme();

  const [fontsLoaded] = useFonts({
    'MPLUS2-Regular': MPLUS2_400Regular,
    'MPLUS2-Bold': MPLUS2_700Bold,
  });

  useEffect(() => {
    if (fontsLoaded) {
      SplashScreen.hideAsync().catch(console.warn);
    }
  }, [fontsLoaded]);

  if (!fontsLoaded) {
    return null;
  }

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <AuthProvider>
        <AnimatedSplashOverlay />
        <InitialLayout />
      </AuthProvider>
    </ThemeProvider>
  );
}
