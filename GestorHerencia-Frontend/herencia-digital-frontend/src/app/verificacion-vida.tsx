/**
 * @file verificacion-vida.tsx
 * @description Pantalla de configuración del sistema de Verificación de Vida (Frames 11, 37 y 39).
 * 
 * Permite al usuario activar/desactivar el monitoreo de vida, definir la frecuencia de chequeo,
 * el método de verificación (Notificaciones Push, Email, SMS) y configurar un contacto de confianza.
 * Guarda las configuraciones localmente en SecureStore asociadas al ID de usuario único.
 * Utiliza selectores colapsables en línea idénticos a los del formulario de Nuevo Activo para
 * preservar la consistencia visual y de interacción en la app.
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  Switch,
  ActivityIndicator,
  Alert,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, ChevronDown, ChevronUp, AlertCircle } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import * as SecureStore from 'expo-secure-store';
import { useAuth } from '../context/AuthContext';
import { AssetsService } from '../services/assets.service';

export default function VerificacionVidaScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token, userEmail } = useAuth();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Estados de configuración
  const [isActive, setIsActive] = useState(false);
  const [frecuencia, setFrecuencia] = useState('Cada 3 meses');
  const [metodo, setMetodo] = useState('Notificación push');
  const [contacto, setContacto] = useState('');

  // Control de visibilidad de los dropdowns (estilo inline condicional)
  const [showFrecuenciaDropdown, setShowFrecuenciaDropdown] = useState(false);
  const [showMetodoDropdown, setShowMetodoDropdown] = useState(false);

  const opcionesFrecuencia = ['Cada 3 meses', 'Cada 6 meses', 'Cada 12 meses'];
  const opcionesMetodo = ['Notificación push', 'Email', 'SMS'];

  // Cargar configuración previa al montar la pantalla
  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarConfiguracion = async () => {
      if (!userEmail) {
        setLoading(false);
        return;
      }
      try {
        const sanitizedEmail = userEmail.replace(/[^a-zA-Z0-9.-]/g, '_');
        const key = `life-verification-config-${sanitizedEmail}`;
        const savedData = await SecureStore.getItemAsync(key);
        if (savedData) {
          const config = JSON.parse(savedData);
          setIsActive(config.isActive ?? false);
          setFrecuencia(config.frecuencia ?? 'Cada 3 meses');
          setMetodo(config.metodo ?? 'Notificación push');
          setContacto(config.contacto ?? '');
        }
      } catch (err) {
        console.log('Error loading life verification config:', err);
      } finally {
        setLoading(false);
      }
    };

    cargarConfiguracion();
  }, [token, userEmail]);

  /**
   * Valida si el contacto de confianza ingresado es un beneficiario verificado (estado Aceptado/2)
   */
  const handleToggleSwitch = async (value: boolean) => {
    if (!value) {
      setIsActive(false);
      return;
    }

    // El contacto de confianza no puede estar vacío si se intenta activar el monitoreo
    if (!contacto.trim()) {
      Alert.alert(
        'Contacto Requerido',
        'Por favor, ingresa el email del contacto de confianza antes de activar la verificación.'
      );
      return;
    }

    try {
      const bList = await AssetsService.getMisBeneficiarios(token!);
      const query = contacto.trim().toLowerCase();

      // El backend no tiene "nombre" de beneficiario: la única coincidencia posible es por email
      const contactBeneficiary = bList.find((b) => b.email.toLowerCase() === query);

      if (!contactBeneficiary) {
        Alert.alert(
          'Contacto no registrado',
          'El contacto de confianza debe ser uno de tus beneficiarios registrados en el sistema.'
        );
        return;
      }

      // El estado del beneficiario en la base de datos debe ser Aceptado (2) -> "Verificado" en la UI
      if (contactBeneficiary.estado !== 2) {
        Alert.alert(
          'Contacto no verificado',
          'El contacto seleccionado debe haber confirmado su email y aceptado la invitación (estado "Verificado") para poder ser tu contacto de confianza activo.'
        );
        return;
      }

      // Si pasa todas las reglas de negocio, se permite activar
      setIsActive(true);
    } catch (err) {
      console.log('Error al validar contacto:', err);
      Alert.alert('Error de validación', 'Ocurrió un error al verificar los permisos del contacto.');
    }
  };

  /**
   * Guarda la configuración de verificación de vida de forma persistente y local.
   */
  const handleGuardarConfiguracion = async () => {
    if (!token || !userEmail) return;

    if (isActive && !contacto.trim()) {
      Alert.alert('Contacto Requerido', 'Debes configurar un nombre o email de contacto de confianza.');
      return;
    }

    setSaving(true);
    try {
      const sanitizedEmail = userEmail.replace(/[^a-zA-Z0-9.-]/g, '_');
      const key = `life-verification-config-${sanitizedEmail}`;
      const configData = {
        isActive,
        frecuencia,
        metodo,
        contacto: contacto.trim(),
      };

      await SecureStore.setItemAsync(key, JSON.stringify(configData));

      Alert.alert(
        'Configuración guardada',
        'El sistema de verificación de vida se ha configurado con éxito.',
        [{ text: 'OK', onPress: () => router.replace('/') }]
      );
    } catch (err: any) {
      console.log('Error saving life verification config:', err);
      Alert.alert('Error', 'Ocurrió un error al guardar la configuración.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando configuración...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* HEADER CON GRADIENTE */}
      <LinearGradient
        colors={['#23856C', '#022739']}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 0.5 }}
        style={[styles.header, { paddingTop: insets.top + 20 }]}
      >
        <View style={styles.headerContent}>
          <TouchableOpacity onPress={() => router.replace('/')} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Verificación de vida</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>

          {/* CARD DE ESTADO (Frame 11/37/39) */}
          <View style={styles.statusCard}>
            <View style={styles.statusTextContainer}>
              <Text style={styles.statusLabel}>Estado</Text>
              <Text style={[styles.statusValue, isActive ? styles.statusActive : styles.statusInactive]}>
                {isActive ? 'Activo' : 'Inactivo'}
              </Text>
            </View>
            <Switch
              value={isActive}
              onValueChange={handleToggleSwitch}
              trackColor={{ false: '#CCD3CE', true: '#39C55C' }}
              thumbColor="#FFFFFF"
              ios_backgroundColor="#CCD3CE"
            />
          </View>

          {/* CAMPO: FRECUENCIA DE CHEQUEO (DROPDOWN CON HIGHLIGHT) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Frecuencia de chequeo</Text>
            <TouchableOpacity
              style={[
                styles.dropdownButton,
                showFrecuenciaDropdown && styles.dropdownButtonActive,
              ]}
              activeOpacity={0.8}
              onPress={() => {
                setShowFrecuenciaDropdown(!showFrecuenciaDropdown);
                setShowMetodoDropdown(false);
              }}
            >
              <Text style={[
                styles.dropdownButtonText,
                showFrecuenciaDropdown && styles.dropdownButtonTextActive,
              ]}>{frecuencia}</Text>
              {showFrecuenciaDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
            </TouchableOpacity>

            {showFrecuenciaDropdown && (
              <View style={styles.dropdownInlineList}>
                {opcionesFrecuencia.map((f) => (
                  <TouchableOpacity
                    key={f}
                    style={[
                      styles.dropdownInlineOption,
                      frecuencia === f && styles.dropdownInlineOptionSelected,
                    ]}
                    onPress={() => {
                      setFrecuencia(f);
                      setShowFrecuenciaDropdown(false);
                    }}
                  >
                    <Text
                      style={[
                        styles.dropdownInlineOptionText,
                        frecuencia === f && styles.dropdownInlineOptionTextSelected,
                      ]}
                    >
                      {f}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </View>

          {/* CAMPO: MÉTODO DE VERIFICACIÓN (DROPDOWN CON HIGHLIGHT) */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Metodo de verificacion</Text>
            <TouchableOpacity
              style={[
                styles.dropdownButton,
                showMetodoDropdown && styles.dropdownButtonActive,
              ]}
              activeOpacity={0.8}
              onPress={() => {
                setShowMetodoDropdown(!showMetodoDropdown);
                setShowFrecuenciaDropdown(false);
              }}
            >
              <Text style={[
                styles.dropdownButtonText,
                showMetodoDropdown && styles.dropdownButtonTextActive,
              ]}>{metodo}</Text>
              {showMetodoDropdown ? (
                <ChevronUp size={20} color="#2E7D32" />
              ) : (
                <ChevronDown size={20} color="#1a2e2e" />
              )}
            </TouchableOpacity>

            {showMetodoDropdown && (
              <View style={styles.dropdownInlineList}>
                {opcionesMetodo.map((m) => (
                  <TouchableOpacity
                    key={m}
                    style={[
                      styles.dropdownInlineOption,
                      metodo === m && styles.dropdownInlineOptionSelected,
                    ]}
                    onPress={() => {
                      setMetodo(m);
                      setShowMetodoDropdown(false);
                    }}
                  >
                    <Text
                      style={[
                        styles.dropdownInlineOptionText,
                        metodo === m && styles.dropdownInlineOptionTextSelected,
                      ]}
                    >
                      {m}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </View>

          {/* CAMPO: CONTACTO DE CONFIANZA */}
          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Contacto de confianza</Text>
            <TextInput
              style={styles.textInput}
              value={contacto}
              onChangeText={(text) => setContacto(text)}
              placeholder="Email del contacto de confianza"
              placeholderTextColor="#8A9E95"
              keyboardType="email-address"
              autoCapitalize="none"
            />
          </View>

          {/* MENSAJE DE ADVERTENCIA */}
          <View style={styles.infoCard}>
            <AlertCircle size={20} color="#0B4B5C" style={styles.infoIcon} />
            <Text style={styles.infoText}>
              Si no respondes en el plazo configurado, tu contacto de confianza sera notificado antes de activar la herencia.
            </Text>
          </View>

          {/* BOTÓN GUARDAR CONFIGURACIÓN */}
          <TouchableOpacity
            style={styles.saveButton}
            onPress={handleGuardarConfiguracion}
            disabled={saving}
          >
            {saving ? (
              <ActivityIndicator size="small" color="#FFFFFF" />
            ) : (
              <Text style={styles.saveButtonText}>Guardar configuración</Text>
            )}
          </TouchableOpacity>

        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo verde claro pastel
  },
  loadingContainer: {
    flex: 1,
    backgroundColor: '#DAF8BD',
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#23856C',
    marginTop: 12,
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
    gap: 20,
  },
  statusCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  statusTextContainer: {
    gap: 4,
  },
  statusLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  statusValue: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
  },
  statusActive: {
    color: '#39C55C', // Verde activo Figma
  },
  statusInactive: {
    color: '#8A9E95',
  },
  inputGroup: {
    width: '100%',
    gap: 8,
  },
  inputLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#5E746A',
  },
  textInput: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    height: 48,
    paddingHorizontal: 16,
    color: '#000000',
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
  },
  textArea: {
    height: 100,
    paddingTop: 16,
  },
  dropdownButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado (rectangular suave)
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    height: 48,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  dropdownButtonText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#000000',
  },
  dropdownButtonActive: {
    borderColor: '#2E7D32', // Borde verde oscuro activo
    borderWidth: 1.5,
  },
  dropdownButtonTextActive: {
    color: '#2E7D32', // Texto verde oscuro activo
  },
  dropdownInlineList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12, // Menos redondeado
    borderWidth: 1,
    borderColor: '#C1E3A4', // Borde verde claro suave
    padding: 10,
    marginTop: 6,
    gap: 2,
  },
  dropdownInlineOption: {
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8, // Esquinas más suaves para la selección interna
  },
  dropdownInlineOptionSelected: {
    backgroundColor: '#DAF8BD', // Fondo verde claro pastel de selección
  },
  dropdownInlineOptionText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
    color: '#333333',
  },
  dropdownInlineOptionTextSelected: {
    fontFamily: 'MPLUS2-Bold',
    color: '#2E7D32', // Texto verde oscuro para la opción seleccionada
  },
  infoCard: {
    backgroundColor: '#CDE5E9', // Fondo celeste/azul claro (Figma Frame 11)
    borderWidth: 1,
    borderColor: '#7FAAB5',
    borderRadius: 16,
    padding: 16,
    flexDirection: 'row',
    gap: 12,
    alignItems: 'flex-start',
  },
  infoIcon: {
    marginTop: 2,
  },
  infoText: {
    flex: 1,
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#0B4B5C',
    lineHeight: 18,
  },
  saveButton: {
    backgroundColor: '#39C55C', // Verde Figma
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    marginTop: 12,
  },
  saveButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
  },
});
