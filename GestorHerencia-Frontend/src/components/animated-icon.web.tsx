/**
 * @file animated-icon.web.tsx
 * @description Variante Web del logotipo animado de bienvenida. Metro resuelve este archivo
 * en vez de animated-icon.tsx cuando la plataforma es 'web' (convención de sufijo .web.tsx),
 * ya que Web no tiene splash screen nativa y necesita el gradiente vía CSS en vez de estilo RN.
 */

import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';
import Animated, { Keyframe, Easing } from 'react-native-reanimated';

import classes from './animated-icon.module.css';
// Animación más corta que en la versión nativa (300ms vs 600ms): en Web no hay que
// esperar a que se oculte una splash nativa, así que se prioriza una entrada más ágil.
const DURATION = 300;

/**
 * Placeholder Web: no hay splash nativa que cubrir, por lo que no renderiza nada
 * (el overlay real vive en animated-icon.tsx).
 */
export function AnimatedSplashOverlay() {
  return null;
}

// Fondo: en Web arranca desde escala 0 (no hay splash nativa que cubrir, así que sale
// "de la nada") y rebota levemente por encima de su tamaño final (1.2) antes de asentarse.
const keyframe = new Keyframe({
  0: {
    transform: [{ scale: 0 }],
  },
  60: {
    transform: [{ scale: 1.2 }],
    easing: Easing.elastic(1.2),
  },
  100: {
    transform: [{ scale: 1 }],
    easing: Easing.elastic(1.2),
  },
});

// Logo: permanece invisible y encogido hasta el 60% de la animación para que aparezca
// recién cuando el fondo ya hizo la mayor parte de su rebote.
const logoKeyframe = new Keyframe({
  0: {
    opacity: 0,
  },
  60: {
    transform: [{ scale: 1.2 }],
    opacity: 0,
    easing: Easing.elastic(1.2),
  },
  100: {
    transform: [{ scale: 1 }],
    opacity: 1,
    easing: Easing.elastic(1.2),
  },
});

// Brillo: además de girar (como en nativo) hace fade-in y un "destape" rotado (-180deg a 0deg).
// Los keys de Keyframe son porcentajes (0-100) de duration(), no ms: como acá duration() es
// de varios minutos, "DURATION / 1000" cae casi en el 0%, así el destape ocurre casi al instante.
const glowKeyframe = new Keyframe({
  0: {
    transform: [{ rotateZ: '-180deg' }, { scale: 0.8 }],
    opacity: 0,
  },
  [DURATION / 1000]: {
    transform: [{ rotateZ: '0deg' }, { scale: 1 }],
    opacity: 1,
    easing: Easing.elastic(0.7),
  },
  100: {
    transform: [{ rotateZ: '7200deg' }],
  },
});

/**
 * Logotipo animado para la pantalla de bienvenida (versión Web).
 * Adapta los efectos visuales y animaciones usando clases CSS y Reanimated.
 */
export function AnimatedIcon() {
  return (
    <View style={styles.iconContainer}>
      <Animated.View entering={glowKeyframe.duration(60 * 1000 * 4)} style={styles.glow}>
        <Image style={styles.glow} source={require('@/assets/images/logo-glow.png')} />
      </Animated.View>

      <Animated.View style={styles.background} entering={keyframe.duration(DURATION)}>
        {/* Gradiente vía clase CSS en vez de "experimental_backgroundImage" (usado en nativo):
            esa propiedad de RN Web no siempre se traduce de forma confiable a CSS real en el DOM. */}
        <div className={classes.expoLogoBackground} />
      </Animated.View>

      <Animated.View style={styles.imageContainer} entering={logoKeyframe.duration(DURATION)}>
        <Image style={styles.image} source={require('@/assets/images/expo-logo.png')} />
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    width: '100%',
    zIndex: 1000,
    position: 'absolute',
    top: 128 / 2 + 138,
  },
  imageContainer: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  glow: {
    width: 201,
    height: 201,
    position: 'absolute',
  },
  iconContainer: {
    justifyContent: 'center',
    alignItems: 'center',
    width: 128,
    height: 128,
  },
  image: {
    position: 'absolute',
    width: 76,
    height: 71,
  },
  background: {
    width: 128,
    height: 128,
    position: 'absolute',
  },
});
