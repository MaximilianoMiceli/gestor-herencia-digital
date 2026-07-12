/**
 * @file ayuda.tsx
 * @description Pantalla de ayuda / acerca de la app, pensada para un usuario nuevo que
 * todavía no entiende para qué sirve cada sección. Es contenido estático (no llama a
 * ningún endpoint): una guía rápida de qué es "Gestor de Herencia Digital" y cómo se usa.
 */

import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, Archive, Users, HeartPulse, FileCheck2, ShieldCheck } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

/**
 * Estructura de cada bloque temático mostrado en la pantalla de ayuda.
 */
interface Seccion {
  /** Componente de ícono (lucide-react-native) que ilustra la sección. */
  icon: any;
  /** Título corto del tema (ej. "Beneficiarios"). */
  titulo: string;
  /** Texto explicativo en lenguaje simple, sin jerga técnica. */
  texto: string;
}

// Contenido estático de la ayuda: un resumen de cada funcionalidad principal de la app,
// pensado para que un usuario nuevo entienda el flujo completo sin tener que navegar
// cada pantalla. Se recorre con .map() para renderizar las cards en el mismo orden.
const SECCIONES: Seccion[] = [
  {
    icon: Archive,
    titulo: 'Activos digitales',
    texto:
      'Registrá cuentas bancarias, billeteras cripto, redes sociales, correos electrónicos o archivos importantes, junto con instrucciones para que tu beneficiario sepa cómo acceder a ellos el día que los necesite.',
  },
  {
    icon: Users,
    titulo: 'Beneficiarios',
    texto:
      'Al crear un activo, invitás a la persona que lo va a heredar con su email. Si todavía no tiene cuenta, la invitación queda esperando y se vincula sola apenas se registre con ese mismo correo.',
  },
  {
    icon: HeartPulse,
    titulo: 'Verificación de vida',
    texto:
      'Es el corazón de la app: configurás cada cuánto tenés que confirmar que seguís activo (3, 6 o 12 meses). Si no respondés, se te envían recordatorios y, si el silencio continúa, se notifica a tu contacto de confianza y se habilita el siguiente paso.',
  },
  {
    icon: FileCheck2,
    titulo: 'Certificado de defunción',
    texto:
      'Si el sistema detecta inactividad prolongada, un heredero ya aceptado puede subir el certificado de defunción real. Un administrador lo revisa y, al aprobarlo, se liberan los activos hacia todos los herederos aceptados.',
  },
  {
    icon: ShieldCheck,
    titulo: 'Seguridad de tu cuenta',
    texto:
      'Desde "Editar perfil" podés cambiar tu contraseña y activar la verificación en dos pasos (2FA): un código que te llega por email cada vez que inicies sesión, además de tu contraseña habitual.',
  },
];

/**
 * Pantalla de ayuda: header con botón de volver y un listado de cards informativas
 * (una por cada entrada de SECCIONES) que explican el propósito de la app.
 */
export default function AyudaScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();

  return (
    <View style={styles.container}>
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.back()} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Ayuda</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>
          <Text style={styles.intro}>
            Gestor de Herencia Digital te ayuda a organizar tus cuentas y activos importantes para que,
            si algún día no podés gestionarlos vos mismo, la persona que elijas pueda acceder a ellos de
            forma ordenada y verificada.
          </Text>

          {SECCIONES.map((seccion) => {
            // Se reasigna a una variable con mayúscula inicial para poder usarlo como
            // componente JSX (<Icon .../>); React exige esa convención para distinguir
            // un componente de una etiqueta HTML nativa.
            const Icon = seccion.icon;
            return (
              <View key={seccion.titulo} style={styles.card}>
                <View style={styles.cardHeaderRow}>
                  <View style={styles.iconWrapper}>
                    <Icon size={20} color="#23856C" />
                  </View>
                  <Text style={styles.cardTitle}>{seccion.titulo}</Text>
                </View>
                <Text style={styles.cardText}>{seccion.texto}</Text>
              </View>
            );
          })}
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
  },
  header: {
    paddingHorizontal: 20,
    paddingBottom: 20,
    borderBottomLeftRadius: 20,
    borderBottomRightRadius: 20,
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  backButton: {
    padding: 4,
  },
  headerTitle: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    textAlign: 'center',
  },
  scrollContent: {
    padding: 24,
    paddingBottom: 40,
  },
  centeredWrapper: {
    width: '100%',
    maxWidth: 600,
    alignSelf: 'center',
    gap: 16,
  },
  intro: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#1a2e2e',
    lineHeight: 20,
    marginBottom: 4,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 18,
    gap: 10,
  },
  cardHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  iconWrapper: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#EEFDE2',
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  cardText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#5E746A',
    lineHeight: 19,
  },
});
