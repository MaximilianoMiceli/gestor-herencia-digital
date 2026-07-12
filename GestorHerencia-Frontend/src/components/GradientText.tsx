import React from 'react';
import { Text, TextStyle } from 'react-native';
import MaskedView from '@react-native-masked-view/masked-view';
import { LinearGradient } from 'expo-linear-gradient';

/**
 * Propiedades del componente GradientText.
 */
interface GradientTextProps {
  /** Texto a renderizar con relleno de degradé. */
  text: string;
  /** Estilo tipográfico (tamaño, familia de fuente, peso, etc.) del texto. */
  style?: TextStyle;
  /** Colores del degradé, en orden, de izquierda a derecha. Requiere al menos 2 colores. */
  colors?: readonly [string, string, ...string[]];
}

/**
 * Texto con relleno de degradé de color, usado para títulos destacados (ej: branding,
 * pantalla de bienvenida). React Native no soporta un "background-clip: text" nativo,
 * así que se logra el efecto enmascarando un LinearGradient con la forma del propio texto.
 */
export default function GradientText({
  text,
  style,
  colors = ['#DF5173', '#874BE5'],
}: GradientTextProps) {
  return (
    <MaskedView
      // El texto de la máscara define QUÉ áreas del degradé de abajo quedan visibles
      // (la silueta de las letras); su color real no importa, por eso el fondo es transparente.
      maskElement={
        <Text style={[style, { backgroundColor: 'transparent' }]}>{text}</Text>
      }
    >
      <LinearGradient
        colors={colors}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0 }}
      >
        {/* Texto invisible (opacity: 0) que solo sirve para que el LinearGradient
            adopte el mismo ancho/alto que el texto real, ya que el gradiente por sí
            solo no tiene tamaño intrínseco definido por el contenido. */}
        <Text style={[style, { opacity: 0 }]}>{text}</Text>
      </LinearGradient>
    </MaskedView>
  );
}
