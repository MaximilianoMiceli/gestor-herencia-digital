import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  Modal,
  Platform,
} from 'react-native';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { Mail } from 'lucide-react-native';
import { useAuth } from '../context/AuthContext';
import { InvitacionesService, InvitacionDTO } from '../services/invitaciones.service';
import GradientText from '../components/GradientText';

export default function InvitacionScreen() {
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id?: string }>();
  const { token } = useAuth();

  const [loading, setLoading] = useState(true);
  const [invitacion, setInvitacion] = useState<InvitacionDTO | null>(null);
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [processing, setProcessing] = useState(false);

  useEffect(() => {
    if (!id) {
      Alert.alert('Error', 'ID de invitación no provisto en el enlace.');
      router.replace('/(auth)/welcome');
      return;
    }

    const fetchInvitacion = async () => {
      try {
        const data = await InvitacionesService.obtener(id);
        setInvitacion(data);
      } catch (err: any) {
        Alert.alert('Error al cargar', err.message || 'La invitación no existe, ha sido vencida o fue revocada.');
        router.replace('/(auth)/welcome');
      } finally {
        setLoading(false);
      }
    };

    fetchInvitacion();
  }, [id]);

  const handleAceptar = async () => {
    if (token && id) {
      setProcessing(true);
      try {
        await InvitacionesService.procesar(id, 'aceptar');

        Alert.alert('Éxito', '¡Invitación aceptada con éxito!', [
          { text: 'Ir al Inicio', onPress: () => router.replace('/') },
        ]);
      } catch (err: any) {
        Alert.alert('Error', err.message);
      } finally {
        setProcessing(false);
      }
    } else {
      setShowAuthModal(true);
    }
  };

  const handleRechazar = () => {
    Alert.alert(
      'Rechazar Invitación',
      '¿Estás seguro de que deseas rechazar esta designación como beneficiario? Esta acción eliminará el vínculo de forma permanente.',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Rechazar',
          style: 'destructive',
          onPress: async () => {
            if (!id) return;
            setProcessing(true);
            try {
              await InvitacionesService.procesar(id, 'rechazar');

              Alert.alert('Invitación rechazada', 'Se ha eliminado la invitación con éxito.', [
                { text: 'OK', onPress: () => router.replace('/(auth)/welcome') },
              ]);
            } catch (err: any) {
              Alert.alert('Error', err.message);
            } finally {
              setProcessing(false);
            }
          },
        },
      ]
    );
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando invitación...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.logoContainer}>
        <Text style={styles.titlePrefix}>Gestor de</Text>
        <GradientText text="Herencia Digital" style={styles.titleGradient} />
      </View>

      <View style={styles.invitationCard}>
        <View style={styles.iconCircle}>
          <Mail size={44} color="#1a2e2e" strokeWidth={1.5} />
        </View>

        <Text style={styles.cardHeader}>Fuiste invitado como heredero</Text>
        
        <Text style={styles.cardBody}>
          <Text style={{ fontFamily: 'MPLUS2-Bold' }}>{invitacion?.emisorNombre}</Text> te designó como beneficiario de sus activos digitales.
        </Text>

        <View style={styles.divider} />

        <Text style={styles.ownerName}>{invitacion?.emisorNombre}</Text>
      </View>

      <View style={styles.buttonContainer}>
        {processing ? (
          <ActivityIndicator size="large" color="#23856C" style={{ marginVertical: 20 }} />
        ) : (
          <>
            <TouchableOpacity style={styles.acceptButton} onPress={handleAceptar}>
              <Text style={styles.acceptButtonText}>Aceptar</Text>
            </TouchableOpacity>

            <TouchableOpacity style={styles.rejectButton} onPress={handleRechazar}>
              <Text style={styles.rejectButtonText}>Rechazar</Text>
            </TouchableOpacity>
          </>
        )}
      </View>

      <Modal
        visible={showAuthModal}
        transparent
        animationType="fade"
        onRequestClose={() => setShowAuthModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>¿Ya tenes cuenta?</Text>

            <TouchableOpacity
              style={styles.modalLoginButton}
              onPress={() => {
                setShowAuthModal(false);
                router.push({
                  pathname: '/(auth)/login',
                  params: { acceptInvitationId: id },
                });
              }}
            >
              <Text style={styles.modalLoginText}>Si, tengo una</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={styles.modalRegisterButton}
              onPress={() => {
                setShowAuthModal(false);
                router.push({
                  pathname: '/(auth)/register',
                  params: { 
                    email: invitacion?.beneficiarioEmail,
                    acceptInvitationId: id 
                  },
                });
              }}
            >
              <Text style={styles.modalRegisterText}>Crear cuenta</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
    paddingHorizontal: 24,
    justifyContent: 'space-between',
    paddingTop: 80,
    paddingBottom: 60,
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
  logoContainer: {
    alignItems: 'center',
    marginTop: 20,
  },
  titlePrefix: {
    fontSize: 18,
    fontFamily: 'MPLUS2-Regular',
    color: '#DF5173',
    marginBottom: -5,
  },
  titleGradient: {
    fontSize: 34,
    fontFamily: 'MPLUS2-Regular',
    borderTopWidth: 1,
    borderTopColor: '#C1E3A4',
    paddingTop: 10,
    marginTop: 10,
  },
  invitationCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    paddingVertical: 36,
    paddingHorizontal: 24,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#C1E3A4',
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 6 },
        shadowOpacity: 0.1,
        shadowRadius: 10,
      },
      android: {
        elevation: 4,
      },
    }),
  },
  iconCircle: {
    width: 64,
    height: 64,
    borderRadius: 32,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
  },
  cardHeader: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    color: '#1a2e2e',
    textAlign: 'center',
    marginBottom: 16,
  },
  cardBody: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 15,
    color: '#445E51',
    textAlign: 'center',
    lineHeight: 22,
    marginBottom: 24,
  },
  divider: {
    height: 1,
    width: '100%',
    backgroundColor: '#EEFDE2',
    marginBottom: 20,
  },
  ownerName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 17,
    color: '#1a2e2e',
    textAlign: 'center',
    marginBottom: 4,
  },
  buttonContainer: {
    width: '100%',
    gap: 16,
  },
  acceptButton: {
    backgroundColor: '#2E7D32',
    height: 52,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    ...Platform.select({
      ios: {
        shadowColor: '#2E7D32',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.2,
        shadowRadius: 6,
      },
      android: {
        elevation: 2,
      },
    }),
  },
  acceptButtonText: {
    color: '#FFFFFF',
    fontSize: 18,
    fontFamily: 'MPLUS2-Bold',
  },
  rejectButton: {
    backgroundColor: '#FFFFFF',
    height: 52,
    borderRadius: 12,
    borderWidth: 1.5,
    borderColor: '#CCCCCC',
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
  },
  rejectButtonText: {
    color: '#8A9E95',
    fontSize: 18,
    fontFamily: 'MPLUS2-Bold',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(26, 46, 46, 0.45)',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 36,
  },
  modalContent: {
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    paddingVertical: 28,
    paddingHorizontal: 24,
    alignItems: 'center',
    width: '100%',
    shadowColor: '#1a2e2e',
    shadowOffset: { width: 0, height: 10 },
    shadowOpacity: 0.25,
    shadowRadius: 15,
    elevation: 10,
  },
  modalTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 20,
    color: '#1a2e2e',
    textAlign: 'center',
    marginBottom: 24,
  },
  modalLoginButton: {
    backgroundColor: '#02213D',
    height: 48,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    marginBottom: 16,
  },
  modalLoginText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontFamily: 'MPLUS2-Bold',
  },
  modalRegisterButton: {
    backgroundColor: '#2E7D32',
    height: 48,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
  },
  modalRegisterText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontFamily: 'MPLUS2-Bold',
  },
});
