/**
 * @file agregar-beneficiario.tsx
 * @description Formulario de alta de beneficiario e invitaciones (Frames 10 y 29).
 * 
 * Posibilita al usuario registrar un heredero por su Nombre, Relación y Correo electrónico.
 * Valida la estructura del email en el cliente y despliega advertencias visuales personalizadas
 * (banner "Email inválido" y recuadro rojo, Frame 29) si no cumple el formato esperado.
 * Envía la petición HTTP POST /api/beneficiarios en base a la sesión del usuario.
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  Modal,
  FlatList,
  Platform,
  Alert,
  KeyboardAvoidingView,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, Info, AlertTriangle, ChevronDown, ChevronUp } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../context/AuthContext';
import { AssetsService } from '../services/assets.service';

const RELACIONES_SEED = [
  'Esposo/a',
  'Hijo/a',
  'Padre/Madre',
  'Hermano/a',
  'Tío/a',
  'Sobrino/a',
  'Primo/a',
  'Otro',
];

export default function AgregarBeneficiarioScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const [nombre, setNombre] = useState('');
  const [parentesco, setParentesco] = useState('');
  const [email, setEmail] = useState('');

  const [loading, setLoading] = useState(false);
  const [showPicker, setShowPicker] = useState(false);
  const [emailError, setEmailError] = useState(false);

  // Expresión regular estándar para validación sintáctica de direcciones de email
  const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  /**
   * Valida la estructura del email y envía la petición de creación al backend.
   */
  const handleEnviarInvitacion = async () => {
    // Resetear error
    setEmailError(false);

    if (!nombre.trim()) {
      Alert.alert('Datos incompletos', 'Por favor, ingresa el nombre del beneficiario.');
      return;
    }

    if (!parentesco) {
      Alert.alert('Datos incompletos', 'Por favor, selecciona la relación o parentesco.');
      return;
    }

    // Validar formato del email
    const esEmailValido = EMAIL_REGEX.test(email.trim());
    if (!email.trim() || !esEmailValido) {
      setEmailError(true);
      return;
    }

    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    setLoading(false);
    try {
      setLoading(true);
      await AssetsService.createBeneficiario(token, {
        nombre: nombre.trim(),
        email: email.trim().toLowerCase(),
        parentesco,
      });

      Alert.alert('Invitación Enviada', 'Se ha registrado el beneficiario e invitado con éxito.', [
        { text: 'OK', onPress: () => router.replace('/(tabs)/beneficiarios') },
      ]);
    } catch (err: any) {
      Alert.alert('Error al registrar', err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <View style={styles.container}>
        {/* HEADER DE ALTA FIDELIDAD CON GRADIENTE Y BOTÓN VOLVER */}
        <LinearGradient
          colors={['#23856C', '#022739']}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 0.5 }}
          style={[styles.header, { paddingTop: insets.top + 20 }]}
        >
          <View style={styles.headerContent}>
            <TouchableOpacity onPress={() => router.replace('/(tabs)/beneficiarios')} style={styles.backButton}>
              <ArrowLeft size={24} color="#FFFFFF" />
            </TouchableOpacity>
            <Text style={styles.headerTitle}>Agregar beneficiario</Text>
            <View style={{ width: 24 }} />
          </View>
        </LinearGradient>

        <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
          <View style={styles.centeredWrapper}>
            
            {/* CAMPO: NOMBRE COMPLETO */}
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Nombre completo</Text>
              <TextInput
                style={styles.textInput}
                placeholder="Nombre del beneficiario"
                placeholderTextColor="#8A9E95"
                value={nombre}
                onChangeText={setNombre}
              />
            </View>

            {/* CAMPO: RELACIÓN (Selector expandible inline) */}
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Relación</Text>
              <View style={styles.dropdownContainer}>
                <TouchableOpacity
                  style={[
                    styles.pickerSelector,
                    showPicker && styles.pickerSelectorExpanded
                  ]}
                  activeOpacity={0.8}
                  onPress={() => setShowPicker(!showPicker)}
                >
                  <Text style={parentesco ? styles.pickerSelectorText : styles.pickerPlaceholderText}>
                    {parentesco || 'Seleccionar relación'}
                  </Text>
                  {showPicker ? (
                    <ChevronUp size={20} color="#8A9E95" />
                  ) : (
                    <ChevronDown size={20} color="#8A9E95" />
                  )}
                </TouchableOpacity>

                {showPicker && (
                  <View style={styles.dropdownOptionsList}>
                    <ScrollView style={{ maxHeight: 180 }} nestedScrollEnabled>
                      {RELACIONES_SEED.map((item) => (
                        <TouchableOpacity
                          key={item}
                          style={styles.dropdownOptionItem}
                          onPress={() => {
                            setParentesco(item);
                            setShowPicker(false);
                          }}
                        >
                          <Text style={styles.dropdownOptionText}>{item}</Text>
                        </TouchableOpacity>
                      ))}
                    </ScrollView>
                  </View>
                )}
              </View>
            </View>

            {/* ERROR DE EMAIL (Banner Fiel al Frame 29) */}
            {emailError && (
              <View style={styles.errorBanner}>
                <AlertTriangle size={18} color="#C53929" />
                <Text style={styles.errorBannerText}>Email inválido</Text>
              </View>
            )}

            {/* CAMPO: EMAIL */}
            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Email</Text>
              <TextInput
                style={[styles.textInput, emailError && styles.textInputError]}
                placeholder="Email"
                placeholderTextColor="#8A9E95"
                keyboardType="email-address"
                autoCapitalize="none"
                value={email}
                onChangeText={(val) => {
                  setEmail(val);
                  if (emailError) setEmailError(false);
                }}
              />
            </View>

            {/* RECUADRO INFORMATIVO DE INVITACIÓN (Frame 10 / 29) */}
            <View style={styles.infoBox}>
              <Info size={20} color="#0E4A4C" style={styles.infoIcon} />
              <Text style={styles.infoText}>
                Se enviará una invitación al email ingresado para que el beneficiario cree su cuenta.
              </Text>
            </View>

            {/* BOTÓN "ENVIAR INVITACIÓN" */}
            <View style={styles.buttonWrapper}>
              {loading ? (
                <ActivityIndicator size="large" color="#E2A53C" style={{ marginVertical: 10 }} />
              ) : (
                <TouchableOpacity style={styles.submitButton} onPress={handleEnviarInvitacion}>
                  <Text style={styles.submitButtonText}>Enviar invitación</Text>
                </TouchableOpacity>
              )}
            </View>
          </View>
        </ScrollView>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD', // Fondo general verde pastel
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
  inputGroup: {
    width: '100%',
    gap: 8,
  },
  inputLabel: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
    color: '#1a2e2e',
  },
  textInput: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    height: 52,
    paddingHorizontal: 16,
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#1a2e2e',
  },
  textInputError: {
    borderColor: '#C53929', // Borde rojo por error de email (Frame 29)
    borderWidth: 1.5,
  },
  pickerSelector: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    borderRadius: 12,
    height: 52,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  pickerSelectorText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#1a2e2e',
  },
  pickerPlaceholderText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#8A9E95',
  },
  // Banner de alerta (Frame 29)
  errorBanner: {
    backgroundColor: '#FCE8E6', // Fondo rosado
    borderColor: '#F8C0B9',
    borderWidth: 1,
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: -8,
  },
  errorBannerText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#C53929', // Texto rojo
  },
  infoBox: {
    backgroundColor: '#C5E2D0', // Fondo celeste/verde agua
    borderRadius: 12,
    borderWidth: 1.2,
    borderColor: '#A1CBB2',
    paddingVertical: 16,
    paddingHorizontal: 16,
    alignItems: 'center',
    flexDirection: 'column',
    gap: 8,
    marginTop: 8,
  },
  infoIcon: {
    marginBottom: 2,
  },
  infoText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#0E4A4C',
    textAlign: 'center',
    lineHeight: 20,
    paddingHorizontal: 8,
  },
  buttonWrapper: {
    marginTop: 12,
    width: '100%',
  },
  submitButton: {
    backgroundColor: '#E2A53C', // Botón naranja
    height: 52,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    ...Platform.select({
      ios: {
        shadowColor: '#E2A53C',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.2,
        shadowRadius: 6,
      },
      android: {
        elevation: 2,
      },
    }),
  },
  submitButtonText: {
    color: '#FFFFFF',
    fontSize: 18,
    fontFamily: 'MPLUS2-Bold',
  },
  // Estilos del dropdown inline
  dropdownContainer: {
    width: '100%',
    position: 'relative',
    zIndex: 50,
  },
  pickerSelectorExpanded: {
    borderBottomLeftRadius: 0,
    borderBottomRightRadius: 0,
  },
  dropdownOptionsList: {
    backgroundColor: '#FFFFFF',
    borderLeftWidth: 1,
    borderRightWidth: 1,
    borderBottomWidth: 1,
    borderColor: '#C1E3A4',
    borderBottomLeftRadius: 12,
    borderBottomRightRadius: 12,
    marginTop: -1,
    overflow: 'hidden',
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.08,
        shadowRadius: 4,
      },
      android: {
        elevation: 2,
      },
    }),
  },
  dropdownOptionItem: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#EEFDE2',
  },
  dropdownOptionText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 16,
    color: '#1a2e2e',
  },
});
