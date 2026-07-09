import { useEffect, useState } from 'react';
import { useColorScheme as useRNColorScheme } from 'react-native';

/**
 * Hook personalizado para obtener el esquema de color en entornos Web.
 * Emplea un estado de hidratación (hasHydrated) para mitigar discrepancias (mismatch)
 * de renderizado del lado del cliente vs servidor (SSR) retornando 'light' por defecto en el servidor.
 */
export function useColorScheme() {
  const [hasHydrated, setHasHydrated] = useState(false);

  useEffect(() => {
    // Se ejecuta al montar el cliente, confirmando que la fase de hidratación culminó.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setHasHydrated(true);
  }, []);

  const colorScheme = useRNColorScheme();

  if (hasHydrated) {
    return colorScheme;
  }

  // Fallback seguro durante SSR en el servidor.
  return 'light';
}
