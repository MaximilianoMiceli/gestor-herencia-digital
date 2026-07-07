import { DarkTheme, DefaultTheme, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme } from 'react-native';
import { useFonts, MPLUS2_400Regular, MPLUS2_700Bold } from '@expo-google-fonts/m-plus-2';

import { AnimatedSplashOverlay } from '../components/animated-icon';
import AppTabs from '../components/app-tabs';

// Evita que la pantalla de carga nativa se oculte sola para permitir que la animación
// personalizada de AnimatedSplashOverlay maneje la transición de salida de forma suave.
SplashScreen.preventAutoHideAsync();

/**
 * Layout principal de la aplicación. Carga las fuentes del sistema, configura
 * el proveedor de temas (oscuro/claro) y orquesta la presentación del overlay
 * de la pantalla de carga y el menú de navegación principal.
 */
export default function TabLayout() {
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
      <AnimatedSplashOverlay />
      <AppTabs />
    </ThemeProvider>
  );
}
