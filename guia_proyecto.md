# Guía del Proyecto: Sistema de Gestión de Mantenimiento Interactivo (Coworking)

## Contexto del Proyecto

El proyecto consiste en el **diseño e implementación de un sistema de gestión de mantenimiento interactivo en tiempo real** enfocado en optimizar la infraestructura sanitaria (baños) en espacios de Coworking. El objetivo principal es reducir el Tiempo Medio de Reparación (MTTR) de incidencias y mitigar riesgos, eliminando las rondas de limpieza ineficientes.

El sistema se basa íntegramente en el ecosistema **.NET (C#)** y se compone de tres frentes principales:
1. **API Backend (.NET Core Web API):** Orquesta la lógica de negocio, persistencia (Entity Framework Core) y comunicación bidireccional mediante SignalR (WebSockets).
2. **Aplicación Móvil (.NET MAUI):**
   - **Para usuarios regulares:** Permite emitir alertas precisas y rápidas (ej. falta de papel, avería).
   - **Para operarios de limpieza:** Interpreta alertas en tiempo real, guiando al personal mediante mapas interactivos o de calor.
3. **Módulo Administrativo (Blazor WebAssembly):** Permite a los Facility Managers modelar visualmente la topología de los baños mediante un **Grid System 2D interactivo** (drag-and-drop), ubicando dispensadores, retretes y lavamanos.

---

## Estado Actual (Qué está hecho)

Tras la revisión del código fuente actual, se ha detectado el siguiente progreso:

### 1. Backend (`CoworkingAPI` y `Coworking.Shared`)
- **Arquitectura Base:** Implementación inicial de Vertical Slices / CQRS (carpeta `Features` con `CommandHandlers` como `CreateIssueReportCommandHandler`).
- **Persistencia de Datos:** Entity Framework Core configurado (`AppDbContext`) con migraciones iniciales (`Migrations`).
- **Modelos de Dominio y DTOs:** Ampliamente definidos en `Coworking.Shared` (`Building`, `Floor`, `Restroom`, `RestroomAsset`, `IssueReport`, etc.). Interfaces clave como `ISoftDeletable` e `IAlertPublisher` están creadas.
- **SignalR (Tiempo Real):** Configuración básica del hub de alertas (`AlertTelemetryHub` y `SignalRAlertPublisher`) integrada con la creación de reportes.

### 2. Frontend Móvil (`CoworkingMobile`)
- Proyecto estructurado en **.NET MAUI** utilizando patrón **MVVM** (`Models`, `Pages`, `PageModels`).
- **Limitación actual:** El código base actual en la app móvil corresponde a un template de gestión de tareas genérico (`Project`, `Task`, `Tag`, gráficos y dashboards básicos). Todavía no implementa el cliente de SignalR ni las interfaces específicas para los Aseadores (mapas de alertas) o Usuarios (botones de pánico para limpieza).

### 3. Frontend Web Administrador (`CoworkingAdminWeb`)
- Proyecto **Blazor WebAssembly** inicializado y funcional.
- **Limitación actual:** Solo contiene las páginas del template por defecto de Blazor (`Counter`, `Weather`, `Home`). El motor de mapeo (Grid System 2D interactivo) aún no se ha construido.

---

## Qué hace falta (Pendientes de Desarrollo)

Para cumplir con los objetivos del documento técnico (`ProyectoDeybiMontenegro2026_2.pdf`), es necesario llevar a cabo las siguientes tareas:

### 1. Desarrollo del Motor de Mapeo en Blazor (Prioridad Alta)
- Limpiar el template genérico de `CoworkingAdminWeb`.
- Implementar el lienzo cartesiano en Blazor (`<svg>` o canvas HTML5 interconectado).
- Crear los eventos nativos `@ondragstart` y `@ondrop` para arrastrar activos (lavamanos, inodoros) hacia el plano bidimensional.
- Aplicar lógica de "snap-to-grid" y validación para evitar colisiones lógicas en el plano.
- Serializar la matriz a JSON o enviar las coordenadas al backend para guardarse en Base de Datos.

### 2. Integración y Lógica Móvil (.NET MAUI)
- **Operarios de Limpieza:** Instalar e implementar cliente SignalR (`HubConnectionBuilder`) para escuchar alertas en tiempo real de `AlertTelemetryHub`. Construir el Dashboard que muestre notificaciones push locales y el estado visual del piso (cuáles activos están en rojo/averiados).
- **Usuarios Regulares:** Crear los formularios simplificados de reporte de incidencias (llamando a `IssuesController` / `CreateIssueReport`).
- Eliminar el código del template de Tareas/Proyectos.

### 3. Ajustes y Cierre en el Backend (API)
- Implementar seguridad y autenticación **JWT (JSON Web Tokens)** para proteger los endpoints y hubs de SignalR.
- Lógica automatizada para cálculo de métricas (MTTR = Tiempo Final de Resolución - Tiempo Inicial del Reporte).
- Agregar Pruebas Unitarias/Integración (xUnit o NUnit) para validar la lógica del Grid System (ausencia de colisiones).
