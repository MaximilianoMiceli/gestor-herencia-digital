import { Href, Link } from 'expo-router';
import { openBrowserAsync, WebBrowserPresentationStyle } from 'expo-web-browser';
import { type ComponentProps } from 'react';

/**
 * Propiedades para el componente ExternalLink, omitiendo el href estándar de Link.
 */
type Props = Omit<ComponentProps<typeof Link>, 'href'> & { href: Href & string };

/**
 * Componente que gestiona enlaces a sitios web externos de forma segura.
 * En plataformas nativas, abre el enlace en un navegador dentro de la aplicación para no perder al usuario.
 * En la web, se abre en una pestaña nueva utilizando target="_blank".
 */
export function ExternalLink({ href, ...rest }: Props) {
  return (
    <Link
      target="_blank"
      {...rest}
      href={href}
      onPress={async (event) => {
        if (process.env.EXPO_OS !== 'web') {
          // Prevent the default behavior of linking to the default browser on native.
          event.preventDefault();
          // Open the link in an in-app browser.
          await openBrowserAsync(href, {
            presentationStyle: WebBrowserPresentationStyle.AUTOMATIC,
          });
        }
      }}
    />
  );
}
