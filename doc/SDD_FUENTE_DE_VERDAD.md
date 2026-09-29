# 🏛️ Documento de Diseño de Software (SDD) & Fuente Única de Verdad (SSOT)
## Proyecto: SapDiApi.Bridge (Bridge SAP Business One DI API)

> **PROPÓSITO DE ESTE DOCUMENTO:**
> Este documento define la **metodología de diseño y desarrollo (SDD - Software Design Document / Specification-Driven Development)**, la arquitectura técnica, las **reglas innegociables anti-sobreingeniería**, el modelo de **seguridad multi-empresa** y las directrices de **gestión de memoria COM x64** para el proyecto `SapDiApi.Bridge`.
> **Toda IA asistente y todo desarrollador DEBEN consultar y respetar este documento como Fuente Única de Verdad (Single Source of Truth - SSOT)** antes de proponer, diseñar o modificar código en este repositorio.

---

## 🎯 1. Filosofía Central y Reglas Anti-Sobreingeniería

El objetivo principal de `SapDiApi.Bridge` es servir como un **puente ligero, seguro, ultrarrápido y robusto** entre clientes HTTP (REST / GraphQL) y la librería COM no administrada **SAP Business One DI API (`SAPbobsCOM.dll`)**.

### 🚫 Lo que NUNCA se debe hacer (Anti-Patrones Prohibidos):
1. **NO agregar capas intermedias innecesarias:**
   - ❌ No introducir librerías de mediación pesadas (e.g., MediatR, pipelines de comandos complejos) para operaciones que se resuelven con un flujo directo `Controller / GraphQL -> Service -> ISapDiApiConnector / ISapCompanyPool`.
   - ❌ No implementar CQRS sobrediseñado con múltiples proyectos y buses de eventos para una API de puente sincrónico.
   - ❌ No crear abstracciones prematuras (patrones Factory de Factories, repositorios genéricos con 10 interfaces vacías).
2. **NO inventar microservicios ni fragmentar la solución:**
   - Mantener el diseño modular y limpio en el monolito modular actual `.NET 8`.
3. **NO duplicar lógica entre REST y GraphQL:**
   - Ambos protocolos deben consumir los mismos métodos de la capa `Services/` o `ISapDiApiConnector`.
4. **NO retener objetos COM ni olvidar su liberación determinística:**
   - Está terminantemente prohibido iterar colecciones COM hijas (`Lines.Item(i)`, `Fields.Item(u)`, `Decisions.Item(i)`) sin liberar cada referencia con `ComHelper.Release(item)` en un bloque `finally`.
5. **NO abrir y cerrar conexiones COM en cada petición HTTP:**
   - Las conexiones deben ser administradas por el pool inteligente `SapCompanyPool` con caché Keep-Alive por base de datos y semáforos dedicados.

### ✅ Principios Rectores (Obligatorios):
- **KISS (Keep It Simple, Straightforward):** La solución más directa, legible y con menor número de saltos es siempre la preferida.
- **YAGNI (You Aren't Gonna Need It):** No programar código para "posibles casos futuros hipotéticos". Se resuelve únicamente la necesidad actual con contratos claros.
- **Specification-Driven (SDD):** Todo nuevo requerimiento se define primero mediante un contrato/DTO claro, luego su implementación en el servicio SAP y finalmente su exposición en el controlador/GraphQL.
- **Fail-Fast & Transparent Errors:** Los errores de SAP DI API deben ser capturados y devueltos con códigos HTTP apropiados y mensajes descriptivos (estilo SAP Service Layer).

---

## 🏗️ 2. Arquitectura del Sistema

```mermaid
graph TD
    Client[Aplicaciones Externas / Portales / App Móvil / Postman] -->|REST: /b1s/v1 o /api/v1| RestFilter[B1SessionAuthFilter]
    Client -->|GraphQL: /graphql| GqlHelper[GraphQLAuthHelper]

    subgraph "Autenticación & Resolución de Contexto"
        RestFilter -->|Token B1SESSION| SessionMgr[SessionManager (En Memoria)]
        RestFilter -->|X-Api-Key / Header X-Company-DB| ApiKeyCat[ApiKeyOptions / Catálogo Clientes]
        GqlHelper -->|Token B1SESSION o X-Api-Key| ApiKeyCat
        ApiKeyCat -->|Valida AllowedCompanies| ValidSession[UserSession / AuditContext]
    end

    subgraph "Capa de Exposición & Servicios"
        ValidSession --> RestCtrl[Controllers REST]
        ValidSession --> GqlOps[GraphQL Query / Mutation]
        RestCtrl --> Svc[Capa de Servicios / Services]
        GqlOps --> Svc
        Svc --> Connector[SapDiApiConnector]
    end

    subgraph "SapCompanyPool & COM Interop x64"
        Connector --> Pool[SapCompanyPool (Keep-Alive Cache)]
        Pool -->|SemaphoreSlim x Empresa| CompInst[Instancia SAPbobsCOM.Company]
        CompInst -->|Llamadas COM Directas| ComWork[Lectura / Escritura SAP]
        ComWork -->|finally ComHelper.Release| FreeMem[Liberación RCW Inmediata]
    end

    CompInst --> SAPDB[(SAP Business One Server & DB)]
```

### Componentes y Responsabilidades:

| Capa / Directorio | Responsabilidad | Qué debe contener | Qué NO debe contener |
| :--- | :--- | :--- | :--- |
| **`Controllers/`** | Exposición de endpoints REST compatibles con Service Layer | Enrutamiento dual (`/api/v1/` y `/b1s/v1/`), atributos OpenAPI/Scalar, `[B1SessionAuth]`, mapeo HTTP | Lógica de negocio pesada o llamadas directas a COM sin pasar por servicios |
| **`GraphQL/`** | Consultas (`Query.cs`) y Mutaciones (`Mutation.cs`) | Proyecciones Hot Chocolate (`[UseProjection]`, `[UseFiltering]`, `[UseSorting]`), invocación a `Services` | Lógica de acceso a datos duplicada |
| **`Services/<Modulo>/`** | Lógica de negocio y orquestación de operaciones de dominio | Validación de reglas de negocio, consumo de `ICompanyResolverService` e `ISapDiApiConnector` | Objetos HTTP específicos (`HttpContext`, `IActionResult`) |
| **`Services/Sap/`** | Implementación de bajo nivel DI API COM x64 segmentada en `partial class` | `SapDiApiConnector.<Modulo>.cs` (`partial class`) con manipulación de `SAPbobsCOM`, recordsets y mapeos DTO | Lógica de controladores o dependencias HTTP |
| **`Models/<Modulo>/`** | DTOs, Requests, Responses y Payloads | Clases POCO, anotaciones de validación, documentación XML | Lógica de base de datos o referencias a `SAPbobsCOM` |
| **`Infrastructure/`** | Cross-cutting concerns | Seguridad (`B1SessionAuthFilter`, `ApiKeyOptions`), Conexiones (`SapCompanyPool`), Memoria (`ComHelper`), Logging (`Serilog`), Swagger/Scalar | Lógica particular de módulos de SAP |
| **`lib/`** | Dependencias COM x64 nativas | `Interop.SAPbobsCOM.dll` | Archivos no relacionados con la interoperabilidad SAP |

---

## 🧩 2.1. Estándar de Modularidad y Segmentación de Archivos (Partial Classes)

Para evitar archivos monolíticos de miles de líneas y mantener una estructura limpia, escalable y mantenible, se establece la siguiente distribución obligatoria:

```
SapDiApi.Bridge/
├── Controllers/
│   ├── BusinessPartnersController.cs
│   ├── InvoicesController.cs            <-- Nuevo controlador por dominio
│   ├── UsersController.cs
│   └── CompaniesController.cs
├── Services/
│   ├── Invoices/
│   │   ├── IInvoiceService.cs           <-- Interfaz de lógica de negocio
│   │   └── InvoiceService.cs            <-- Valida reglas y llama a ISapDiApiConnector
│   ├── Companies/
│   │   ├── ICompanyResolverService.cs   <-- Resolución de Alias/ID a DB real
│   │   └── CompanyResolverService.cs    <-- SQLite + Caché en Memoria (0 ms)
│   └── Sap/
│       ├── ISapDiApiConnector.cs        <-- Contrato unificado de interoperabilidad SAP
│       ├── SapDiApiConnector.cs         <-- Constructor, pool e inicialización
│       ├── SapDiApiConnector.Invoices.cs<-- partial class: COM logic exclusiva de facturas
│       ├── SapDiApiConnector.Users.cs   <-- partial class: COM logic de usuarios y seguridad
│       ├── SapDiApiConnector.Drafts.cs  <-- partial class: COM logic de borradores
│       └── SapDiApiConnector.Bp.cs      <-- partial class: COM logic de socios de negocio
```

### Reglas de Segmentación:
1. **Regla de `partial class` para `SapDiApiConnector`:**
   - La clase `SapDiApiConnector` implementa `ISapDiApiConnector`.
   - Cada dominio nuevo o existente se codifica en su propio archivo `SapDiApiConnector.<Modulo>.cs` como `public partial class SapDiApiConnector : ISapDiApiConnector`.
   - Se mantiene un único registro en Dependency Injection (`services.AddScoped<ISapDiApiConnector, SapDiApiConnector>()`), pero con código 100% segmentado y desacoplado por dominio.
2. **Separación de Responsabilidades:**
   - `Services/<Modulo>/` contiene las reglas de negocio, validaciones previas y orquestación.
   - `Services/Sap/SapDiApiConnector.<Modulo>.cs` contiene exclusivamente el consumo de `SAPbobsCOM`, recordsets y la liberación con `ComHelper.Release`.

---

## 🔐 3. Modelo de Seguridad y Multi-Empresa

El sistema soporta dos modalidades de consumo autenticado:

### Modalidad A: Sesión Tradicional Service Layer (`B1SESSION`)
- El cliente realiza login en `POST /api/v1/Login` con credenciales de usuario SAP.
- Se emite una cookie y token `B1SESSION` con expiración deslizante (30 minutos).
- Las siguientes peticiones envían la cookie o el header `B1SESSION: <guid>`.

### Modalidad B: Multi-Empresa Desacoplado vía `X-Api-Key` (Recomendado para Integraciones)
- El cliente externo envía una API Key maestra o por aplicación en la cabecera `X-Api-Key` o `Authorization: ApiKey <clave>`.
- El filtro resuelve la identidad del cliente contra `ApiKeyAuth:Clients` en `appsettings.json`.
- **Capa de Abstracción de Empresas (`ICompanyResolverService`):**
  - Los clientes externos NO necesitan conocer ni exponer los nombres técnicos de bases de datos HANA/SQL.
  - Pueden enviar `X-Company-Id: <id_o_alias>`, `X-Company-Code: <codigo>` o el tradicional `X-Company-DB: <nombre_bd>`.
  - El sistema resuelve la base de datos destino en **0 ms** mediante un catálogo persistido en SQLite (`Data/bridge_metadata.db`) con réplica en memoria (`ConcurrentDictionary`).
  - Sincronización nativa con SAP mediante `company.GetCompanyList()` vía SLD (sin requerir permisos cruzados de base de datos `SBOCOMMON`).
- **Validación de Permisos (`AllowedCompanies`):** Si la empresa resuelta no está en la lista permitida para esa API Key, el sistema rechaza la petición con `403 Forbidden`.
- **Trazabilidad y Auditoría:** Se pueden enviar las cabeceras opcionales `X-Audit-User` (usuario final operador) y `X-Audit-App` para registro en los logs de auditoría sin consumir licencias nominales de SAP por cada operador.

### Cabeceras HTTP Soportadas:

| Cabecera | Requerida | Propósito | Ejemplo |
| :--- | :--- | :--- | :--- |
| `X-Api-Key` | Sí (si no hay `B1SESSION`) | Autenticación de aplicación cliente | `ak_live_a1b2c3d4e5...` |
| `X-Company-Id` | Opcional | Identificador o Alias público de la empresa | `EMP-01` o `GT-PROD` |
| `X-Company-Code` | Opcional | Código mnemónico de la empresa | `GT_PROD` |
| `X-Company-DB` | Opcional | Nombre técnico de la base de datos SAP (compatibilidad) | `SBODemoGT` |
| `X-Audit-User` | Opcional | Identificador del usuario final que opera | `juan.perez` |
| `X-Audit-App` | Opcional | Nombre del módulo o subsistema emisor | `PortalProveedores` |
| `B1SESSION` | Opcional | Token de sesión SAP Service Layer | `c4b12345-...` |

---

## ⚡ 4. Pool de Conexiones Keep-Alive y Gestión de Memoria COM

### 1. Conexiones Persistentes (`SapCompanyPool`):
- Mantiene un diccionario concurrente (`ConcurrentDictionary<string, PooledCompanyEntry>`) donde la clave de conexión es `Server|CompanyDB|UserName`.
- Al recibir una solicitud para una base de datos ya conectada, la reutiliza inmediatamente en **~15ms** en lugar de reconectar (que toma ~3000ms).
- **Control de Inactividad (Sliding Expiration):** Las instancias de `Company` se desconectan y liberan automáticamente tras `IdleConnectionTimeoutMinutes` (por defecto 15 min) sin actividad.
- **Thread-Safety COM:** Cada entrada del pool dispone de su propio `SemaphoreSlim(1, 1)` para asegurar que dos hilos no ejecuten operaciones concurrentes en la misma instancia COM de SAP simultáneamente.
- **Límite Global de Concurrencia:** Controlado mediante un `SemaphoreSlim` global configurado por `MaxConcurrentConnections`.

### 2. Liberación Determinística de Memoria COM (`ComHelper.cs`):
- **Causa raíz de crashes `0xc0000374` (Heap Corruption) y `0xc0000005` (Access Violation):** En aplicaciones x64 que consumen `SAPbobsCOM`, invocar métodos indexados como `Lines.Item(i)` o `Fields.Item(u)` instancia objetos Runtime Callable Wrapper (RCW) en el heap no administrado. Si el Garbage Collector de .NET intenta liberar estos punteros cuando el hilo ha finalizado o la conexión ha cambiado, el proceso colapsa.
- **Regla Estricta:** Todo objeto COM obtenido debe ser liberado de inmediato con `ComHelper.Release(obj)` (que ejecuta `Marshal.FinalReleaseComObject`) dentro de un bloque `finally`.

Ejemplo de implementación obligatoria:
```csharp
SAPbobsCOM.BusinessPartners? oBP = null;
try
{
    oBP = (SAPbobsCOM.BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);
    if (oBP.GetByKey(cardCode))
    {
        var contacts = oBP.ContactEmployees;
        if (contacts != null)
        {
            for (int i = 0; i < contacts.Count; i++)
            {
                contacts.SetCurrentLine(i);
                // ... mapeo de propiedades ...
            }
            ComHelper.Release(contacts);
        }
    }
}
finally
{
    ComHelper.Release(oBP);
}
```

---

## 📊 5. Matriz de Cobertura de Endpoints

Todos los endpoints del sistema están diseñados con compatibilidad dual REST (convención SAP Service Layer `/b1s/v1/` y `/api/v1/`) y GraphQL (`/graphql`), soportando autenticación por `B1SESSION` y `X-Api-Key` multi-empresa:

| Módulo / Recurso | Método REST | Ruta REST Dual | Operación GraphQL | Autenticación | Multi-Empresa |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Catálogo de Empresas** | `GET` | `/api/v1/Companies` | N/A | `ApiKey` / Pública | Filtro por API Key |
| **Catálogo de Empresas** | `GET` | `/api/v1/Companies/{id}` | N/A | `ApiKey` / Pública | Sí |
| **Catálogo de Empresas** | `POST` | `/api/v1/Companies/sync` | N/A | `ApiKey` (Admin) | Sincronización SLD |
| **Autenticación** | `POST` | `/api/v1/Login` | `mutation { login(...) }` | Pública | Sí (`CompanyDB` / `CompanyId`) |
| **Autenticación** | `POST` | `/api/v1/Logout` | `mutation { logout }` | `B1SessionAuth` | Sí |
| **Autenticación** | `GET` | `/api/v1/SessionInfo` | N/A | `B1SessionAuth` | Sí |
| **Usuarios SAP** | `GET` | `/api/v1/Users` | `query { users(filter) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Usuarios SAP** | `GET` | `/api/v1/Users({id})` | `query { userById(id) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Usuarios SAP** | `POST` | `/api/v1/Users({id})/ChangePassword` | `mutation { changeUserPassword(...) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Autorizaciones** | `GET` | `/api/v1/ApprovalRequests` | `query { approvalRequests(filter) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Autorizaciones** | `GET` | `/api/v1/ApprovalRequests({code})` | `query { approvalRequestByCode(code) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Autorizaciones** | `PATCH/PUT` | `/api/v1/ApprovalRequests({code})` | `mutation { updateApprovalRequest(code, input) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Borradores (Drafts)** | `GET` | `/api/v1/Drafts` | `query { drafts(filter) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Borradores (Drafts)** | `GET` | `/api/v1/Drafts({docEntry})` | `query { draftByDocEntry(docEntry) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Borradores (Drafts)** | `POST` | `/api/v1/Drafts({docEntry})/SaveDraftToDocument` | `mutation { saveDraftToDocument(docEntry) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Socios de Negocio** | `GET` | `/api/v1/BusinessPartners` | `query { businessPartners }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Socios de Negocio** | `GET` | `/api/v1/BusinessPartners('{code}')` | `query { businessPartnerByCardCode(cardCode) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Socios de Negocio** | `POST` | `/api/v1/BusinessPartners` | `mutation { createBusinessPartner(input) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Socios de Negocio** | `PATCH/PUT` | `/api/v1/BusinessPartners('{code}')` | `mutation { updateBusinessPartner(cardCode, input) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Anexos (Attachments2)**| `POST` | `/api/v1/Attachments2` | `mutation { createAttachment(input) }` | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Anexos (Attachments2)**| `PATCH/PUT` | `/api/v1/Attachments2({id})` | N/A | `B1SessionAuth` / `ApiKey` | Sí (`X-Company-Id`) |
| **Diagnóstico SAP** | `POST` | `/api/v1/Sap/TestConnection` | N/A | `ApiKey` | Sí (`CompanyDB` / `CompanyId`) |
| **Salud del Sistema**| `GET` | `/api/health` | `query { health }` | Pública | N/A |
| **Pruebas y Echo** | `GET/POST` | `/api/test/secure-ping`, `/api/test/echo` | N/A | `B1SessionAuth` / `ApiKey` | Sí |

---

## 🔁 6. Metodología SDD: Flujo Paso a Paso para Nuevos Requerimientos

Cada vez que se deba agregar un nuevo endpoint, entidad de SAP o funcionalidad, **se debe seguir estrictamente este flujo de 5 pasos**:

```
[ Paso 1: Especificar Modelo/DTO en Models/<Modulo>/ ] 
                               ⬇️
[ Paso 2: Crear SapDiApiConnector.<Modulo>.cs (partial class) & Service de Dominio ]
                               ⬇️
[ Paso 3: Exponer en Controller REST (con Documentación XML y [B1SessionAuth]) ]
                               ⬇️
[ Paso 4: Exponer en GraphQL (Hot Chocolate Query / Mutation) ]
                               ⬇️
[ Paso 5: Validación y Pruebas (dotnet build x64 / Test) ]
```

### Paso 1: Especificar Modelo / Contrato (DTO)
- Ubicación: `Models/<Modulo>/`
- Crear DTOs de entrada (`CreateXRequestDto` o `XInput`) y de salida (`XDto`).
- Usar nombres de propiedades estándar de SAP Service Layer (PascalCase en C#, serializado a JSON compatible).

### Paso 2: Implementar Lógica COM en `SapDiApiConnector.<Modulo>.cs` y Servicio de Dominio
- Extender `SapDiApiConnector` creando un archivo `Services/Sap/SapDiApiConnector.<Modulo>.cs` con `public partial class SapDiApiConnector : ISapDiApiConnector`.
- Si se requiere orquestación de negocio o validaciones adicionales, crear `Services/<Modulo>/<Modulo>Service.cs`.
- Siempre solicitar la ejecución a través de `_companyPool.ExecuteAsync(connInfo, company => ...)`.
- Liberar todos los objetos COM creados con `ComHelper.Release(obj)` en bloques `finally`.

### Paso 3: Exponer en Controlador REST
- Ubicación: `Controllers/<Modulo>Controller.cs`
- Rutas duales compatibles: `@Route("~/api/v1/Modulo")` y reescritura automática `/b1s/v1/Modulo`.
- **Obligatorio:** Comentarios XML en C# (`<summary>`, `<remarks>`, `<response>`) para que Swagger/Scalar generen la documentación viva sin pasos extra.
- Decorar con `[B1SessionAuth]`.

### Paso 4: Exponer en GraphQL (Hot Chocolate)
- Lecturas: Agregar método en `GraphQL/Query.cs`.
- Escrituras: Agregar método en `GraphQL/Mutation.cs`.
- Extraer sesión mediante `GraphQLAuthHelper.RequireSession(...)`.

### Paso 5: Verificación
- Compilación limpia x64 (`dotnet build /t:CoreCompile`).
- Verificación en Scalar (`/doc`) y Banana Cake Pop (`/graphql`).

---

## 📋 7. Checklist de Verificación para Asistentes IA y Desarrolladores

Antes de entregar cualquier cambio en el código, verificar:

- [ ] **¿El cambio introduce código innecesario o patrones complejos sin justificación?** (Si la respuesta es sí, simplificar).
- [ ] **¿La lógica de SAP COM se implementó en su correspondiente `SapDiApiConnector.<Modulo>.cs` usando `partial class`?**
- [ ] **¿Se liberan todos los objetos `SAPbobsCOM` y colecciones hijas con `ComHelper.Release()` en bloques `finally`?**
- [ ] **¿Se agregaron comentarios XML C# en los métodos de los controladores y DTOs?**
- [ ] **¿Se mantuvieron las convenciones de rutas duales (`/api/v1/...` y `/b1s/v1/...`)?**
- [ ] **¿El nuevo endpoint respeta el filtro `[B1SessionAuth]` o `GraphQLAuthHelper.RequireSession` para soportar `X-Company-Id`, `X-Company-Code` y `X-Company-DB`?**
- [ ] **¿Compila sin errores ni advertencias (`dotnet build /t:CoreCompile`) en arquitectura `x64`?**

---

## 📚 8. Referencias y Documentación Relacionada
- **Guía de Frontend & Catálogo de Empresas:** [AUTENTICACION_Y_CATALOGO_EMPRESAS_FRONTEND.md](file:///c:/Users/fmartir/source/Mis%20Proyectos%202026/BridgeSap/doc/AUTENTICACION_Y_CATALOGO_EMPRESAS_FRONTEND.md)
- **Guía de GraphQL y OpenAPI:** [GUIA_DESARROLLO_DOCUMENTACION_Y_GRAPHQL.md](file:///c:/Users/fmartir/source/Mis%20Proyectos%202026/BridgeSap/doc/GUIA_DESARROLLO_DOCUMENTACION_Y_GRAPHQL.md)
- **Generador de API Keys:** `scripts/generate_api_key.py`
- **Configuración de Swagger/Scalar:** [SwaggerConfigurationExtensions.cs](file:///c:/Users/fmartir/source/Mis%20Proyectos%202026/BridgeSap/SapDiApi.Bridge/Infrastructure/Swagger/SwaggerConfigurationExtensions.cs)
- **Punto de Entrada del Proyecto:** [Program.cs](file:///c:/Users/fmartir/source/Mis%20Proyectos%202026/BridgeSap/SapDiApi.Bridge/Program.cs)
