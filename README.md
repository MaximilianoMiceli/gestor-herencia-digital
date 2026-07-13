# Gestor de Herencia Digital

Aplicación para la gestión segura de activos digitales (cuentas bancarias, billeteras cripto, redes sociales, correos electrónicos, dominios, etc.) y su transferencia ordenada a beneficiarios designados en caso de fallecimiento del titular.

El sistema combina un mecanismo de **verificación de vida** ("proof of life") con un flujo de **certificado de defunción**: mientras el titular confirma periódicamente que sigue activo, sus activos permanecen bajo su control; si deja de responder a los recordatorios durante el plazo configurado, o se carga y valida un certificado de defunción, los activos asignados quedan disponibles para que cada beneficiario los reclame.

## Contexto académico

Este proyecto fue desarrollado como **Proyecto Integrador Final** de la materia Programación 3 en la carrera Tecnicatura Universitaria en Programación (UTN FRRe). Su objetivo es demostrar la aplicación práctica de una arquitectura en capas, persistencia con ORM, autenticación/autorización, y consumo de una API REST desde un cliente móvil multiplataforma.

## Stack tecnológico

**Backend**
- C# sobre **.NET 10** (ASP.NET Core Web API)
- **Entity Framework Core** como ORM
- **SQLite** como motor de base de datos
- Autenticación con **JWT Bearer** (+ segundo factor por email)
- **Swagger / OpenAPI** (Swashbuckle) para documentación interactiva de la API

**Frontend**
- **React Native** con **Expo** (Expo Router, ruteo basado en archivos)
- **TypeScript**
- **Axios** para el consumo de la API

## Arquitectura

El backend está organizado en **3 capas**, cada una en su propio proyecto de la solución:

| Proyecto | Responsabilidad |
|---|---|
| `Herencia.Api` | Controllers HTTP, autenticación JWT, configuración de la aplicación (`Program.cs`), jobs en background. No conoce detalles de persistencia. |
| `Herencia.Business` | Lógica de negocio y validaciones (`Services`), contratos (`Interfaces`), objetos de transferencia de datos (`Dtos`) y excepciones de dominio (`Exceptions`). No conoce EF Core ni SQLite directamente: solo interfaces de repositorio. |
| `Herencia.Data` | Modelos de EF Core (`Models`), `AppDbContext`, repositorios (`Repositories`) y migraciones (`Migrations`). Único lugar que sabe que la base de datos es SQLite. |

Cada capa solo conoce a la inferior a través de **interfaces**, inyectadas por contenedor de Dependencias de ASP.NET Core — ni `Herencia.Api` ni `Herencia.Business` instancian repositorios o el `DbContext` directamente.

El frontend sigue una separación similar: pantallas en `src/app` (ruteo de Expo Router), componentes reutilizables en `src/components`, llamadas HTTP encapsuladas en `src/services`, y estado de sesión global en `src/context/AuthContext.tsx`.

## Estructura de roles

El sistema distingue dos niveles:

- **A nivel de cuenta** (`RolUsuario`): `Usuario` (rol por defecto de cualquier cuenta registrada) y `Administrador` (acceso a operaciones globales, como listar todos los usuarios).
- **A nivel de relación entre activos** (roles contextuales, no un campo fijo en la base de datos):
  - **Otorgante**: el titular de un activo digital, que lo asigna a uno o más beneficiarios con un porcentaje y una condición de liberación.
  - **Beneficiario**: la persona designada para recibir un activo (o una porción de él) una vez verificado el fallecimiento del otorgante.

Una misma cuenta puede ser Otorgante de sus propios activos y, al mismo tiempo, Beneficiario de activos que otra persona le haya asignado.

## Guía de instalación y ejecución

### Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20+ y npm
- La app [Expo Go](https://expo.dev/go) (opcional, para probar en un dispositivo físico) o un emulador Android/iOS

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd gestor-herencia-digital
```

### 2. Backend (`GestorHerencia-Backend`)

```bash
cd GestorHerencia-Backend/Herencia.Api
dotnet restore
```

**Configurar el secreto del Token JWT** (nunca se commitea en `appsettings.json`, que lo deja vacío a propósito). El proyecto ya tiene un `UserSecretsId` configurado, así que alcanza con:

```bash
dotnet user-secrets set "AppSettings:Token" "una-clave-secreta-larga-y-aleatoria-de-al-menos-32-caracteres"
```

**Levantar la API:**

```bash
dotnet run
```

Al iniciar, la aplicación aplica automáticamente las migraciones pendientes (`context.Database.Migrate()` en `Program.cs`) y — al tratarse de la primera ejecución — puebla la base de datos SQLite con los seeders de prueba (3 usuarios, activos digitales y asignaciones de herencia de ejemplo). **No hace falta correr `dotnet ef database update` a mano.**

La API queda disponible en:
- `http://localhost:5055` (HTTP)
- `https://localhost:7184` (HTTPS)
- Documentación interactiva Swagger: `http://localhost:5055/swagger`

### 3. Frontend (`GestorHerencia-Frontend`)

```bash
cd GestorHerencia-Frontend
npm install
```

**Configurar la URL del backend.** Crear un archivo `.env` (o `.env.local`) en la raíz de `GestorHerencia-Frontend` apuntando a la API:

```
EXPO_PUBLIC_API_BASE_URL=http://localhost:5055/api
```

> Si vas a probar desde un dispositivo físico con Expo Go (no un emulador), reemplazá `localhost` por la IP de red local de la máquina donde corre el backend (ej. `http://192.168.0.10:5055/api`).

**Levantar la app:**

```bash
npx expo start
```

Desde la terminal de Expo se puede elegir abrir la app en modo web (`w`), Android (`a`), iOS (`i`), o escaneando el QR con Expo Go.

### 4. Usuarios de prueba (seeders)

| Nombre | Email | Contraseña | Rol |
|---|---|---|---|
| Maximiliano Miceli | `maximiceli@hotmail.com.ar` | `Test123456!` | Administrador |
| Ana Torres | `ana.torres@example.com` | `Test123456!` | Usuario |
| Carlos Sosa | `carlos.sosa@example.com` | `Test123456!` | Usuario |

Estos tres usuarios ya tienen activos digitales y asignaciones de herencia cargadas entre sí, pensadas para poder probar el flujo completo (Otorgante → Beneficiario →, a su vez, Otorgante de un tercero) sin necesidad de cargar datos manualmente.
