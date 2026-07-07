import React, { useEffect } from 'react';
import { Platform } from 'react-native';
import { Tabs } from 'expo-router';
import { Home, Archive, Users, Lock } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Animated, { useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';

/**
 * Propiedades del componente auxiliar TabIcon.
 */
interface TabIconProps {
  /** Componente del ícono de Lucide a renderizar. */
  IconComponent: any;
  /** Color del ícono provisto por el Tab Navigator. */
  color: any;
  /** Indica si la pestaña correspondiente está activa. */
  focused: boolean;
}

/**
 * Componente que envuelve e implementa micro-animaciones en los íconos del Tab Bar inferior.
 * Emplea react-native-reanimated para escalar de forma elástica el ícono cuando se selecciona.
 */
function TabIcon({ IconComponent, color, focused }: TabIconProps) {
  const scale = useSharedValue(1);

  useEffect(() => {
    // Aplica una física de resorte (spring bounce) al cambiar el estado de foco.
    scale.value = withSpring(focused ? 1.2 : 1.0, { damping: 12, stiffness: 200 });
  }, [focused]);

  const animatedStyle = useAnimatedStyle(() => {
    return {
      transform: [
        { scale: scale.value },
      ],
    };
  });

  const iconColor = color as string;

  return (
    <Animated.View style={animatedStyle}>
      <IconComponent
        size={22}
        color={iconColor}
        fill={focused ? iconColor : 'none'}
        strokeWidth={focused ? 2.5 : 2}
      />
    </Animated.View>
  );
}

/**
 * Menu de navegación inferior principal (Bottom Tabs) para plataformas nativas.
 * Integra las pantallas de Inicio, Activos, Beneficiarios y Seguridad con transiciones fluidas.
 */
export default function AppTabs() {
  const insets = useSafeAreaInsets();

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        animation: 'fade', // Hace que la transición entre pantallas sea un desvanecido suave (slide no está disponible en Tabs)
        tabBarStyle: {
          // Ajusta la altura de forma dinámica sumando el área segura inferior (evita colisiones con barras de gestos)
          height: Platform.OS === 'ios' ? 56 + insets.bottom : 64 + insets.bottom,
          backgroundColor: '#FFFFFF',
          borderTopWidth: 1,
          borderTopColor: '#C1E3A4', // Borde superior sutil gris/verde
          // Agrega un padding inferior proporcional para empujar las etiquetas de las pestañas fuera del área física de gestos
          paddingBottom: insets.bottom > 0 ? insets.bottom : 10,
          paddingTop: 8,
          elevation: 8,
          shadowColor: '#1a2e2e',
          shadowOffset: { width: 0, height: -2 },
          shadowOpacity: 0.05,
          shadowRadius: 4,
        },
        tabBarActiveTintColor: '#1a2e2e', // Color activo (negro/oscuro)
        tabBarInactiveTintColor: '#8A9E95', // Color inactivo (gris-verde)
        tabBarLabelStyle: {
          fontFamily: 'MPLUS2-Regular',
          fontSize: 11,
          marginTop: 4,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: 'Inicio',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon IconComponent={Home} color={color} focused={focused} />
          ),
        }}
      />

      <Tabs.Screen
        name="activos"
        options={{
          title: 'Activos',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon IconComponent={Archive} color={color} focused={focused} />
          ),
        }}
      />

      <Tabs.Screen
        name="beneficiarios"
        options={{
          title: 'Beneficiarios',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon IconComponent={Users} color={color} focused={focused} />
          ),
        }}
      />

      <Tabs.Screen
        name="seguridad"
        options={{
          title: 'Seguridad',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon IconComponent={Lock} color={color} focused={focused} />
          ),
        }}
      />

      {/* Ocultar la pantalla explore por defecto */}
      <Tabs.Screen
        name="explore"
        options={{
          href: null,
        }}
      />
    </Tabs>
  );
}
