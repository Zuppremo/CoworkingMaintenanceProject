# Agente .NET MAUI y Blazor - Reglas y Estilos de Programación

Este documento establece las normativas arquitectónicas y convenciones de codificación que **todo agente IA y desarrollador** debe seguir estrictamente al generar código para este ecosistema.

## 1. Reglas Generales del Ecosistema
- **Lenguaje Único (C# de Extremo a Extremo):** Se prioriza fuertemente el uso exclusivo de C# y .NET. Se debe evitar la integración forzosa de librerías complejas de JavaScript (React, Vue, D3.js) a menos que sea estrictamente necesario mediante JSInterop para casos muy específicos.
- **Código Limpio:** Nomenclatura PascalCase para clases, interfaces (prefijo `I`), métodos y propiedades públicas. Usar camelCase para variables locales y campos privados.

## 2. Capa Backend (`CoworkingAPI` & `Coworking.Shared`)
- **Arquitectura (Vertical Slices):** El código se organiza por características (Features) en lugar de capas técnicas. Cada carpeta de `Features` (ej. `Features/IssueReports`) debe contener sus propios Controladores, Handlers (patrón CQRS) y modelos específicos si aplica.
- **Manejo de Inyección de Dependencias:** Usar siempre inyección por constructor para servicios como `AppDbContext`, `IAlertPublisher`, etc.
- **Entity Framework Core:** El esquema se diseña mediante Code-First. Nunca modificar la base de datos manualmente. Siempre crear migraciones. Entidades lógicas borrables deben implementar la interfaz compartida `ISoftDeletable`.
- **Tiempo Real:** Se usa SignalR. Cualquier estado modificado que requiera UI reactiva (ej. nueva alerta) debe disparar un evento mediante `IAlertPublisher.BroadcastStatusUpdateAsync`.

## 3. Capa Frontend Administrador (`CoworkingAdminWeb` - Blazor WASM)
- **Grid System y Manipulación Visual:** El mapeo bidimensional de los baños debe basarse en eventos puros de Blazor (`@ondragstart`, `@ondrop`, `@onmousemove`) manipulando coordenadas SVG o DOM, manteniendo la lógica matemática en C#.
- **Separación de Responsabilidades:** Utilizar componentes de Razor limpios con Code-Behind parcial (`.razor.cs`) si la lógica del componente es muy extensa.

## 4. Capa Cliente Móvil (`CoworkingMobile` - .NET MAUI)
- **Patrón MVVM Estricto:** Toda página (`.xaml`) debe estar acompañada de un PageModel o ViewModel. Toda la lógica de negocio, llamadas a la API y comandos (Commands) residen en el ViewModel, NUNCA en el Code-Behind del `.xaml.cs` (excepto para animaciones o configuraciones de UI muy locales).
- **Consumo de SignalR:** Los clientes móviles de los aseadores deben usar `Microsoft.AspNetCore.SignalR.Client` para mantener una conexión abierta. Escuchar eventos y actualizar colecciones observables (`ObservableCollection<T>`) para que la UI se refresque automáticamente.
- **Diseño de Interfaz:** Mantener UI de fricción mínima. Aprovechar `CommunityToolkit.Mvvm` para generadores de código (`[ObservableProperty]`, `[RelayCommand]`) que reducen el boilerplate.

## 5. Pruebas y Validación (Testing)
- **Lógica Crítica:** Usar `xUnit` o `NUnit` para probar que no existan colisiones espaciales lógicas al registrar un nuevo objeto en las coordenadas $(X, Y)$ del backend.
- **Asincronía:** Garantizar `async/await` en todos los procesos de I/O de disco, red y DB, usando `Task` y `Task<T>`.
