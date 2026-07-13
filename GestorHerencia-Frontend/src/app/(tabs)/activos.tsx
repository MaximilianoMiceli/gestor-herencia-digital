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
import { ArrowLeft, Search, ChevronDown, HelpCircle, Info } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '../../context/AuthContext';
import { AssetsService } from '../../services/assets.service';

interface ActivoItem {
  id: number;
  nombre: string;
  tipo: number;
  descripcion: string;
}

export default function ActivosScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();
  const { deleted } = useLocalSearchParams();

  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [activos, setActivos] = useState<ActivoItem[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState<number | null>(null);
  const [showFilterDropdown, setShowFilterDropdown] = useState(false);
  const [showSuccessBanner, setShowSuccessBanner] = useState(false);

  const tiposActivo = [
    { label: 'Todos', value: null },
    { label: 'Cuenta bancaria', value: 0 },
    { label: 'Red social', value: 1 },
    { label: 'Cripto', value: 2 },
    { label: 'Correo electrónico', value: 3 },
    { label: 'Archivo / Otro', value: 4 },
  ];

  const fetchActivos = async () => {
    if (!token) return;
    try {
      // El backend no expone búsqueda por query params: se trae la lista completa y se filtra acá.
      const fullList = await AssetsService.getAssets();
      let filtered = fullList;
      if (searchQuery.trim().length > 0) {
        const query = searchQuery.toLowerCase();
        filtered = filtered.filter((a) => a.nombre.toLowerCase().includes(query));
      }
      if (selectedType !== null) {
        filtered = filtered.filter((a) => a.tipo === selectedType);
      }

      setActivos(filtered);
    } catch (err: any) {
      console.log('Error loading assets:', err.message);
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
      fetchActivos();
    }, [token, searchQuery, selectedType])
  );

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchActivos();
    setRefreshing(false);
  };

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

  const mapearTipoActivo = (tipoVal: number): string => {
    switch (tipoVal) {
      case 0: return 'Cuenta bancaria';
      case 1: return 'Red social';
      case 2: return 'Cripto';
      case 3: return 'Correo electrónico';
      default: return 'Archivo / Otro';
    }
  };

  const handleVerDetalles = (item: ActivoItem) => {
    router.push({
      pathname: '/editar-activo',
      params: { id: item.id.toString() },
    });
  };

  const getFiltroLabel = () => {
    if (selectedType === null) return 'Filtrar';
    const found = tiposActivo.find(t => t.value === selectedType);
    return found ? found.label : 'Filtrar';
  };

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
          <Text style={styles.headerTitle}>Activos guardados</Text>
          <View style={{ width: 24 }} />
        </View>
      </LinearGradient>

      <View style={styles.content}>
        {showSuccessBanner && (
          <View style={styles.successBanner}>
            <Info size={18} color="#0E4A4C" />
            <Text style={styles.successBannerText}>Activo eliminado con éxito</Text>
          </View>
        )}

        <View style={styles.searchFilterRow}>
          <View style={styles.searchContainer}>
            <Search size={18} color="#8A9E95" style={styles.searchIcon} />
            <TextInput
              style={styles.searchInput}
              placeholder="Buscar"
              placeholderTextColor="#8A9E95"
              value={searchQuery}
              onChangeText={(text) => setSearchQuery(text)}
            />
          </View>

          <TouchableOpacity
            style={styles.filterButton}
            onPress={() => setShowFilterDropdown(!showFilterDropdown)}
          >
            <Text style={styles.filterButtonText} numberOfLines={1}>
              {getFiltroLabel()}
            </Text>
            <ChevronDown size={16} color="#1a2e2e" />
          </TouchableOpacity>
        </View>

        {showFilterDropdown && (
          <View style={styles.dropdownContainer}>
            {tiposActivo.map((tipo) => (
              <TouchableOpacity
                key={tipo.label}
                style={[
                  styles.dropdownOption,
                  selectedType === tipo.value && styles.dropdownOptionSelected,
                ]}
                onPress={() => {
                  setSelectedType(tipo.value);
                  setShowFilterDropdown(false);
                }}
              >
                <Text
                  style={[
                    styles.dropdownOptionText,
                    selectedType === tipo.value && styles.dropdownOptionTextSelected,
                  ]}
                >
                  {tipo.label}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        )}

        {loading ? (
          <View style={styles.loadingWrapper}>
            <ActivityIndicator size="large" color="#23856C" />
            <Text style={styles.loadingText}>Cargando activos...</Text>
          </View>
        ) : activos.length === 0 ? (
          <View style={styles.emptyStateCard}>
            <HelpCircle size={48} color="#8A9E95" strokeWidth={1.5} />
            <Text style={styles.emptyStateTitle}>Sin activos guardados</Text>
            <Text style={styles.emptyStateSubtitle}>
              No se encontraron activos que coincidan con la búsqueda.
            </Text>
          </View>
        ) : (
          <FlatList
            data={activos}
            keyExtractor={(item) => item.id.toString()}
            showsVerticalScrollIndicator={false}
            contentContainerStyle={styles.listContent}
            refreshControl={
              <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#23856C']} tintColor="#23856C" />
            }
            renderItem={({ item }) => (
              <View style={styles.assetCard}>
                <View style={styles.assetInfo}>
                  <Text style={styles.assetName}>{item.nombre}</Text>
                  <Text style={styles.assetType}>{mapearTipoActivo(item.tipo)}</Text>
                </View>

                <TouchableOpacity
                  style={styles.detailsButton}
                  onPress={() => handleVerDetalles(item)}
                >
                  <Text style={styles.detailsButtonText}>Detalles</Text>
                </TouchableOpacity>
              </View>
            )}
          />
        )}
      </View>
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
  content: {
    flex: 1,
    padding: 20,
  },
  searchFilterRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 16,
    zIndex: 10,
  },
  searchContainer: {
    flex: 1,
    backgroundColor: '#E6EAE7',
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    height: 48,
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
  filterButton: {
    backgroundColor: '#E6EAE7',
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    height: 48,
    width: 120,
    gap: 4,
  },
  filterButtonText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1a2e2e',
    flex: 1,
  },
  dropdownContainer: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 8,
    gap: 4,
    marginBottom: 16,
    ...Platform.select({
      ios: {
        shadowColor: '#1a2e2e',
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.1,
        shadowRadius: 6,
      },
      android: {
        elevation: 3,
      },
    }),
  },
  dropdownOption: {
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  dropdownOptionSelected: {
    backgroundColor: '#DAF8BD',
  },
  dropdownOptionText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#1a2e2e',
  },
  dropdownOptionTextSelected: {
    fontFamily: 'MPLUS2-Bold',
    color: '#2E7D32',
  },
  loadingWrapper: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 12,
  },
  loadingText: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 14,
    color: '#23856C',
  },
  listContent: {
    gap: 12,
    paddingBottom: 20,
  },
  assetCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#C1E3A4',
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  assetInfo: {
    flex: 1,
    gap: 4,
  },
  assetName: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
  },
  assetType: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
  },
  detailsButton: {
    borderWidth: 1.5,
    borderColor: '#005B9A',
    borderRadius: 12,
    paddingVertical: 6,
    paddingHorizontal: 16,
  },
  detailsButtonText: {
    color: '#005B9A',
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
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
    padding: 32,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    marginTop: 20,
  },
  emptyStateTitle: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 16,
    color: '#1a2e2e',
    marginTop: 8,
  },
  emptyStateSubtitle: {
    fontFamily: 'MPLUS2-Regular',
    fontSize: 13,
    color: '#8A9E95',
    textAlign: 'center',
    lineHeight: 18,
  },
});
