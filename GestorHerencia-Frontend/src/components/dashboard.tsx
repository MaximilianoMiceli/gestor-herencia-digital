import React from 'react';
import {
  StyleSheet,
  View,
  Text,
  ScrollView,
  Pressable,
  Platform,
  Alert,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { useRouter } from 'expo-router';
import { Archive, Users, CheckCircle, Lock, User, AlertCircle, Info } from 'lucide-react-native';
import Animated, {
  useSharedValue,
  useAnimatedStyle,
  withSpring,
  SharedValue,
} from 'react-native-reanimated';

/**
 * Propiedades del componente Dashboard.
 */
interface DashboardProps {
  /** Nombre del usuario para mostrar el saludo personalizado en el Header. */
  userName?: string;
  /** Cantidad de activos digitales registrados por el usuario. */
  assetsCount?: number;
  /** Cantidad de beneficiarios asignados a los activos del usuario. */
  beneficiariosCount?: number;
  /** Estado del sistema de verificación de vida del usuario (activo/inactivo). */
  isLifeVerificationActive?: boolean;
  /** Estado del segundo factor de autenticación para protección de datos sensibles. */
  is2FAActive?: boolean;
  /** Cantidad de herencias recibidas que están pendientes de aceptar/rechazar. */
  pendingHerenciasCount?: number;
  /** Callback para la acción de agregar un nuevo activo. */
  onAddAsset?: () => void;
  /** Callback para la navegación hacia otras pestañas principales. */
  onNavigateToTab?: (tabName: string) => void;
  /** Indica si se debe mostrar la notificación de éxito tras guardar un activo. */
  showSuccessNotification?: boolean;
}

/**
 * Componente Dashboard principal. Presenta de manera resumida el estado general
 * del gestor de herencia, incluyendo estadísticas de activos, beneficiarios,
 * y configuraciones de seguridad críticas para el resguardo de la información.
 */
export default function Dashboard({
  userName = '',
  assetsCount = 0,
  beneficiariosCount = 0,
  isLifeVerificationActive = false,
  is2FAActive = false,
  pendingHerenciasCount = 0,
  onAddAsset,
  onNavigateToTab,
  showSuccessNotification = false,
}: DashboardProps) {
  const insets = useSafeAreaInsets();
  const router = useRouter();

  // Escala compartida para animar la pulsación del botón principal.
  const addBtnScale = useSharedValue(1);
  // Escala compartida para animar la pulsación del botón de herencias (azul).
  const inheritancesBtnScale = useSharedValue(1);

  const animatedAddBtnStyle = useAnimatedStyle(() => {
    return {
      transform: [{ scale: addBtnScale.value }],
    };
  });

  const animatedInheritancesBtnStyle = useAnimatedStyle(() => {
    return {
      transform: [{ scale: inheritancesBtnScale.value }],
    };
  });

  const handlePressIn = (scaleVar: SharedValue<number>) => {
    scaleVar.value = withSpring(0.96, { damping: 10, stiffness: 300 });
  };

  const handlePressOut = (scaleVar: SharedValue<number>) => {
    scaleVar.value = withSpring(1, { damping: 10, stiffness: 300 });
  };

  // Fallback para cuando el componente se usa sin conectar a la navegación real.
  const defaultOnAddAsset = () => {
    if (onAddAsset) {
      onAddAsset();
    } else {
      Alert.alert('Nuevo Activo', 'Redirigiendo a agregar un nuevo activo...');
    }
  };

  return (
    <View style={styles.container}>
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        {showSuccessNotification && (
          <View style={[styles.notificationPill, { top: insets.top + 4 }]}>
            <Info size={16} color="#1a2e2e" />
            <Text style={styles.notificationText}>Activo guardado con exito</Text>
          </View>
        )}
        <View style={styles.centeredWrapper}>
          <View style={styles.headerContent}>
            <View style={styles.headerTextContainer}>
              <Text style={styles.headerSubtitle}>Gestor de Herencia Digital</Text>
              <Text style={styles.headerTitle}>
                {userName ? `Bienvenido, ${userName}` : 'Te damos la bienvenida'}
              </Text>
            </View>

            {/* Icono de perfil: lleva directo a "Editar perfil" (datos, contraseña y 2FA) */}
            <Pressable style={styles.profileContainer} onPress={() => router.push('/editar-perfil')}>
              <View style={styles.profileIconCircle}>
                <User size={24} color="#ffffff" strokeWidth={2} />
              </View>
            </Pressable>
          </View>
        </View>
      </LinearGradient>

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.centeredWrapper}>
          {/* Banner persistente (no es una píldora que se autodesvanece): así alguien que ya
              tenía una herencia esperándolo la ve de inmediato, sin buscar "Mis herencias". */}
          {pendingHerenciasCount > 0 && (
            <Pressable style={styles.pendingBanner} onPress={() => router.push('/mis-herencias')}>
              <View style={styles.pendingBannerIconWrapper}>
                <AlertCircle size={22} color="#B25E09" strokeWidth={2} />
              </View>
              <View style={{ flex: 1 }}>
                <Text style={styles.pendingBannerTitle}>
                  {pendingHerenciasCount === 1
                    ? 'Tenés una herencia pendiente'
                    : `Tenés ${pendingHerenciasCount} herencias pendientes`}
                </Text>
                <Text style={styles.pendingBannerSubtitle}>Tocá acá para revisarla y aceptarla</Text>
              </View>
            </Pressable>
          )}

          <View style={styles.card}>
            <Text style={styles.cardTitle}>Estado General</Text>

            <Pressable
              style={styles.cardRow}
              onPress={() => onNavigateToTab?.('Activos')}
            >
              <View style={styles.rowIconContainer}>
                <Archive
                  size={24}
                  color={assetsCount > 0 ? '#23856C' : '#8A9E95'}
                  strokeWidth={2}
                />
              </View>
              <Text
                style={[
                  styles.rowText,
                  assetsCount > 0 ? styles.textDarkGreen : styles.textMuted,
                ]}
              >
                {assetsCount > 0
                  ? `${assetsCount} Activos Guardados`
                  : 'Sin activos guardados'}
              </Text>
            </Pressable>

            <Pressable
              style={styles.cardRow}
              onPress={() => onNavigateToTab?.('Beneficiarios')}
            >
              <View style={styles.rowIconContainer}>
                <Users
                  size={24}
                  color={beneficiariosCount > 0 ? '#23856C' : '#8A9E95'}
                  strokeWidth={2}
                />
              </View>
              <Text
                style={[
                  styles.rowText,
                  beneficiariosCount > 0 ? styles.textDarkGreen : styles.textMuted,
                ]}
              >
                {beneficiariosCount > 0
                  ? `${beneficiariosCount} Beneficiarios Asignados`
                  : 'Sin beneficiarios asignados'}
              </Text>
            </Pressable>

            <Pressable
              style={styles.cardRow}
              onPress={() => router.push('/verificacion-vida' as any)}
            >
              <View style={styles.rowIconContainer}>
                {isLifeVerificationActive ? (
                  <CheckCircle size={24} color="#23856C" strokeWidth={2} />
                ) : (
                  <AlertCircle size={24} color="#f59e0b" strokeWidth={2} />
                )}
              </View>
              <Text
                style={[
                  styles.rowText,
                  isLifeVerificationActive ? styles.textDarkGreen : styles.textOrange,
                ]}
              >
                {isLifeVerificationActive
                  ? 'Verificación de vida activa'
                  : 'Configurar verificación de vida'}
              </Text>
            </Pressable>

            {/* Navega directo a la sección 2FA de "Editar perfil" (donde vive el toggle real)
                en vez de la pestaña Más, evitando un segundo toque + scroll para encontrarlo. */}
            <Pressable
              style={[styles.cardRow, styles.lastRow]}
              onPress={() => router.push({ pathname: '/editar-perfil', params: { focus: '2fa' } })}
            >
              <View style={styles.rowIconContainer}>
                <Lock
                  size={24}
                  color={is2FAActive ? '#23856C' : '#f59e0b'}
                  strokeWidth={2}
                />
              </View>
              <Text
                style={[
                  styles.rowText,
                  is2FAActive ? styles.textDarkGreen : styles.textOrange,
                ]}
              >
                {is2FAActive ? '2FA Activo' : '2FA Inactivo'}
              </Text>
            </Pressable>
          </View>

          <Animated.View style={animatedAddBtnStyle}>
            <Pressable
              style={styles.addButtonGreen}
              onPress={defaultOnAddAsset}
              onPressIn={() => handlePressIn(addBtnScale)}
              onPressOut={() => handlePressOut(addBtnScale)}
            >
              <Text style={styles.addButtonGreenText}>+ Agregar nuevo activo</Text>
            </Pressable>
          </Animated.View>

          {/* Azul en vez de naranja/amarillo: ese color ya se usa como aviso de "2FA Inactivo" y
              verificación de vida pendiente (ver textOrange); reutilizarlo acá lo hacía leerse
              como advertencia en vez de una acción normal de navegación. */}
          <Animated.View style={[animatedInheritancesBtnStyle, { marginTop: 16 }]}>
            <View style={styles.inheritancesBtnWrapper}>
              <Pressable
                style={styles.addButtonBlue}
                onPress={() => router.push('/mis-herencias')}
                onPressIn={() => handlePressIn(inheritancesBtnScale)}
                onPressOut={() => handlePressOut(inheritancesBtnScale)}
              >
                <Text style={styles.addButtonBlueText}>Mis herencias</Text>
              </Pressable>
              {/* Mismo dato que el banner de arriba (pendingHerenciasCount): un aviso visible
                  sin necesidad de leer el banner completo. */}
              {pendingHerenciasCount > 0 && (
                <View style={styles.inheritancesBtnDot} />
              )}
            </View>
          </Animated.View>
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo general verde claro pastel
  },
  centeredWrapper: {
    width: '100%',
    maxWidth: 600,
    alignSelf: 'center',
  },
  header: {
    paddingHorizontal: 20,
    paddingBottom: 25,
    borderBottomLeftRadius: 20,
    borderBottomRightRadius: 20,
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.15,
        shadowRadius: 10,
      },
      android: {
        elevation: 8,
      },
    }),
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  headerTextContainer: {
    flex: 1,
    gap: 4,
  },
  headerSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#ffffff',
    opacity: 0.7,
  },
  headerTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 22,
    color: '#ffffff',
  },
  profileContainer: {
    marginLeft: 15,
  },
  profileIconCircle: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 1.5,
    borderColor: '#ffffff',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
  },
  scrollContent: {
    paddingHorizontal: 20,
    paddingTop: 32,
    paddingBottom: 24, // Reducido para quitar el espacio sobrante innecesario
  },
  pendingBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FFF4DE',
    borderWidth: 1.5,
    borderColor: '#F0A83C',
    borderRadius: 16,
    padding: 16,
    marginBottom: 20,
    gap: 12,
    ...Platform.select({
      ios: {
        shadowColor: '#B25E09',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.12,
        shadowRadius: 6,
      },
      android: {
        elevation: 2,
      },
    }),
  },
  pendingBannerIconWrapper: {
    width: 38,
    height: 38,
    borderRadius: 19,
    backgroundColor: '#FFE7BC',
    alignItems: 'center',
    justifyContent: 'center',
  },
  pendingBannerTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#8A4B0C',
  },
  pendingBannerSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 12,
    color: '#B25E09',
    marginTop: 2,
  },
  card: {
    backgroundColor: '#EEFDE2', // Fondo ligeramente más claro que el general (#DAF8BD)
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde sutil gris/verde
    borderRadius: 20,
    ...Platform.select({
      ios: {
        shadowColor: '#23856C',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.08,
        shadowRadius: 6,
      },
      android: {
        elevation: 2,
      },
    }),
  },
  cardTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#1a2e2e',
    padding: 20,
  },
  cardRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 20,
    paddingHorizontal: 20,
    borderBottomWidth: 0.5,
    borderBottomColor: '#C1E3A4',
  },
  lastRow: {
    borderBottomWidth: 0,
  },
  rowIconContainer: {
    width: 24,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
  },
  rowText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    flex: 1,
  },
  textDarkGreen: {
    color: '#23856C',
  },
  textOrange: {
    color: '#f59e0b',
  },
  textMuted: {
    color: '#8A9E95',
  },
  addButtonGreen: {
    marginTop: 28,
    backgroundColor: '#EEFDE2', // Fondo verde claro pastel
    borderWidth: 1.5,
    borderColor: '#2E7D32', // Borde verde oscuro
    borderRadius: 16,
    paddingVertical: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  addButtonGreenText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#2E7D32',
  },
  inheritancesBtnWrapper: {
    // 'relative' para poder anclar el puntito de notificación en una esquina del botón.
    position: 'relative',
  },
  addButtonBlue: {
    backgroundColor: '#E7F1FA', // Fondo azul claro pastel (coherente con el degradé del header)
    borderWidth: 1.5,
    borderColor: '#2D6E9E', // Borde azul
    borderRadius: 16,
    paddingVertical: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  addButtonBlueText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#2D6E9E',
  },
  inheritancesBtnDot: {
    position: 'absolute',
    top: -4,
    right: -4,
    width: 14,
    height: 14,
    borderRadius: 7,
    backgroundColor: '#D32F2F', // Rojo de notificación (mismo tono que usamos para "rechazado")
    borderWidth: 2,
    borderColor: '#DAF8BD', // Borde del color de fondo general, para que el punto no se vea pegado al botón
  },
  notificationPill: {
    position: 'absolute',
    alignSelf: 'center',
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    paddingVertical: 8,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#1a2e2e',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 4,
    zIndex: 999,
  },
  notificationText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    color: '#1a2e2e',
    marginLeft: 8,
  },
});
