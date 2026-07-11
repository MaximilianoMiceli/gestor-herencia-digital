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
import { VerificacionVidaService } from '../../services/verificacion-vida.service';
import { UsuariosService } from '../../services/usuarios.service';

export default function HomeScreen() {
  const router = useRouter();
  const { userName, userId, token, signOut } = useAuth();

  // Leemos el query parameter "success" inyectado al redireccionar tras crear un activo
  const { success } = useLocalSearchParams<{ success?: string }>();
  const [showSuccess, setShowSuccess] = useState(false);

  // Estados para conteos en tiempo real y verificación de vida
  const [assetsCount, setAssetsCount] = useState(0);
  const [beneficiariosCount, setBeneficiariosCount] = useState(0);
  const [isLifeVerificationActive, setIsLifeVerificationActive] = useState(false);
  const [is2FAActive, setIs2FAActive] = useState(false);
  const [pendingHerenciasCount, setPendingHerenciasCount] = useState(0);

  // Función para obtener los conteos desde los servicios
  const fetchCounts = async () => {
    if (!token) return;
    try {
      const assets = await AssetsService.getAssets();
      setAssetsCount(assets.length);

      const beneficiaries = await AssetsService.getMisBeneficiarios();
      setBeneficiariosCount(beneficiaries.length);

      // Herencias recibidas que todavía no acepté ni rechacé: se destacan en un banner
      // llamativo en el Dashboard (ver dashboard.tsx) para que, por ejemplo, alguien que
      // se acaba de registrar con un email que ya tenía un beneficio esperándolo lo note
      // de inmediato, en vez de tener que descubrir por su cuenta el botón "Mis herencias".
      const herencias = await AssetsService.getMisHerencias();
      setPendingHerenciasCount(herencias.filter((h) => h.estado === 'Pendiente').length);

      // Cargar la configuración REAL de Verificación de Vida desde el backend (antes se
      // leía de SecureStore local, que nunca reflejaba el estado que el servidor
      // realmente usa para decidir cuándo liberar la herencia).
      const configuracion = await VerificacionVidaService.obtenerConfiguracion();
      setIsLifeVerificationActive(configuracion.activo);

      // Estado REAL del 2FA (antes esta fila del Dashboard era puramente decorativa,
      // siempre en "Inactivo", sin ningún backend detrás).
      if (userId) {
        const usuario = await UsuariosService.obtenerPorId(userId);
        setIs2FAActive(usuario.dobleFactorHabilitado);
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
    }, [token, userId])
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
      is2FAActive={is2FAActive}
      pendingHerenciasCount={pendingHerenciasCount}
      onAddAsset={handleAddAsset}
      onNavigateToTab={handleNavigateToTab}
      showSuccessNotification={showSuccess}
    />
  );
}
