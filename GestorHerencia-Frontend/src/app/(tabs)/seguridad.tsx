/**
 * @file seguridad.tsx
 * @description Pantalla de la pestaña de Seguridad del Tab Navigator.
 *
 * Antes era un placeholder: un texto genérico y el botón de logout, sin ningún otro
 * contenido real. Ahora es el punto de entrada a "Editar perfil" (datos, contraseña,
 * 2FA) y, si el usuario autenticado tiene rol Administrador, también al panel de
 * revisión de certificados de defunción.
 */

import React from 'react';
import { StyleSheet, View, TouchableOpacity, Text } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { useRouter } from 'expo-router';
import { useAuth } from '../../context/AuthContext';
import { LogOut, User, ShieldCheck, HelpCircle, ChevronRight } from 'lucide-react-native';

/**
 * Pantalla "Más" (pestaña Seguridad del Tab Navigator). Menú de accesos a Editar perfil,
 * al panel de administrador (condicional según rol) y a Ayuda, más el botón de logout.
 */
export default function SeguridadScreen() {
  const router = useRouter();
  const { signOut, userRole } = useAuth();

  return (
    <View style={styles.container}>
      {/* Cabecera estilizada con degradado en concordancia con el diseño del Dashboard */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={styles.header}
      >
        {/* SafeAreaView previene solapamiento con la barra de estado superior (notches) */}
        <SafeAreaView edges={['top']} style={styles.headerSafeArea}>
          <Text style={styles.headerSubtitle}>Gestor de Herencia Digital</Text>
          <Text style={styles.headerTitle}>Más</Text>
        </SafeAreaView>
      </LinearGradient>

      {/* Contenido principal */}
      <View style={styles.content}>
        <View style={styles.menuCard}>
          <TouchableOpacity style={styles.menuRow} onPress={() => router.push('/editar-perfil')}>
            <View style={styles.menuIconWrapper}>
              <User size={20} color="#23856C" />
            </View>
            <Text style={styles.menuText}>Editar perfil y contraseña</Text>
            <ChevronRight size={20} color="#8A9E95" />
          </TouchableOpacity>

          {/* Solo visible para el rol Administrador: revisión de certificados de defunción */}
          {userRole === 'Administrador' && (
            <TouchableOpacity
              style={styles.menuRow}
              onPress={() => router.push('/admin/certificados')}
            >
              <View style={styles.menuIconWrapper}>
                <ShieldCheck size={20} color="#23856C" />
              </View>
              <Text style={styles.menuText}>Panel de administrador</Text>
              <ChevronRight size={20} color="#8A9E95" />
            </TouchableOpacity>
          )}

          <TouchableOpacity style={[styles.menuRow, styles.menuRowLast]} onPress={() => router.push('/ayuda')}>
            <View style={styles.menuIconWrapper}>
              <HelpCircle size={20} color="#23856C" />
            </View>
            <Text style={styles.menuText}>Ayuda</Text>
            <ChevronRight size={20} color="#8A9E95" />
          </TouchableOpacity>
        </View>

        {/* Botón de Cerrar Sesión: borra el token y redirige a welcome */}
        <TouchableOpacity style={styles.logoutButton} onPress={signOut}>
          <LogOut color="#A83232" size={20} style={{ marginRight: 8 }} />
          <Text style={styles.logoutText}>Cerrar sesión</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo verde claro unificado de la app
  },
  header: {
    paddingHorizontal: 20,
    paddingBottom: 20,
    borderBottomLeftRadius: 16,
    borderBottomRightRadius: 16,
  },
  headerSafeArea: {
    paddingTop: 10,
    gap: 4,
  },
  headerSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 12,
    color: '#ffffff',
    opacity: 0.7,
  },
  headerTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    color: '#ffffff',
  },
  content: {
    flex: 1,
    padding: 20,
    gap: 20,
  },
  menuCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    overflow: 'hidden',
  },
  menuRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 18,
    paddingHorizontal: 16,
    gap: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#EEFDE2',
  },
  menuRowLast: {
    borderBottomWidth: 0,
  },
  menuIconWrapper: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#EEFDE2',
    alignItems: 'center',
    justifyContent: 'center',
  },
  menuText: {
    flex: 1,
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#1a2e2e',
  },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1.5,
    borderColor: '#A83232', // Borde rojo indicativo de acción destructiva
    borderRadius: 8,
    paddingVertical: 12,
    paddingHorizontal: 24,
    backgroundColor: '#FFFFFF',
    shadowColor: '#A83232',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 1,
  },
  logoutText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#A83232',
  },
});
