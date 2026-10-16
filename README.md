# Sistema de Gestión de Mantenimiento Interactivo en Tiempo Real (Coworking Spaces)

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)
![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-Multi--platform-512BD4?logo=dotnet)
![ASP.NET Core Web API](https://img.shields.io/badge/ASP.NET%20Core-Web%20API%20%2B%20SignalR-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver)

---

## 📌 1. Descripción del Proyecto

El **Sistema de Gestión de Mantenimiento Interactivo para Espacios de Coworking** es una solución integral diseñada para optimizar la operación y el mantenimiento de la infraestructura sanitaria (baterías de baños) en edificios de trabajo compartido. 

Resuelve la ineficiencia de las rondas de aseo periódicas ciegas y la lentitud en la atención de averías mediante:
1. **Modelado Gráfico 2D:** Un editor interactivo en cuadrícula (Grid System bidimensional) donde los administradores diagraman la distribución física de cada baño (inodoros, lavamanos, secadores, dispensadores).
2. **Generación de Códigos QR:** Cada baño cuenta con un código QR único que los coworkers pueden escanear sin necesidad de instalar aplicaciones ni iniciar sesión.
3. **Reporte Ciudadano Interactivo:** Al escanear el QR, el usuario visualiza el plano exacto del baño, toca el activo dañado y emite el reporte en segundos.
4. **Despacho y Telemetría en Tiempo Real (SignalR):** Las alertas se transmiten de forma instantánea mediante WebSockets y se filtran por grupos de edificio, llegando exclusivamente a los operarios de limpieza asignados a esa sede a través de una aplicación móvil nativa.

---

## 👥 2. Integrantes y Autoría

* **Autor / Desarrollador Principal:** Deybi Montenegro (`deibymontenegrob@gmail.com`)
* **Repositorio:** Coworking Maintenance Project
* **Versión del Sistema:** 2.0 (Lanzamiento .NET 10 Multi-rol)

---

## 🛠️ 3. Stack Tecnológico

| Capa / Componente | Tecnología | Justificación Técnica |
| :--- | :--- | :--- |
| **Backend & APIs** | ASP.NET Core 10 Web API | Rendimiento extremo, soporte nativo AOT y arquitectura Vertical Slices / CQRS. |
| **Telemetría en Vivo** | ASP.NET Core SignalR + MessagePack | Comunicación full-duplex sobre WebSockets con serialización binaria ultraligera. |
| **Seguridad e Identidad** | ASP.NET Core Identity | Manejo de usuarios, roles (`SuperAdmin`, `Admin`) y tokens de portador opacos de alta seguridad. |
| **Base de Datos** | Microsoft SQL Server 2022 | RDBMS robusto con integridad referencial, transaccionalidad ACID y soporte EF Core 10. |
| **ORM / Acceso a Datos** | Entity Framework Core 10 | Mapeo objeto-relacional Code-First, filtros globales para Soft Delete y jerarquías polimórficas (TPH). |
| **Caché en Memoria** | Singleton ConcurrentDictionary | Validación en $O(1)$ de activos sin impacto en la base de datos durante ráfagas de reportes. |
| **Frontend Web Admin** | Blazor WebAssembly (.NET 10) | Single Page Application compilada a WebAssembly; comparte modelos de C# sin duplicar DTOs. |
| **Generación de QR** | QRCoder | Renderizado vectorial y matricial en memoria de códigos QR directos por cada baño. |
| **Frontend Móvil** | .NET MAUI 10 (.NET Multi-platform App UI) | Aplicación nativa compilada para Android y Windows con patrón MVVM estricto (`CommunityToolkit.Mvvm`). |
| **Almacenamiento Local** | `SecureStorage` (Móvil) / `LocalStorage` (Web) | Cifrado nativo de claves/tokens en hardware móvil y persistencia en navegador web. |

---

## 🏛️ 4. Arquitectura General del Sistema

La solución se estructura bajo una **Arquitectura de Slices Verticales** en el Backend y una separación limpia en 4 proyectos:

```
CoworkingMaintanceProject/
├── Coworking.Shared/          # Biblioteca de Dominio Compartida (Modelos, Enums, DTOs, Interfaces)
├── CoworkingAPI/              # Backend Web API (Identity, SignalR Hubs, Handlers CQRS, DbContext)
├── CoworkingAdminWeb/         # Portal Web Blazor WASM (Editor 2D, Árbol Jerárquico, Reporte QR)
└── CoworkingMobile/           # App Móvil .NET MAUI (Panel de Notificaciones y Operación de Aseo)
```

### Modelo de Comunicación y Seguridad
* **Super Administrador (Web):** Control total de la infraestructura (crea sedes, elimina edificios, diseña planos 2D).
* **Administrador de Sede (Web):** Restringido únicamente a los edificios asignados mediante la tabla relacional `UserBuilding`.
* **Personal de Aseo (Móvil MAUI):** Inicia sesión con su cuenta corporativa; el Hub de SignalR detecta sus edificios asignados y lo suscribe automáticamente a los canales `Building_{Id}` correspondientes.
* **Usuario Coworker (Público Web):** Escanea el QR físico e interactúa de manera anónima con la ruta `/report?RestroomId={id}` sin credenciales.

---

## 🚀 5. Instrucciones de Instalación y Ejecución

### Prerrequisitos
1. **.NET 10 SDK** instalado en el sistema.
2. **Workload de .NET MAUI**:
   ```bash
   dotnet workload install maui
   # O específicamente para Android:
   dotnet workload install maui-android
   ```
3. **Motor Docker** (para la base de datos SQL Server) o una instancia local de SQL Server.

---

### Paso 1: Levantar la Base de Datos (SQL Server en Docker)

Ejecuta el siguiente comando en tu terminal para levantar el contenedor oficial de SQL Server 2022:

```bash
docker run -e "ACCEPT_EULA=Y" \
           -e "MSSQL_SA_PASSWORD=D3iby!M0n" \
           -p 1433:1433 \
           --name sqlserver-coworking \
           -d mcr.microsoft.com/mssql/server:2022-latest
```

> [!NOTE]
> Las credenciales configuradas en `CoworkingAPI/appsettings.json` coinciden con este comando (`sa` / `D3iby!M0n`).

---

### Paso 2: Confiar en los Certificados HTTPS Locales

```bash
dotnet dev-certs https --trust
```

---

### Paso 3: Ejecutar el Backend (`CoworkingAPI`)

Abre una terminal en la raíz del proyecto y ejecuta:

```bash
dotnet run --project CoworkingAPI
```

* **Puertos activos:**
  * `https://localhost:7200` (HTTPS para Swagger y clientes Web)
  * `http://0.0.0.0:5200` (HTTP para emuladores móviles y SignalR)
* **Migración Automática:** Al iniciar, la API ejecuta automáticamente `context.Database.Migrate()` y `DataSeeder.SeedRolesAndSuperAdminAsync()`, creando las tablas, los roles, los edificios de prueba y los usuarios administradores.
* **Swagger UI:** Puedes verificar la API ingresando a `https://localhost:7200/swagger`.

---

### Paso 4: Ejecutar el Portal de Administración (`CoworkingAdminWeb`)

En una segunda terminal:

```bash
dotnet run --project CoworkingAdminWeb
```

Abre tu navegador en la URL indicada (habitualmente `https://localhost:7100` o la asignada por el host).

---

### Paso 5: Ejecutar la Aplicación Móvil (`CoworkingMobile`)

Desde **JetBrains Rider** o **Visual Studio**:
1. Selecciona el proyecto de inicio `CoworkingMobile`.
2. Elige el dispositivo de destino:
   * **Emulador de Android:** La app se comunicará automáticamente a `http://10.0.2.2:5200`.
   * **Windows Machine:** La app se comunicará a `https://localhost:7200`.
3. Presiona **Ejecutar / Depurar (F5)**.

---

## 🔑 6. Credenciales y Cuentas Semilla (Entorno de Desarrollo)

El semillero automático (`DataSeeder`) crea las siguientes cuentas de prueba:

| Rol | Correo Electrónico | Contraseña | Alcance / Permisos |
| :--- | :--- | :--- | :--- |
| **Super Administrador** | `superadmin@coworking.com` | `Admin123*` | Control global de todos los edificios, creación y borrado de sedes. |
| **Administrador / Aseo Sede Norte** | `admin@coworking.com` | `Admin123*` | Restringido únicamente al **Edificio Norte (Mock)** y sus alertas. |
| **Usuario Coworker** | *Sin sesión* | N/A | Acceso público exclusivo a `/report?RestroomId=...` mediante QR. |

---

## 🔒 7. Variables de Entorno y Configuración

Los parámetros de conexión se encuentran desacoplados en los archivos de configuración:

* **Backend (`CoworkingAPI/appsettings.json`):**
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost,1433;Database=CoworkingDB;User Id=sa;Password=<TU_CONTRASEÑA>;TrustServerCertificate=True;MultipleActiveResultSets=true"
    }
  }
  ```
* **Frontend Web (`CoworkingAdminWeb/Program.cs`):**
  * `API BaseAddress`: Configurado por defecto a `https://localhost:7200/`.
* **Frontend Móvil (`CoworkingMobile/MauiProgram.cs`):**
  * Detecta la plataforma en tiempo de ejecución: `10.0.2.2:5200` para Android o `https://localhost:7200` para Windows.

---

## 📄 8. Licencia
Proyecto desarrollado con fines académicos y de ingeniería para la gestión de infraestructura colaborativa en espacios de Coworking. Todos los derechos reservados © 2026.
