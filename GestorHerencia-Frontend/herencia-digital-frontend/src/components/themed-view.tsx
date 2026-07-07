import { View, type ViewProps } from 'react-native';

import { ThemeColor } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';

/**
 * Propiedades para el componente ThemedView, extendiendo las de View estándar.
 */
export type ThemedViewProps = ViewProps & {
  lightColor?: string;
  darkColor?: string;
  /** Tipo de elemento del tema activo a utilizar para determinar el color de fondo. */
  type?: ThemeColor;
};

/**
 * Componente contenedor (View) tematizado.
 * Adapta automáticamente su color de fondo al tema actual (claro/oscuro) de la aplicación.
 */
export function ThemedView({ style, lightColor, darkColor, type, ...otherProps }: ThemedViewProps) {
  const theme = useTheme();

  return <View style={[{ backgroundColor: theme[type ?? 'background'] }, style]} {...otherProps} />;
}
