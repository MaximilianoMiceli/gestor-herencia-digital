import React from 'react';
import Svg, { Path, Defs, LinearGradient, Stop, Rect } from 'react-native-svg';

interface LockLogoProps {
  size?: number;
}

export default function LockLogo({ size = 80 }: LockLogoProps) {
  return (
    <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <Defs>
        <LinearGradient id="lockGradient" x1="0" y1="0" x2="0" y2="1">
          <Stop offset="0%" stopColor="#DF5173" />
          <Stop offset="100%" stopColor="#874BE5" />
        </LinearGradient>
      </Defs>
      
      {/* Cuerpo del candado */}
      <Rect 
        x="3" 
        y="11" 
        width="18" 
        height="11" 
        rx="2" 
        ry="2" 
        fill="url(#lockGradient)" 
      />
      
      {/* Ojo de la cerradura */}
      <Path 
        d="M12 14c-.83 0-1.5.67-1.5 1.5 0 .58.33 1.08.82 1.34l-.32 1.66h2l-.32-1.66c.49-.26.82-.76.82-1.34 0-.83-.67-1.5-1.5-1.5z" 
        fill="#C1E3A4" 
      />
      
      {/* Arco del candado */}
      <Path 
        d="M7 11V7c0-2.76 2.24-5 5-5s5 2.24 5 5v4" 
        stroke="url(#lockGradient)" 
        strokeWidth="2.5" 
        strokeLinecap="round"
        strokeLinejoin="round" 
      />
    </Svg>
  );
}
