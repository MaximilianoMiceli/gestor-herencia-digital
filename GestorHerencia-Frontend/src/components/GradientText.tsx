import React from 'react';
import { Text, TextStyle } from 'react-native';
import MaskedView from '@react-native-masked-view/masked-view';
import { LinearGradient } from 'expo-linear-gradient';

interface GradientTextProps {
  text: string;
  style?: TextStyle;
  colors?: readonly [string, string, ...string[]];
}

// React Native no soporta "background-clip: text": el efecto se logra enmascarando un
// LinearGradient con la forma del propio texto.
export default function GradientText({
  text,
  style,
  colors = ['#DF5173', '#874BE5'],
}: GradientTextProps) {
  return (
    <MaskedView
      // El texto define qué áreas del degradé quedan visibles; su color no importa.
      maskElement={
        <Text style={[style, { backgroundColor: 'transparent' }]}>{text}</Text>
      }
    >
      <LinearGradient
        colors={colors}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0 }}
      >
        {/* Texto invisible: le da al LinearGradient el mismo ancho/alto que el texto real. */}
        <Text style={[style, { opacity: 0 }]}>{text}</Text>
      </LinearGradient>
    </MaskedView>
  );
}
