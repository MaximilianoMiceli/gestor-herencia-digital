// No existe una entidad "Beneficiario" propia en el backend: una persona lo es porque
// tiene AsignacionHerencia sobre mis activos, y el listado se arma agregando por email.
import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  FlatList,
  RefreshControl,
  Platform,
} from 'react-native';
import { useRouter, useLocalSearchParams, useFocusEffect } from 'expo-router';
import { ArrowLeft, ArrowRight, HelpCircle, Info, Search } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../../context/AuthContext';
import { AssetsService, BeneficiarioResumen } from '../../services/assets.service';

export default function BeneficiariosScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();

  const { deleted } = useLocalSearchParams();
  const [showSuccessBanner, setShowSuccessBanner] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [beneficiarios, setBeneficiarios] = useState<BeneficiarioResumen[]>([]);
  const [searchQuery, setSearchQuery] = useState('');

  const fetchBeneficiarios = async () => {
    if (!token) return;
    try {
      const data = await AssetsService.getMisBeneficiarios();
      setBeneficiarios(data);
    } catch (err: any) {
      console.log('Error loading beneficiaries:', err.message);
      if (
        err.message.includes('401') ||
        err.message.includes('autorización') ||
        err.message.includes('token')
      ) {
        router.replace('/(auth)/welcome');
      }
    } finally {
      setLoading(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      fetchBeneficiarios();
    }, [token])
  );

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchBeneficiarios();
    setRefreshing(false);
  };

  const beneficiariosFiltrados = searchQuery.trim().length > 0
    ? beneficiarios.filter((b) => b.email.toLowerCase().includes(searchQuery.trim().toLowerCase()))
    : beneficiarios;

  useEffect(() => {
    if (deleted === 'true') {
      setShowSuccessBanner(true);
      const timer = setTimeout(() => {
        setShowSuccessBanner(false);
        router.setParams({ deleted: undefined });
      }, 4000);
      return () => clearTimeout(timer);
    }
  }, [deleted]);

  // estado: 1 = Pendiente, 2 = Aceptado/Verificado, 3 = Rechazado.
  const obtenerEstadoBeneficiario = (item: BeneficiarioResumen): 'Verificado' | 'Pendiente' | 'Rechazado' => {
    if (item.estado === 2) return 'Verificado';
    if (item.estado === 3) return 'Rechazado';
    return 'Pendiente';
  };

  const handleVerDetalles = (item: BeneficiarioResumen) => {
    router.push({
      pathname: '/detalle-beneficiario',
      params: { email: item.email },
    });
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#23856C" />
        <Text style={styles.loadingText}>Cargando beneficiarios...</Text>
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
          <TouchableOpacity onPress={() => router.replace('/')} style={styles.backButton}>
            <ArrowLeft size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Mis beneficiarios</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <View style={styles.content}>
        {showSuccessBanner && (
          <View style={styles.successBanner}>
            <Info size={18} color="#0E4A4C" />
            <Text style={styles.successBannerText}>Beneficiario eliminado con éxito</Text>
          </View>
        )}

        {beneficiarios.length > 0 && (
          <View style={styles.searchContainer}>
            <Search size={18} color="#8A9E95" style={styles.searchIcon} />
            <TextInput
              style={styles.searchInput}
              placeholder="Buscar por email"
              placeholderTextColor="#8A9E95"
              value={searchQuery}
              onChangeText={setSearchQuery}
              autoCapitalize="none"
            />
          </View>
        )}

        {beneficiarios.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin beneficiarios aún</Text>
            <Text style={styles.emptyStateText}>
              Los beneficiarios se agregan al crear un activo y asignárselo. Presioná el botón de abajo
              para registrar tu primer activo y designar a su heredero.
            </Text>
          </View>
        ) : beneficiariosFiltrados.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin resultados</Text>
            <Text style={styles.emptyStateText}>Ningún beneficiario coincide con la búsqueda.</Text>
          </View>
        ) : (
          <FlatList
            data={beneficiariosFiltrados}
            keyExtractor={(item) => item.email}
            contentContainerStyle={styles.listContent}
            showsVerticalScrollIndicator={false}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#23856C']} tintColor="#23856C" />
            }
            renderItem={({ item }) => {
              const estado = obtenerEstadoBeneficiario(item);
              return (
                <View style={styles.beneficiaryCard}>
                  <View style={styles.cardHeaderRow}>
                    <View style={styles.cardHeaderLeft}>
                      <Text style={styles.cardName}>{item.email}</Text>
                      <Text style={styles.cardRelation}>
                        {item.asignaciones.length === 1
                          ? '1 activo asignado'
                          : `${item.asignaciones.length} activos asignados`}
                      </Text>
                    </View>
                    <Text style={[
                      styles.cardStatus,
                      estado === 'Verificado' && styles.statusVerified,
                      estado === 'Pendiente' && styles.statusPending,
                      estado === 'Rechazado' && styles.statusRejected
                    ]}>
                      {estado}
                    </Text>
                  </View>

                  <TouchableOpacity
                    style={styles.detailsButton}
                    onPress={() => handleVerDetalles(item)}
                  >
                    <Text style={styles.detailsButtonText}>Detalles</Text>
                    <ArrowRight size={18} color="#FFFFFF" style={styles.detailsIcon} />
                  </TouchableOpacity>
                </View>
              );
            }}
          />
        )}

        <TouchableOpacity
          style={styles.addBeneficiaryButton}
          onPress={() => router.push('/nuevo-activo')}
        >
          <Text style={styles.addBeneficiaryText}>+ Agregar beneficiario</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#DAF8BD',
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
  content: {
    flex: 1,
    padding: 20,
    justifyContent: 'space-between',
  },
  searchContainer: {
    backgroundColor: '#E6EAE7',
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    height: 48,
    marginBottom: 16,
  },
  searchIcon: {
    marginRight: 8,
  },
  searchInput: {
    flex: 1,
    color: '#1a2e2e',
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    padding: 0,
  },
  listContent: {
    gap: 16,
    paddingBottom: 20,
  },
  beneficiaryCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 20,
    gap: 16,
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.08,
        shadowRadius: 6,
      },
      android: {
        elevation: 3,
      },
    }),
  },
  cardHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  cardHeaderLeft: {
    flex: 1,
  },
  cardName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 17,
    color: '#1a2e2e',
    marginBottom: 4,
  },
  cardRelation: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  cardStatus: {
    fontSize: 13,
    fontFamily: 'MPLUS2-Bold',
  },
  statusVerified: {
    color: '#2E7D32',
  },
  statusPending: {
    color: '#E2A53C',
  },
  statusRejected: {
    color: '#D32F2F',
  },
  detailsButton: {
    backgroundColor: '#7EA49E',
    borderRadius: 12,
    height: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
  },
  detailsButtonText: {
    color: '#FFFFFF',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 15,
  },
  detailsIcon: {
    marginLeft: 6,
  },
  addBeneficiaryButton: {
    backgroundColor: '#FFFFFF',
    height: 52,
    borderRadius: 16,
    borderWidth: 1.5,
    borderColor: '#2E7D32',
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    marginTop: 10,
    marginBottom: Platform.OS === 'ios' ? 10 : 0,
  },
  addBeneficiaryText: {
    color: '#2E7D32',
    fontSize: 16,
    fontFamily: 'MPLUS2-Bold',
  },
  successBanner: {
    backgroundColor: '#FFFFFF',
    borderWidth: 1.5,
    borderColor: '#C1E3A4',
    borderRadius: 20,
    paddingVertical: 10,
    paddingHorizontal: 16,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 16,
    alignSelf: 'center',
    shadowColor: '#1a2e2e',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
    width: '100%',
    justifyContent: 'center',
  },
  successBannerText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 13,
    color: '#1a2e2e',
  },
  emptyStateCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    paddingVertical: 40,
    paddingHorizontal: 24,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 16,
    marginTop: 40,
  },
  emptyStateTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 18,
    color: '#1a2e2e',
  },
  emptyStateText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#8A9E95',
    textAlign: 'center',
    lineHeight: 20,
  },
});
