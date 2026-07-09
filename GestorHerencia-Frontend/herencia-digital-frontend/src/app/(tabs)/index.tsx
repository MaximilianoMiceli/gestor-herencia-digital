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
import Dashboard from '../../components/dashboard';
import { useAuth } from '../../context/AuthContext';
import { AssetsService } from '../../services/assets.service';

export default function HomeScreen() {
  const router = useRouter();
  const { userName, token } = useAuth();
  
  // Leemos el query parameter "success" inyectado al redireccionar tras crear un activo
  const { success } = useLocalSearchParams<{ success?: string }>();
  const [showSuccess, setShowSuccess] = useState(false);

  // Estados para conteos en tiempo real
  const [assetsCount, setAssetsCount] = useState(0);
  const [beneficiariosCount, setBeneficiariosCount] = useState(0);

  // Función para obtener los conteos desde los servicios
  const fetchCounts = async () => {
    if (!token) return;
    try {
      const assets = await AssetsService.getAssets(token);
      setAssetsCount(assets.length);

      const beneficiaries = await AssetsService.getBeneficiarios(token);
      setBeneficiariosCount(beneficiaries.length);
    } catch (err) {
      console.log('Error fetching dashboard counts:', err);
    }
  };

  // Recargar los conteos del dashboard cada vez que la pantalla entra en foco
  useFocusEffect(
    useCallback(() => {
      fetchCounts();
    }, [token])
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
      onAddAsset={handleAddAsset}
      onNavigateToTab={handleNavigateToTab}
      showSuccessNotification={showSuccess}
    />
  );
}
