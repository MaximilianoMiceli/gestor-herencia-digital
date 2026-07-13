import React, { useEffect } from 'react';
import { Platform } from 'react-native';
import { Tabs } from 'expo-router';
import { Home, Archive, Users, Menu } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Animated, { useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';

interface TabIconProps {
  IconComponent: any;
  color: any;
  focused: boolean;
}

function TabIcon({ IconComponent, color, focused }: TabIconProps) {
  const scale = useSharedValue(1);

  useEffect(() => {
    scale.value = withSpring(focused ? 1.2 : 1.0, { damping: 12, stiffness: 200 });
  }, [focused, scale]);

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

export default function AppTabs() {
  const insets = useSafeAreaInsets();

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        animation: 'fade',
        tabBarStyle: {
          height: Platform.OS === 'ios' ? 56 + insets.bottom : 64 + insets.bottom,
          backgroundColor: '#FFFFFF',
          borderTopWidth: 1,
          borderTopColor: '#C1E3A4',
          paddingBottom: insets.bottom > 0 ? insets.bottom : 10,
          paddingTop: 8,
          elevation: 8,
          shadowColor: '#1a2e2e',
          shadowOffset: { width: 0, height: -2 },
          shadowOpacity: 0.05,
          shadowRadius: 4,
        },
        tabBarActiveTintColor: '#1a2e2e',
        tabBarInactiveTintColor: '#8A9E95',
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
          title: 'Más',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon IconComponent={Menu} color={color} focused={focused} />
          ),
        }}
      />
    </Tabs>
  );
}
