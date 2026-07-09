import { Platform } from 'react-native';

// La URL base cambiará dependiendo del entorno.
// En Android el emulador accede a localhost a través de 10.0.2.2
export const API_BASE_URL = __DEV__
  ? 'http://192.168.100.9:5055/api'
  : 'https://tu-dominio.com/api';
