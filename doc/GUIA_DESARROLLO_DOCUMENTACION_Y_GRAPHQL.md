# 📘 Guía de Desarrollo: Documentación de Endpoints y Extensión de GraphQL

Esta guía detalla cómo mantener, documentar y extender los endpoints de la API (**REST / OpenAPI / Scalar**) y cómo programar nuevas consultas y mutaciones en **GraphQL** conectado a **SAP Business One DI API**.

---

## 📁 1. Estructura y Ubicación de Archivos

| Módulo / Funcionalidad | Ruta en el Proyecto | Descripción |
| :--- | :--- | :--- |
| **Controladores REST** | `SapDiApi.Bridge/Controllers/` | Contiene los endpoints REST compatibles con Service Layer (`AttachmentsController`, `BusinessPartnersController`, `AuthController`, etc.). |
| **Configuración Swagger / Scalar** | `SapDiApi.Bridge/Infrastructure/Swagger/SwaggerConfigurationExtensions.cs` | Configuración visual, seguridad, títulos y descripción global de OpenAPI y Scalar. |
| **Consultas GraphQL (Read)** | `SapDiApi.Bridge/GraphQL/Query.cs` | Métodos de lectura (`Query`) para obtener datos de SAP con proyecciones y filtros. |
| **Mutaciones GraphQL (Write)** | `SapDiApi.Bridge/GraphQL/Mutation.cs` | Métodos de escritura (`Mutation`) para crear o modificar datos (Socios de Negocio, Anexos, Login, etc.). |
| **Helper de Autenticación GraphQL** | `SapDiApi.Bridge/GraphQL/GraphQLAuthHelper.cs` | Extrae y valida `B1SESSION`, Cookies y `X-Api-Key` en GraphQL. |
| **Servicios de Negocio** | `SapDiApi.Bridge/Services/` | Lógica de negocio y conexión (`BusinessPartners/`, `Auth/`, `Health/`, `Sap/`). |
| **Modelos y DTOs** | `SapDiApi.Bridge/Models/` | Clases de entrada y salida de datos (Payloads, DTOs, Entidades). |
| **Configuración Principal** | `SapDiApi.Bridge/Program.cs` | Registro de servicios en el contenedor de dependencias (DI) y pipeline HTTP. |

---

## 📝 2. Cómo Modificar la Documentación de Endpoints REST (Swagger / OpenAPI / Scalar)

La documentación de OpenAPI y Scalar se genera automáticamente a partir de dos elementos en los controladores:
1. **Comentarios de Documentación XML** (encima de cada método del controlador).
2. **Atributos de Controlador C#** (`[ProducesResponseType]`, `[Route]`, `[Tags]`).

### Paso a Paso para Documentar un Endpoint REST

Ubica el archivo del controlador en `SapDiApi.Bridge/Controllers/` (por ejemplo, `BusinessPartnersController.cs`):

```csharp
/// <summary>
/// Título corto del endpoint (Ej: Consultar Socio de Negocio por Código)
/// </summary>
/// <remarks>
/// Explicación detallada de lo que hace el endpoint, compatibilidad con SAP Service Layer,
/// notas de seguridad, parámetros opcionales y reglas de negocio.
/// </remarks>
/// <param name="cardCode">Código del Socio de Negocio en SAP (Ej: 'C00001').</param>
/// <response code="200">Socio de negocio encontrado exitosamente.</response>
/// <response code="401">No autorizado - Sesión B1SESSION inválida o ausente.</response>
/// <response code="404">No se encontró ningún socio de negocio con ese código.</response>
[HttpGet]
[Route("~/api/v1/BusinessPartners('{cardCode}')")]
[B1SessionAuth] // Aplica automáticamente el candado de seguridad en Swagger/Scalar
[ProducesResponseType(typeof(ApiResponse<BusinessPartnerDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetByCardCode(string cardCode)
{
    // Lógica del controlador...
}
```

### Modificar la Información General de la API (Portada Scalar / Swagger)
Si deseas cambiar el título, descripción de bienvenida o contacto:
1. Abre `SapDiApi.Bridge/Infrastructure/Swagger/SwaggerConfigurationExtensions.cs`.
2. Modifica el bloque `options.SwaggerDoc("v1", new OpenApiInfo { ... })`.

---

## ⚡ 3. Cómo Programar y Extender GraphQL (Hot Chocolate)

GraphQL utiliza dos clases principales en la carpeta `SapDiApi.Bridge/GraphQL/`:
- **`Query.cs`**: Para consultas y lecturas (equivalente a `GET`).
- **`Mutation.cs`**: Para creaciones, modificaciones y eliminaciones (equivalente a `POST`, `PATCH`, `DELETE`).

---

### A. Cómo Agregar una Nueva Consulta (`Query`)

Abre `SapDiApi.Bridge/GraphQL/Query.cs` y agrega tu nuevo método.

#### Ejemplo 1: Consulta directa con sesión SAP obligatoria
```csharp
/// <summary>
/// Consulta un anexo por su ID absoluto (AbsoluteEntry).
/// </summary>
public async Task<AttachmentDto?> GetAttachmentByEntry(
    int absoluteEntry,
    [Service] ISapDiApiConnector sapConnector,
    [Service] IHttpContextAccessor httpContextAccessor,
    [Service] ISessionManager sessionManager,
    [Service] IOptions<ApiKeyOptions> apiKeyOptions)
{
    // 1. Extrae y valida la sesión B1SESSION (lanza excepción clara si no existe)
    var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);

    // 2. Invoca la lógica contra SAP DI API
    return await sapConnector.GetAttachmentAsync(session, absoluteEntry);
}
```

#### Ejemplo 2: Consulta de lista con Filtros, Proyecciones y Ordenamiento automático
Hot Chocolate permite que el cliente decida qué campos traer y cómo filtrar sin que tú tengas que programar SQL manualmente:

```csharp
[UseProjection] // Permite al cliente pedir solo los campos que necesita
[UseFiltering]  // Habilita filtros (eq, contains, startsWith, in, gt, lt, etc.)
[UseSorting]    // Habilita ordenamiento (ASC, DESC)
public IQueryable<BusinessPartnerDto> GetBusinessPartners([Service] IBusinessPartnerService bpService)
{
    return bpService.GetBusinessPartnersQueryable();
}
```

---

### B. Cómo Agregar una Nueva Mutación (`Mutation`)

Abre `SapDiApi.Bridge/GraphQL/Mutation.cs` y agrega tu método de acción/escritura:

```csharp
/// <summary>
/// Crea un nuevo documento de Oferta o Pedido de Venta en SAP Business One.
/// </summary>
public async Task<DocumentMutationResult> CreateSalesOrder(
    CreateDocumentInput input,
    [Service] ISapDiApiConnector sapConnector,
    [Service] IHttpContextAccessor httpContextAccessor,
    [Service] ISessionManager sessionManager,
    [Service] IOptions<ApiKeyOptions> apiKeyOptions)
{
    // 1. Validar autenticación
    var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);

    // 2. Ejecutar operación en SAP DI API
    var (success, docEntry, docNum, errorMessage) = await sapConnector.CreateSalesOrderAsync(session, input);

    if (!success)
    {
        // En GraphQL los errores se lanzan con GraphQLException
        throw new GraphQLException(errorMessage ?? "Error al registrar pedido en SAP.");
    }

    return new DocumentMutationResult
    {
        Success = true,
        DocEntry = docEntry,
        DocNum = docNum
    };
}
```

---

### C. Buenas Prácticas al Programar en GraphQL

1. **Inyección de Dependencias con `[Service]`:**
   - En GraphQL no necesitas inyectar servicios en el constructor. Puedes recibirlos directamente como parámetros del método usando el atributo `[Service] MiServicio servicio`.
2. **Autenticación con `GraphQLAuthHelper`:**
   - Usa siempre `GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions)` para obtener la instancia activa de `UserSession`.
3. **Manejo de Errores:**
   - Para retornar un error comprensible al cliente en GraphQL, utiliza `throw new GraphQLException("Mensaje del error");`.
4. **Nombres de Campos (CamelCase):**
   - Aunque en C# los métodos se llamen `GetBusinessPartnerByCardCode`, GraphQL los expone automáticamente en camelCase: `businessPartnerByCardCode`.

---

## 🧪 4. Cómo Probar tus Nuevos Endpoints y GraphQL

### 1. Entorno Interactivo GraphQL (Banana Cake Pop)
1. Inicia la aplicación (`dotnet run` o F5 en Visual Studio).
2. Abre en tu navegador: **`http://localhost:5005/graphql`**
3. En la pestaña **Headers**, añade:
   ```json
   {
     "B1SESSION": "tu_token_de_sesion"
   }
   ```
4. El explorador cuenta con autocompletado inteligente (`Ctrl + Espacio`) de todo el esquema disponible.

### 2. Documentación Interactiva Scalar
- **Scalar UI:** `http://localhost:5005/doc`
- **OpenAPI Schema JSON:** `http://localhost:5005/swagger/v1/swagger.json`
- **Redirección automática raíz (`/`):** Al entrar a `http://localhost:5005/` serás redirigido directamente a `/doc`.
