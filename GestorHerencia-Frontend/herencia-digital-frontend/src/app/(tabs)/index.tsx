/**
 * @file index.tsx
 * @description Pantalla controladora y punto de montaje de la pestaña Inicio (Dashboard).
 * 
 * Gestiona la captura del parámetro `success` proveniente de otras rutas para gatillar
 * las notificaciones flotantes de éxito, realizando auto-limpieza de la query URL para
 * mitigar bucles visuales en el repintado (re-rendering).
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useRouter, useLocalSearchParams, useFocusEffect } from 'expo-router';
import * as SecureStore from 'expo-secure-store';
import Dashboard from '../../components/dashboard';
import { useAuth } from '../../context/AuthContext';
import { AssetsService } from '../../services/assets.service';

export default function HomeScreen() {
  const router = useRouter();
  const { userName, token, userEmail, signOut } = useAuth();
  
  // Leemos el query parameter "success" inyectado al redireccionar tras crear un activo
  const { success } = useLocalSearchParams<{ success?: string }>();
  const [showSuccess, setShowSuccess] = useState(false);

  // Estados para conteos en tiempo real y verificación de vida
  const [assetsCount, setAssetsCount] = useState(0);
  const [beneficiariosCount, setBeneficiariosCount] = useState(0);
  const [isLifeVerificationActive, setIsLifeVerificationActive] = useState(false);

  // Función para obtener los conteos desde los servicios
  const fetchCounts = async () => {
    if (!token) return;
    try {
      const assets = await AssetsService.getAssets(token);
      setAssetsCount(assets.length);

      const beneficiaries = await AssetsService.getMisBeneficiarios(token);
      setBeneficiariosCount(beneficiaries.length);

      // Cargar la configuración de Verificación de Vida para el estado dinámico
      if (userEmail) {
        // Sanitizamos la clave reemplazando caracteres no permitidos en SecureStore (como '@')
        const sanitizedEmail = userEmail.replace(/[^a-zA-Z0-9.-]/g, '_');
        const key = `life-verification-config-${sanitizedEmail}`;
        const savedData = await SecureStore.getItemAsync(key);
        if (savedData) {
          const config = JSON.parse(savedData);
          setIsLifeVerificationActive(config.isActive ?? false);
        } else {
          setIsLifeVerificationActive(false);
        }
      }
    } catch (err: any) {
      console.log('Error fetching dashboard counts:', err);
      // Si el token expiró o es inválido (Status 401), deslogueamos al usuario de forma reactiva
      if (err.message && err.message.includes('401')) {
        signOut();
      }
    }
  };

  // Recargar los conteos del dashboard cada vez que la pantalla entra en foco
  useFocusEffect(
    useCallback(() => {
      fetchCounts();
    }, [token, userEmail])
  );

  useEffect(() => {
    // Si la redirección trae la bandera exitosa:
    if (success === 'true') {
      setShowSuccess(true);
      
      // Limpiamos los parámetros de la URL de forma asíncrona para que no persista el estado 'success'
      // al cambiar de pestañas en el tab bar inferior
      router.setParams({ success: undefined });
      
      // Auto-desvanecer la píldora flotante del Dashboard tras 3.5 segundos
      const timer = setTimeout(() => {
        setShowSuccess(false);
      }, 3500);
      
      return () => clearTimeout(timer);
    }
  }, [success]);

  /**
   * Redirige al usuario a la pestaña correspondiente seleccionada en el Dashboard.
   * @param tabName Nombre de la pestaña de destino (Activos, Beneficiarios, Seguridad)
   */
  const handleNavigateToTab = (tabName: string) => {
    if (tabName === 'Activos') {
      router.push('/activos');
    } else if (tabName === 'Beneficiarios') {
      router.push('/beneficiarios');
    } else if (tabName === 'Seguridad') {
      router.push('/seguridad');
    }
  };

  /**
   * Redirige al flujo modal de creación de un nuevo activo.
   */
  const handleAddAsset = () => {
    router.push('/nuevo-activo');
  };

  return (
    <Dashboard
      userName={userName || undefined}
      assetsCount={assetsCount}
      beneficiariosCount={beneficiariosCount}
      isLifeVerificationActive={isLifeVerificationActive}
      onAddAsset={handleAddAsset}
      onNavigateToTab={handleNavigateToTab}
      showSuccessNotification={showSuccess}
    />
  );
}
