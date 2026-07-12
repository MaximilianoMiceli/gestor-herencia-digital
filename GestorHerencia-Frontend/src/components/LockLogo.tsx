import React from 'react';
import { Image } from 'expo-image';
import { StyleSheet } from 'react-native';

/**
 * Propiedades del componente LockLogo.
 */
interface LockLogoProps {
  /** Ancho y alto del logo, en píxeles. */
  size?: number;
}

/**
 * Logo de la app (ícono del candado) usado en las pantallas de bienvenida y autenticación.
 * Reutiliza el mismo ícono de la app (icon.png) en vez de un asset aparte, para mantener
 * consistencia visual entre el ícono del dispositivo y el logo dentro de la app.
 */
export default function LockLogo({ size = 80 }: LockLogoProps) {
  return (
    <Image
      // El radio de borde escala junto con "size" (22% del tamaño) para que el logo
      // mantenga las mismas proporciones de esquina redondeada sin importar en qué tamaño se use.
      style={[styles.image, { width: size, height: size, borderRadius: size * 0.22 }]}
      source={require('@/assets/images/icon.png')}
      contentFit="cover"
    />
  );
}

const styles = StyleSheet.create({
  image: {
    overflow: 'hidden',
  },
});
