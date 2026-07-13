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

  const { success } = useLocalSearchParams<{ success?: string }>();
  const [showSuccess, setShowSuccess] = useState(false);

  const [assetsCount, setAssetsCount] = useState(0);
  const [beneficiariosCount, setBeneficiariosCount] = useState(0);
  const [isLifeVerificationActive, setIsLifeVerificationActive] = useState(false);
  const [is2FAActive, setIs2FAActive] = useState(false);
  const [pendingHerenciasCount, setPendingHerenciasCount] = useState(0);

  const fetchCounts = async () => {
    if (!token) return;
    try {
      const assets = await AssetsService.getAssets();
      setAssetsCount(assets.length);

      const beneficiaries = await AssetsService.getMisBeneficiarios();
      setBeneficiariosCount(beneficiaries.length);

      // Herencias pendientes de aceptar/rechazar: se destacan en un banner del Dashboard.
      const herencias = await AssetsService.getMisHerencias();
      setPendingHerenciasCount(herencias.filter((h) => h.estado === 'Pendiente').length);

      const configuracion = await VerificacionVidaService.obtenerConfiguracion();
      setIsLifeVerificationActive(configuracion.activo);

      if (userId) {
        const usuario = await UsuariosService.obtenerPorId(userId);
        setIs2FAActive(usuario.dobleFactorHabilitado);
      }
    } catch (err: any) {
      console.log('Error fetching dashboard counts:', err);
      if (err.message && err.message.includes('401')) {
        signOut();
      }
    }
  };

  useFocusEffect(
    useCallback(() => {
      fetchCounts();
    }, [token, userId])
  );

  useEffect(() => {
    if (success === 'true') {
      setShowSuccess(true);
      router.setParams({ success: undefined });

      const timer = setTimeout(() => {
        setShowSuccess(false);
      }, 3500);
      
      return () => clearTimeout(timer);
    }
  }, [success]);

  const handleNavigateToTab = (tabName: string) => {
    if (tabName === 'Activos') {
      router.push('/activos');
    } else if (tabName === 'Beneficiarios') {
      router.push('/beneficiarios');
    } else if (tabName === 'Seguridad') {
      router.push('/seguridad');
    }
  };

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
