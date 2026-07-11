import React from 'react';
import { Image } from 'expo-image';
import { StyleSheet } from 'react-native';

interface LockLogoProps {
  size?: number;
}

export default function LockLogo({ size = 80 }: LockLogoProps) {
  return (
    <Image
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
