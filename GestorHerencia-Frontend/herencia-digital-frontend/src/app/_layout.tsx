import { useEffect } from 'react';
import { DarkTheme, DefaultTheme, ThemeProvider, Slot, useRouter, useSegments } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme } from 'react-native';
import { useFonts, MPLUS2_400Regular, MPLUS2_700Bold } from '@expo-google-fonts/m-plus-2';

import { AnimatedSplashOverlay } from '../components/animated-icon';
import { AuthProvider, useAuth } from '../context/AuthContext';

SplashScreen.preventAutoHideAsync();

function InitialLayout() {
  const { token, isLoading } = useAuth();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;

    const inAuthGroup = segments[0] === '(auth)';

    if (!token && !inAuthGroup) {
      // Si no hay token, redirigir al login
      router.replace('/(auth)/welcome');
    } else if (token && inAuthGroup) {
      // Si hay token y estamos en auth, redirigir a tabs
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
