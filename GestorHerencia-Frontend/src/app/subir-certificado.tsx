/**
 * @file subir-certificado.tsx
 * @description Pantalla para que un heredero suba el certificado de defunción de un titular
 * que ya lo designó como beneficiario y cuya invitación ya aceptó. El backend exige esa misma
 * condición (CertificadoDefuncionService.SubirCertificadoAsync la valida server-side).
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  ScrollView,
} from 'react-native';
import { useRouter } from 'expo-router';
import { ArrowLeft, ChevronDown, ChevronUp, FileWarning, Paperclip } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import * as DocumentPicker from 'expo-document-picker';
import { useAuth } from '../context/AuthContext';
import { AssetsService, MiHerenciaDTO } from '../services/assets.service';
import { CertificadosService } from '../services/certificados.service';

const TIPOS_MIME_PERMITIDOS = ['application/pdf', 'image/jpeg', 'image/png'];
type ArchivoSeleccionado = { uri: string; name: string; mimeType: string };

/** Un titular único (deduplicado), a partir de las herencias YA aceptadas por el usuario. */
interface TitularElegible {
  titularId: number;
  titularNombre: string;
}

export default function SubirCertificadoScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [titularesElegibles, setTitularesElegibles] = useState<TitularElegible[]>([]);
  // Distingue "no aceptaste ninguna herencia todavía" de "ya aceptaste, pero a todos esos
  // titulares ya se les confirmó el fallecimiento": son dos mensajes distintos para el
  // usuario, aunque en ambos casos el selector termine vacío.
  const [hayAceptadasSinTitularesElegibles, setHayAceptadasSinTitularesElegibles] = useState(false);
  const [titularSeleccionado, setTitularSeleccionado] = useState<TitularElegible | null>(null);
  const [showTitularDropdown, setShowTitularDropdown] = useState(false);
  const [archivoSeleccionado, setArchivoSeleccionado] = useState<ArchivoSeleccionado | null>(null);

  useEffect(() => {
    if (!token) {
      router.replace('/(auth)/welcome');
      return;
    }

    const cargarTitulares = async () => {
      try {
        const herencias: MiHerenciaDTO[] = await AssetsService.getMisHerencias();

        // Solo se puede subir el certificado de un titular cuya invitación YA fue
        // aceptada (misma regla de negocio que aplica el backend) Y cuyo fallecimiento
        // todavía NO fue confirmado antes (si "disponible" ya es true, un certificado de
        // este titular ya fue aprobado: el backend rechaza cualquier otro con
        // ReglaNegocioException, así que ni se lo ofrecemos en el selector). Se deduplica
        // por titularId porque un mismo titular puede haberme asignado varios activos.
        const todasAceptadas = herencias.filter((h) => h.estado === 'Aceptado');
        const aceptadasSinConfirmar = todasAceptadas.filter((h) => !h.disponible);
        const mapa = new Map<number, TitularElegible>();
        aceptadasSinConfirmar.forEach((h) => mapa.set(h.titularId, { titularId: h.titularId, titularNombre: h.titularNombre }));

        setTitularesElegibles(Array.from(mapa.values()));
        setHayAceptadasSinTitularesElegibles(todasAceptadas.length > 0 && mapa.size === 0);
      } catch (err: any) {
        Alert.alert('Error', err.message || 'No se pudieron cargar tus herencias.');
      } finally {
        setLoading(false);
      }
    };

    cargarTitulares();
  }, [token]);

  const handleElegirArchivo = async () => {
    try {
      const resultado = await DocumentPicker.getDocumentAsync({
        type: TIPOS_MIME_PERMITIDOS,
        copyToCacheDirectory: true,
      });

      if (resultado.canceled || resultado.assets.length === 0) {
        return;
      }

      const archivo = resultado.assets[0];
      setArchivoSeleccionado({
        uri: archivo.uri,
        name: archivo.name,
        mimeType: archivo.mimeType ?? 'application/octet-stream',
      });
    } catch (err) {
      Alert.alert('Error', 'No se pudo abrir el selector de archivos.');
    }
  };

  const handleSubir = async () => {
    if (!titularSeleccionado) {
      Alert.alert('Campo requerido', 'Elegí de quién es el certificado que estás subiendo.');
      return;
    }
    if (!archivoSeleccionado) {
      Alert.alert('Campo requerido', 'Adjuntá el certificado (PDF, JPG o PNG).');
      return;
    }

    setSaving(true);
    try {
      await CertificadosService.subirCertificado(titularSeleccionado.titularId, archivoSeleccionado);

      Alert.alert(
        'Certificado enviado',
        'Un administrador va a revisar el certificado. Te vamos a notificar cuando se apruebe o rechace.',
        [{ text: 'OK', onPress: () => router.replace('/') }]
      );
    } catch (err: any) {
      Alert.alert('Error', err.message || 'No se pudo subir el certificado.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando...</Text>
      </View>
    );
  }

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
          <Text style={styles.headerTitle}>Subir certificado</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        <View style={styles.centeredWrapper}>

          <View style={styles.infoBox}>
            <FileWarning size={20} color="#0E4A4C" />
            <Text style={styles.infoText}>
              Solo podés subir el certificado de defunción de una persona que ya te designó
              como heredero y cuya invitación ya aceptaste.
            </Text>
          </View>

          {titularesElegibles.length === 0 ? (
            <View style={styles.emptyCard}>
              <Text style={styles.emptyText}>
                {hayAceptadasSinTitularesElegibles
                  ? 'El fallecimiento de todos tus titulares ya fue confirmado: no hay ningún certificado pendiente por subir.'
                  : 'Todavía no aceptaste ninguna herencia. Primero aceptá una invitación desde “Mis herencias” para poder subir un certificado.'}
              </Text>
            </View>
          ) : (
            <>
              {/* SELECCIONAR TITULAR */}
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>¿De quién es el certificado?</Text>
                <TouchableOpacity
                  style={[styles.dropdownButton, showTitularDropdown && styles.dropdownButtonActive]}
                  onPress={() => setShowTitularDropdown(!showTitularDropdown)}
                >
                  <Text
                    style={[
                      styles.dropdownButtonText,
                      !titularSeleccionado && styles.placeholderText,
                    ]}
                  >
                    {titularSeleccionado ? titularSeleccionado.titularNombre : 'Seleccionar titular'}
                  </Text>
                  {showTitularDropdown ? (
                    <ChevronUp size={20} color="#2E7D32" />
                  ) : (
                    <ChevronDown size={20} color="#1a2e2e" />
                  )}
                </TouchableOpacity>

                {showTitularDropdown && (
                  <View style={styles.dropdownInlineList}>
                    {titularesElegibles.map((t) => (
                      <TouchableOpacity
                        key={t.titularId}
                        style={[
                          styles.dropdownInlineOption,
                          titularSeleccionado?.titularId === t.titularId && styles.dropdownInlineOptionSelected,
                        ]}
                        onPress={() => {
                          setTitularSeleccionado(t);
                          setShowTitularDropdown(false);
                        }}
                      >
                        <Text style={styles.dropdownInlineOptionText}>{t.titularNombre}</Text>
                      </TouchableOpacity>
                    ))}
                  </View>
                )}
              </View>

              {/* ADJUNTAR ARCHIVO */}
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Certificado de defunción</Text>
                <View style={styles.fileBoxBorder}>
                  <Paperclip color="#777" size={28} style={{ marginBottom: 6 }} />
                  <Text style={styles.fileText}>
                    {archivoSeleccionado ? archivoSeleccionado.name : 'Tocá para adjuntar (PDF, JPG o PNG)'}
                  </Text>
                  <TouchableOpacity style={styles.attachButton} onPress={handleElegirArchivo}>
                    <Text style={styles.attachButtonText}>
                      {archivoSeleccionado ? 'Cambiar archivo' : 'Adjuntar archivo'}
                    </Text>
                  </TouchableOpacity>
                </View>
              </View>

              <TouchableOpacity style={styles.saveButton} onPress={handleSubir} disabled={saving}>
                {saving ? (
                  <ActivityIndicator size="small" color="#FFFFFF" />
                ) : (
                  <Text style={styles.saveButtonText}>Enviar certificado</Text>
                )}
              </TouchableOpacity>
            </>
          )}
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#DAF8BD' },
  loadingContainer: { flex: 1, backgroundColor: '#DAF8BD', alignItems: 'center', justifyContent: 'center' },
  loadingText: { fontFamily: 'MPLUS2-Regular', fontSize: 16, color: '#23856C', marginTop: 12 },
  header: { paddingHorizontal: 20, paddingBottom: 20, borderBottomLeftRadius: 20, borderBottomRightRadius: 20 },
  headerContent: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  backButton: { padding: 4 },
  headerTitle: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 20, textAlign: 'center' },
  scrollContent: { padding: 24, paddingBottom: 40 },
  centeredWrapper: { width: '100%', maxWidth: 600, alignSelf: 'center', gap: 20 },
  infoBox: {
    backgroundColor: '#C5E2D0',
    borderRadius: 12,
    borderWidth: 1.2,
    borderColor: '#A1CBB2',
    padding: 16,
    flexDirection: 'row',
    gap: 10,
    alignItems: 'flex-start',
  },
  infoText: { flex: 1, fontFamily: 'MPLUS2-Regular', fontSize: 13, color: '#0E4A4C', lineHeight: 18 },
  emptyCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 24,
    alignItems: 'center',
  },
  emptyText: { fontFamily: 'MPLUS2-Regular', fontSize: 14, color: '#8A9E95', textAlign: 'center', lineHeight: 20 },
  inputGroup: { gap: 8 },
  inputLabel: { fontFamily: 'MPLUS2-Bold', fontSize: 14, color: '#5E746A' },
  placeholderText: { color: '#8A9E95' },
  dropdownButton: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    height: 48,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  dropdownButtonActive: { borderColor: '#2E7D32', borderWidth: 1.5 },
  dropdownButtonText: { fontFamily: 'MPLUS2-Bold', fontSize: 15, color: '#000000' },
  dropdownInlineList: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 10,
    marginTop: 6,
    gap: 2,
  },
  dropdownInlineOption: { paddingVertical: 10, paddingHorizontal: 12, borderRadius: 8 },
  dropdownInlineOptionSelected: { backgroundColor: '#DAF8BD' },
  dropdownInlineOptionText: { fontFamily: 'MPLUS2-Regular', fontSize: 15, color: '#333333' },
  fileBoxBorder: {
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    borderStyle: 'dashed',
    borderRadius: 8,
    padding: 20,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#FAFAFA',
  },
  fileText: { fontFamily: 'MPLUS2-Regular', fontSize: 13, color: '#777', marginBottom: 12, textAlign: 'center' },
  attachButton: { backgroundColor: '#D97706', paddingHorizontal: 20, paddingVertical: 10, borderRadius: 6 },
  attachButtonText: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 14 },
  saveButton: {
    backgroundColor: '#39C55C',
    borderRadius: 12,
    height: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  saveButtonText: { color: '#FFFFFF', fontFamily: 'MPLUS2-Bold', fontSize: 16 },
});
