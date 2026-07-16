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
import { ArrowLeft, Search, ChevronDown, ChevronLeft, ChevronRight, HelpCircle, Info } from 'lucide-react-native';
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

const ACTIVOS_POR_PAGINA = 5;

export default function ActivosScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { token } = useAuth();
  const { deleted } = useLocalSearchParams();

  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [cambiandoPagina, setCambiandoPagina] = useState(false);
  const [activos, setActivos] = useState<ActivoItem[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [busquedaFiltrada, setBusquedaFiltrada] = useState('');
  const [selectedType, setSelectedType] = useState<number | null>(null);
  const [pagina, setPagina] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(1);
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

  // Debounce: evita pedir una página nueva al backend en cada tecla escrita.
  // El cambio de búsqueda invalida la página en la que se estaba parado, por eso se resetea acá.
  useEffect(() => {
    const timer = setTimeout(() => {
      setBusquedaFiltrada(searchQuery.trim());
      setPagina(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const fetchActivos = async (paginaAPedir: number) => {
    if (!token) return;
    try {
      const resultado = await AssetsService.getAssetsPaginated(
        paginaAPedir,
        ACTIVOS_POR_PAGINA,
        selectedType,
        busquedaFiltrada
      );

      // La página quedó vacía (por ej. se borró el último activo de esta página): retrocede una.
      if (resultado.items.length === 0 && paginaAPedir > 1 && resultado.totalRegistros > 0) {
        setPagina(paginaAPedir - 1);
        return;
      }

      setActivos(resultado.items);
      setTotalPaginas(Math.max(1, resultado.totalPaginas));
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
      setCambiandoPagina(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      fetchActivos(pagina);
    }, [token, pagina, selectedType, busquedaFiltrada])
  );

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchActivos(pagina);
    setRefreshing(false);
  };

  const irAPagina = (nuevaPagina: number) => {
    if (nuevaPagina < 1 || nuevaPagina > totalPaginas || nuevaPagina === pagina) return;
    setCambiandoPagina(true);
    setPagina(nuevaPagina);
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
                  setPagina(1);
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
            ListFooterComponent={
              totalPaginas > 1 ? (
                <View style={styles.paginationContainer}>
                  <TouchableOpacity
                    style={[styles.paginationButton, pagina <= 1 && styles.paginationButtonDisabled]}
                    onPress={() => irAPagina(pagina - 1)}
                    disabled={pagina <= 1 || cambiandoPagina}
                  >
                    <ChevronLeft size={20} color={pagina <= 1 ? '#B7C7BE' : '#1a2e2e'} />
                  </TouchableOpacity>

                  {cambiandoPagina ? (
                    <ActivityIndicator size="small" color="#23856C" />
                  ) : (
                    <Text style={styles.paginationText}>
                      Página {pagina} de {totalPaginas}
                    </Text>
                  )}

                  <TouchableOpacity
                    style={[styles.paginationButton, pagina >= totalPaginas && styles.paginationButtonDisabled]}
                    onPress={() => irAPagina(pagina + 1)}
                    disabled={pagina >= totalPaginas || cambiandoPagina}
                  >
                    <ChevronRight size={20} color={pagina >= totalPaginas ? '#B7C7BE' : '#1a2e2e'} />
                  </TouchableOpacity>
                </View>
              ) : null
            }
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
  paginationContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 16,
    paddingTop: 8,
  },
  paginationButton: {
    width: 36,
    height: 36,
    borderRadius: 10,
    backgroundColor: '#E6EAE7',
    alignItems: 'center',
    justifyContent: 'center',
  },
  paginationButtonDisabled: {
    opacity: 0.5,
  },
  paginationText: {
    fontFamily: 'MPLUS2-Bold',
    fontSize: 14,
    color: '#1a2e2e',
    minWidth: 110,
    textAlign: 'center',
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
