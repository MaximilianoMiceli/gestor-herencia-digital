import React from 'react';
import { useRouter } from 'expo-router';
import Dashboard from '../components/dashboard';

/**
 * Pantalla principal de la aplicación que sirve como contenedor del Dashboard.
 * Gestiona la navegación y las redirecciones de las acciones ejecutadas en el Dashboard.
 */
export default function HomeScreen() {
  const router = useRouter();

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
   * Redirige al flujo de creación de un nuevo activo (ubicado en la pestaña Activos).
   */
  const handleAddAsset = () => {
    router.push('/activos');
  };

  return (
    <Dashboard
      onAddAsset={handleAddAsset}
      onNavigateToTab={handleNavigateToTab}
    />
  );
}
