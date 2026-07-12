/**
 * @file animated-icon.tsx
 * @description Variante nativa (iOS/Android) del logotipo animado de bienvenida y de la
 * cortina que se superpone a la splash screen nativa mientras la app termina de montarse.
 * Ver también animated-icon.web.tsx, la variante equivalente para Web (mismo propósito,
 * implementación adaptada porque Web no tiene splash screen nativa ni soporta los mismos
 * estilos de gradiente).
 */

import { Image } from 'expo-image';
import * as SplashScreen from 'expo-splash-screen';
import { useState } from 'react';
import { Dimensions, StyleSheet, View } from 'react-native';
import Animated, { Easing, Keyframe } from 'react-native-reanimated';
import { scheduleOnRN } from 'react-native-worklets';

// Escala inicial del ícono de splash: relativa a la altura de pantalla para que la
// cortina cubra el logo nativo (90pt) sin importar el tamaño del dispositivo.
const INITIAL_SCALE_FACTOR = Dimensions.get('screen').height / 90;
// Duración (ms) compartida por las animaciones de entrada/salida del logo y la cortina.
const DURATION = 600;

/**
 * Componente que muestra una cortina sobre la pantalla nativa de carga (Splash)
 * y ejecuta una animación de salida (fade-out) suave una vez que la app está lista.
 */
export function AnimatedSplashOverlay() {
  const [animate, setAnimate] = useState(false);
  const [visible, setVisible] = useState(true);

  // Remueve completamente el overlay del árbol de componentes al finalizar la animación.
  if (!visible) return null;

  // Cortina que imita el logo de la splash nativa (opaca) y luego se desvanece,
  // para que la transición entre la splash de Expo y la UI de la app no se note.
  const splashKeyframe = new Keyframe({
    0: {
      transform: [{ scale: 1 }],
      opacity: 1,
    },
    20: {
      opacity: 1,
    },
    70: {
      opacity: 0,
      easing: Easing.elastic(0.7),
    },
    100: {
      opacity: 0,
      transform: [{ scale: 1 }],
      easing: Easing.elastic(0.7),
    },
  });

  const image = <Image style={styles.image} source={require('@/assets/images/expo-logo.png')} />;

  // Antes de animar, se muestra una View estática (sin Animated) para evitar un parpadeo:
  // recién al terminar el layout ocultamos la splash nativa y activamos la animación.
  return animate ? (
    <Animated.View
      // El callback de entrada corre en el hilo de UI (worklet); scheduleOnRN reprograma
      // setVisible(false) en el hilo de JS, que es el único que puede tocar estado de React.
      entering={splashKeyframe.duration(DURATION).withCallback((finished) => {
        'worklet';
        if (finished) {
          scheduleOnRN(setVisible, false);
        }
      })}
      style={styles.splashOverlay}>
      {image}
    </Animated.View>
  ) : (
    <View
      // onLayout garantiza que la cortina ya está pintada en pantalla antes de destapar
      // la splash nativa, evitando un frame en blanco entre ambas.
      onLayout={() => {
        SplashScreen.hideAsync().finally(() => {
          setAnimate(true);
        });
      }}
      style={styles.splashOverlay}>
      {image}
    </View>
  );
}

// Fondo con gradiente: arranca "gigante" (cubriendo toda la pantalla, como la splash
// nativa) y se encoge elásticamente hasta su tamaño final de ícono (128x128).
const keyframe = new Keyframe({
  0: {
    transform: [{ scale: INITIAL_SCALE_FACTOR }],
  },
  100: {
    transform: [{ scale: 1 }],
    easing: Easing.elastic(0.7),
  },
});

// Logo: aparece con un pequeño delay respecto al fondo (mantiene opacidad 0 hasta el
// 40% de la animación) para que no se vea "flotando" mientras el fondo todavía se encoge.
const logoKeyframe = new Keyframe({
  0: {
    transform: [{ scale: 1.3 }],
    opacity: 0,
  },
  40: {
    transform: [{ scale: 1.3 }],
    opacity: 0,
    easing: Easing.elastic(0.7),
  },
  100: {
    opacity: 1,
    transform: [{ scale: 1 }],
    easing: Easing.elastic(0.7),
  },
});

// Brillo de fondo: una rotación completa (0° a 7200° = 20 vueltas) estirada a lo largo
// de varios minutos (ver duration en AnimatedIcon) para que se perciba como un giro
// lento y sutil, no como una animación que "termina" en pantalla.
const glowKeyframe = new Keyframe({
  0: {
    transform: [{ rotateZ: '0deg' }],
  },
  100: {
    transform: [{ rotateZ: '7200deg' }],
  },
});

/**
 * Logotipo animado para la pantalla de bienvenida.
 * Incluye un efecto de brillo giratorio en segundo plano y una animación de entrada elástica.
 */
export function AnimatedIcon() {
  return (
    <View style={styles.iconContainer}>
      <Animated.View entering={glowKeyframe.duration(60 * 1000 * 4)} style={styles.glow}>
        <Image style={styles.glow} source={require('@/assets/images/logo-glow.png')} />
      </Animated.View>

      <Animated.View entering={keyframe.duration(DURATION)} style={styles.background} />
      <Animated.View style={styles.imageContainer} entering={logoKeyframe.duration(DURATION)}>
        <Image style={styles.image} source={require('@/assets/images/expo-logo.png')} />
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
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
    zIndex: 100,
  },
  image: {
    width: 84,
    height: 84,
    borderRadius: 18,
  },
  background: {
    borderRadius: 40,
    experimental_backgroundImage: `linear-gradient(180deg, #3C9FFE, #0274DF)`,
    width: 128,
    height: 128,
    position: 'absolute',
  },
  splashOverlay: {
    ...StyleSheet.absoluteFill,
    backgroundColor: '#150F26',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
});
