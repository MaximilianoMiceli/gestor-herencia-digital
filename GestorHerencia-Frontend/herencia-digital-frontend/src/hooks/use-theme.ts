import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

/**
 * Hook personalizado para obtener la paleta de colores del tema actual.
 * Resuelve el esquema activo ('light' o 'dark') y devuelve el subconjunto de tokens de color correspondiente.
 */
export function useTheme() {
  const scheme = useColorScheme();
  // Si el esquema del sistema no está definido, se usa el modo claro (light) por defecto.
  const theme = scheme === 'unspecified' ? 'light' : scheme;

  return Colors[theme];
}
