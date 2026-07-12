/**
 * @file animated-icon.web.tsx
 * @description Variante Web del logotipo animado de bienvenida. Metro resuelve este
 * archivo automáticamente en vez de animated-icon.tsx cuando la plataforma es 'web'
 * (convención de sufijo .web.tsx de React Native/Expo), ya que Web no tiene splash
 * screen nativa y necesita un gradiente de fondo vía CSS en lugar de un estilo de
 * React Native.
 */

import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';
import Animated, { Keyframe, Easing } from 'react-native-reanimated';

import classes from './animated-icon.module.css';
// Animación más corta que en la versión nativa (300ms vs 600ms): en Web no hay que
// esperar a que se oculte una splash nativa, así que se prioriza una entrada más ágil.
const DURATION = 300;

/**
 * Componente placeholder para la versión Web.
 * En web no es necesaria la cortina de splash nativa, por lo que retorna null
 * (el archivo nativo sí implementa un overlay real; ver animated-icon.tsx).
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

// Brillo: a diferencia de la versión nativa (que solo gira), acá también hace un fade-in
// y un pequeño "destape" rotado (-180deg a 0deg) durante la entrada. Los keys de Keyframe
// son porcentajes (0-100) del total de duration(), no milisegundos: como este brillo se
// usa con una duration mucho más larga (varios minutos, en AnimatedIcon), el key
// "DURATION / 1000" cae muy cerca del 0%, haciendo que el destape ocurra casi al instante.
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
        {/* El gradiente se aplica con una clase CSS (animated-icon.module.css) en vez del
            estilo "experimental_backgroundImage" que usa la versión nativa, porque esa
            propiedad de React Native Web no siempre se traduce de forma confiable a CSS
            real en el DOM; un <div> con className es la forma directa de lograrlo en Web. */}
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
