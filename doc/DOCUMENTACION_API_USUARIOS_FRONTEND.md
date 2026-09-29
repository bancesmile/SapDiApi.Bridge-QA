# 📘 Especificación y Guía Frontend: Módulo de Usuarios SAP (`Users`)
## SapDiApi.Bridge — Documento de Integración para Frontend / IA de UI

> **PROPÓSITO:**  
> Este documento contiene la especificación completa, contratos de datos (DTOs), endpoints REST y operaciones GraphQL para construir la interfaz de usuario (UI / Frontend) de **Administración de Usuarios de SAP Business One**.

---

## 🔐 1. Métodos de Autenticación Soportados

El frontend puede comunicarse con la API mediante cualquiera de estos dos mecanismos:

### Opción A: Autenticación por `X-Api-Key` (Recomendado para SPA / Frontend Integrado)
Envía las siguientes cabeceras HTTP en cada petición:

```http
X-Api-Key: <tu_api_key>
X-Company-DB: TEST1_SBO_UNOCINCO
X-Audit-User: <usuario_operador_logueado>
X-Audit-App: PortalAdminUsuarios
Content-Type: application/json
```

### Opción B: Sesión Tradicional `B1SESSION`
1. El usuario inicia sesión en `POST /api/v1/Login` con credenciales de SAP.
2. La API emite el token `B1SESSION`.
3. En las siguientes peticiones enviar:
```http
B1SESSION: <guid_session_id>
Content-Type: application/json
```

---

## 🧩 2. Estructura del Objeto Usuario (`UserDto`)

El modelo representa fielmente los campos administrativos de la ventana **"Usuarios - Definiciones" (OUSR)** de SAP Business One:

```typescript
export interface UserDto {
  // Identificadores y Estado
  InternalKey: number;             // USERID de SAP (ej: 200)
  UserCode: string;                // Código de login único (ej: "creditos08")
  UserName: string;                // Nombre completo o descriptivo (ej: "Gabriela Acabal")
  Superuser: "tYES" | "tNO";       // ¿Es Superusuario?
  MobileUser: "tYES" | "tNO";      // ¿Habilitado para app móvil?
  Locked: "tYES" | "tNO";          // ¿Cuenta bloqueada?
  Defaults?: string | null;        // Grupo de valores predeterminados

  // Pestaña General
  WindowsUserName?: string | null; // Usuario de dominio Windows
  EmployeeId?: number | null;      // ID de empleado vinculado (OHEM)
  EmployeeName?: string | null;    // Nombre del empleado vinculado
  eMail?: string | null;           // Correo electrónico principal
  MobilePhoneNumber?: string | null; // Teléfono móvil
  MobileDeviceId?: string | null;  // ID del dispositivo móvil (ej: "7JU4MJ5MUH8...")
  Remarks?: string | null;         // Observaciones / Comentarios
  Branch?: number | null;          // Código de sucursal (BPLId)
  BranchName?: string | null;      // Nombre de sucursal
  Department?: number | null;      // Código de departamento (OUDP)
  DepartmentName?: string | null;  // Nombre de departamento
  Group?: string | null;           // Grupo de usuarios (ej: "ug_Regular")
  PasswordNeverExpires?: "tYES" | "tNO";
  ChangePasswordNextLogon?: "tYES" | "tNO";

  // Pestaña Servicios & Visualizar
  LanguageCode?: string | null;    // Idioma (ej: "ln_Spanish_La")
  ScreenLockTime?: number | null;  // Tiempo de bloqueo en minutos (ej: 1800)

  // Campos de Usuario (UDFs - Facturación Electrónica / Custom)
  U_Establecimiento?: string | null;       // FE - Establecimiento
  U_visualizar_todos_DTE?: "Y" | "N";      // FE - ¿Ver DTE de otros usuarios?
  U_MultiEst?: "Y" | "N";                  // FE - Múltiples Establecimientos
  U_MensajeEnvioDocto?: "Y" | "N";         // FE - ¿Mostrar mensaje al enviar documento?
  U_ActivarLog?: "Y" | "N";                // FE - ¿Activar log?
  U_ActivarXML?: "Y" | "N";                // FE - ¿Guardar XML?
  UserFields?: Record<string, any> | null; // Otros UDFs dinámicos

  // Metadatos de Auditoría y Conexión
  LastLoginTime?: string | null;           // HH:mm:ss
  LastLogoutDate?: string | null;          // yyyy-MM-dd
  LastLogoutTime?: string | null;          // HH:mm:ss
  LastPasswordChangeTime?: string | null;  // HH:mm:ss
  LastPasswordChangedBy?: string | null;   // Usuario que modificó la clave

  // Permisos (Opcional, sólo si se solicita con ?includePermissions=true)
  UserPermission?: Array<{
    UserCode: number;
    PermissionID: string;
    Permission: "boper_Full" | "boper_ReadOnly" | "boper_None";
  }> | null;
}
```

---

## 🌐 3. Endpoints REST API

Base URL: `https://<servidor-bridge>/api/v1` (o compatible con Service Layer `/b1s/v1`)

### 3.1. Listar Usuarios (para Data Grids y Búsquedas)
- **Método:** `GET`
- **Ruta:** `/api/v1/Users` (o `/b1s/v1/Users`)
- **Parámetros Query (Filtros):**
  - `search` *(string)*: Busca por coincidencia parcial en `UserCode` o `UserName`.
  - `locked` *(string)*: `tYES` (solo bloqueados) o `tNO` (solo activos).
  - `superuser` *(string)*: `tYES` o `tNO`.
  - `branch` *(number)*: Filtrar por ID de sucursal.
  - `department` *(number)*: Filtrar por ID de departamento.
  - `page` *(number, defecto: 1)*: Página actual.
  - `pageSize` *(number, defecto: 50, máx: 500)*: Cantidad de registros por página.

**Ejemplo de Petición:**
`GET /api/v1/Users?search=creditos&locked=tNO&page=1&pageSize=20`

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Usuarios recuperados para la sociedad 'TEST1_SBO_UNOCINCO'",
  "data": [
    {
      "InternalKey": 200,
      "UserCode": "creditos08",
      "UserName": "Gabriela Acabal",
      "Superuser": "tNO",
      "MobileUser": "tNO",
      "Locked": "tNO",
      "eMail": "gacabal@milesimo.com.gt",
      "MobilePhoneNumber": "30267080",
      "Branch": -2,
      "Department": -2,
      "U_Establecimiento": null,
      "U_visualizar_todos_DTE": "N",
      "LastLogoutDate": "2025-03-18",
      "LastLoginTime": "14:31:43"
    }
  ]
}
```

---

### 3.2. Consultar Detalle de un Usuario
- **Método:** `GET`
- **Rutas soportadas:**
  - Por ID numérico: `/api/v1/Users(200)` o `/api/v1/Users/200`
  - Por código: `/api/v1/Users('creditos08')`
- **Parámetro Opcional:** `?includePermissions=true` *(para consultar la lista de permisos USR3 sólo cuando el usuario abre la pestaña de permisos)*.

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Usuario recuperado exitosamente.",
  "data": {
    "InternalKey": 200,
    "UserCode": "creditos08",
    "UserName": "Gabriela Acabal",
    "Superuser": "tNO",
    "MobileUser": "tNO",
    "Locked": "tNO",
    "Defaults": null,
    "WindowsUserName": null,
    "EmployeeId": null,
    "eMail": "gacabal@milesimo.com.gt",
    "MobilePhoneNumber": "30267080",
    "MobileDeviceId": "7JU4MJ5MUH8YJ9RUZU6J1RAM9Y/1",
    "Remarks": "Usuario del área de créditos",
    "Branch": 1,
    "Department": 2,
    "Group": "ug_Regular",
    "PasswordNeverExpires": "tYES",
    "ChangePasswordNextLogon": "tNO",
    "LanguageCode": "ln_Spanish_La",
    "U_Establecimiento": "001",
    "U_visualizar_todos_DTE": "N",
    "U_MultiEst": "N",
    "U_MensajeEnvioDocto": "N",
    "U_ActivarLog": "N",
    "U_ActivarXML": "N",
    "LastLogoutDate": "2025-03-18",
    "LastLoginTime": "14:31:43",
    "LastLogoutTime": "11:35:25",
    "LastPasswordChangeTime": "15:18:38",
    "LastPasswordChangedBy": "creditos"
  }
}
```

---

### 3.3. Crear un Nuevo Usuario
- **Método:** `POST`
- **Ruta:** `/api/v1/Users` (o `/b1s/v1/Users`)
- **Body JSON:**
```json
{
  "UserCode": "operador01",
  "UserName": "Operador de Facturación",
  "UserPassword": "PasswordInicial2026!",
  "Superuser": "tNO",
  "Locked": "tNO",
  "eMail": "operador01@empresa.com",
  "MobilePhoneNumber": "55551234",
  "Branch": 1,
  "Department": 2,
  "Defaults": "DEF_VENTAS",
  "U_Establecimiento": "001",
  "U_visualizar_todos_DTE": "N",
  "U_MultiEst": "N",
  "U_MensajeEnvioDocto": "Y",
  "U_ActivarLog": "Y",
  "U_ActivarXML": "Y"
}
```

**Respuesta Exitosa (201 Created):**
```json
{
  "success": true,
  "message": "Usuario 'operador01' creado exitosamente en SAP con InternalKey #205.",
  "data": {
    "InternalKey": 205,
    "UserCode": "operador01"
  }
}
```

---

### 3.4. Actualizar Usuario / Modificar Perfil / Bloquear
- **Método:** `PATCH` / `PUT`
- **Ruta:** `/api/v1/Users(200)` (o `/b1s/v1/Users(200)`)
- **Body JSON (enviar solo los campos a modificar):**
```json
{
  "UserName": "Gabriela Acabal - Actualizado",
  "Locked": "tYES",
  "eMail": "gacabal_nuevo@milesimo.com.gt",
  "MobilePhoneNumber": "30267099",
  "Branch": 1,
  "Department": 2,
  "U_Establecimiento": "002",
  "U_visualizar_todos_DTE": "Y"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Usuario #200 actualizado exitosamente en SAP.",
  "data": {
    "InternalKey": 200
  }
}
```

---

### 3.5. Cambiar o Restablecer Contraseña (Admin Reset)
Permite restablecer la contraseña de un usuario por su código (`UserCode`) o por su `InternalKey`, y opcionalmente forzar al usuario a modificar la clave en su siguiente conexión a SAP.

- **Métodos y Rutas:**
  - Por código (Recomendado para multi-empresa): `POST /api/v1/Users('{userCode}')/ChangePassword`
  - Por InternalKey: `POST /api/v1/Users({id})/ChangePassword`
- **Headers:**
  ```http
  X-Api-Key: [BridgeApiKey]
  X-Company-DB: [NombreBaseDatosSAP]
  X-Audit-User: [UsuarioQueEjecuta]
  X-Audit-App: PortalAdminUsuarios
  Content-Type: application/json
  ```
- **Body JSON (Completo / Con cambio obligatorio en próximo logon):**
```json
{
  "NewPassword": "NuevaPasswordSegura2026!",
  "ChangePasswordNextLogon": "tYES",
  "PasswordNeverExpires": "tNO"
}
```
> **Nota:** `ChangePasswordNextLogon` y `PasswordNeverExpires` son opcionales. Aceptan `"tYES"` / `"tNO"`, `"Y"` / `"N"`, o `"true"` / `"false"`.

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "message": "Contraseña del usuario 'creditos08' cambiada exitosamente en SAP.",
  "data": {
    "UserCode": "creditos08",
    "InternalKey": 200
  }
}
```

---

## 🔮 4. Operaciones GraphQL (`/graphql`)

Endpoint GraphQL: `https://<servidor-bridge>/graphql`

### 4.1. Listar Usuarios con Proyecciones y Filtros
```graphql
query GetUsersList($filter: UserFilterDtoInput) {
  users(filter: $filter) {
    internalKey
    userCode
    userName
    email
    mobilePhoneNumber
    superuser
    locked
    branch
    department
    u_Establecimiento
    u_visualizar_todos_DTE
    lastLoginTime
    lastLogoutDate
  }
}
```
**Variables:**
```json
{
  "filter": {
    "search": "creditos",
    "locked": "tNO",
    "page": 1,
    "pageSize": 20
  }
}
```

---

### 4.2. Consultar Detalle Completo de Usuario
```graphql
query GetUserDetail($id: Int!) {
  userById(internalKey: $id, includePermissions: false) {
    internalKey
    userCode
    userName
    email
    mobilePhoneNumber
    mobileDeviceId
    superuser
    locked
    defaults
    branch
    department
    group
    passwordNeverExpires
    languageCode
    u_Establecimiento
    u_visualizar_todos_DTE
    u_MultiEst
    u_MensajeEnvioDocto
    u_ActivarLog
    u_ActivarXML
    lastLoginTime
    lastLogoutDate
    lastLogoutTime
    lastPasswordChangeTime
    lastPasswordChangedBy
  }
}
```

---

### 4.3. Mutación: Crear Usuario
```graphql
mutation CreateNewUser($input: CreateUserDtoInput!) {
  createUser(input: $input) {
    success
    internalKey
    errorMessage
  }
}
```

---

### 4.4. Mutación: Actualizar Usuario
```graphql
mutation UpdateUserData($id: Int!, $input: UpdateUserDtoInput!) {
  updateUser(internalKey: $id, input: $input) {
    success
    internalKey
    errorMessage
  }
}
```

---

### 4.5. Mutación: Cambiar Contraseña
```graphql
mutation ResetPassword($id: Int!, $newPass: String!) {
  changeUserPassword(internalKey: $id, newPassword: $newPass) {
    success
    internalKey
    errorMessage
  }
}
```

---

## ⚠️ 5. Estructura de Manejo de Errores (Error Handling)

Cuando SAP o la API rechazan una operación (código HTTP `400`, `401`, `403`, `404`, `500`), el formato de error devuelto sigue el estándar:

```json
{
  "error": {
    "code": 400,
    "message": {
      "lang": "es-ES",
      "value": "Error SAP (-10): El código de usuario 'operador01' ya existe en la base de datos."
    }
  }
}
```

### Recomendación para el Frontend:
Extraer y mostrar al usuario final el mensaje descriptivo ubicado en `error.message.value`.

---

## 🏢 6. Estrategia y Patrones para Operaciones Masivas Multi-Empresa

Cuando el administrador necesita **crear, actualizar o resetear contraseña a un usuario en múltiples bases de datos de SAP** simultáneamente (ej: `TEST1_SBO_UNOCINCO`, `TEST2_SBO_DOS`, `TEST3_SBO_TRES`), se deben considerar dos aspectos clave de SAP:

1. **`UserCode` es el Identificador Universal:**  
   El código del usuario (ej: `"creditos08"`) es constante en todas las empresas, mientras que el `InternalKey` (`USERID`) puede ser `200` en Empresa A y `145` en Empresa B.  
   👉 **Por ello, utiliza los endpoints por código:** `PATCH /api/v1/Users('{userCode}')` y `POST /api/v1/Users('{userCode}')/ChangePassword`.

2. **Cabecera `X-Company-DB` por Petición:**  
   Basta con cambiar la cabecera `X-Company-DB: <nombre_empresa>` en cada iteración manteniendo la misma `X-Api-Key`.

---

### 💻 Ejemplo de Implementación en Frontend (TypeScript / React / Vue / Angular)

A continuación un servicio frontend listo para ejecutar la replicación multi-empresa con reporte granular y progreso en tiempo real usando `Promise.allSettled`:

```typescript
export interface MultiCompanyResult {
  companyDb: string;
  status: 'SUCCESS' | 'ERROR';
  internalKey?: number;
  errorMessage?: string;
}

export class UserMultiCompanyService {
  private baseUrl = 'https://bridge-sap.tuempresa.com/api/v1';
  private apiKey = 'sk_live_85067IE0NV8luY1lcCYWgi0mcdohCAfuLbt-pTHGIJU';

  /**
   * Crea un usuario en una lista de empresas seleccionadas
   */
  async createUserInCompanies(
    companies: string[],
    userData: any,
    operatorUser: string,
    onProgress?: (completed: number, total: number) => void
  ): Promise<MultiCompanyResult[]> {
    let completedCount = 0;

    const tasks = companies.map(async (companyDb): Promise<MultiCompanyResult> => {
      try {
        const response = await fetch(`${this.baseUrl}/Users`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Api-Key': this.apiKey,
            'X-Company-DB': companyDb,
            'X-Audit-User': operatorUser,
            'X-Audit-App': 'PortalAdminUsuarios'
          },
          body: JSON.stringify(userData)
        });

        const data = await response.json();

        completedCount++;
        onProgress?.(completedCount, companies.length);

        if (!response.ok) {
          return {
            companyDb,
            status: 'ERROR',
            errorMessage: data?.error?.message?.value || 'Error al crear en SAP'
          };
        }

        return {
          companyDb,
          status: 'SUCCESS',
          internalKey: data?.data?.InternalKey
        };
      } catch (err: any) {
        completedCount++;
        onProgress?.(completedCount, companies.length);
        return {
          companyDb,
          status: 'ERROR',
          errorMessage: err?.message || 'Error de conexión'
        };
      }
    });

    return await Promise.all(tasks);
  }

  /**
   * Actualiza o bloquea un usuario por su UserCode en múltiples empresas
   */
  async updateUserInCompanies(
    companies: string[],
    userCode: string,
    updateData: any,
    operatorUser: string,
    onProgress?: (completed: number, total: number) => void
  ): Promise<MultiCompanyResult[]> {
    let completedCount = 0;

    const tasks = companies.map(async (companyDb): Promise<MultiCompanyResult> => {
      try {
        const response = await fetch(`${this.baseUrl}/Users('${encodeURIComponent(userCode)}')`, {
          method: 'PATCH',
          headers: {
            'Content-Type': 'application/json',
            'X-Api-Key': this.apiKey,
            'X-Company-DB': companyDb,
            'X-Audit-User': operatorUser
          },
          body: JSON.stringify(updateData)
        });

        const data = await response.json();

        completedCount++;
        onProgress?.(completedCount, companies.length);

        if (!response.ok) {
          return {
            companyDb,
            status: 'ERROR',
            errorMessage: data?.error?.message?.value || 'Error al actualizar en SAP'
          };
        }

        return {
          companyDb,
          status: 'SUCCESS',
          internalKey: data?.data?.InternalKey
        };
      } catch (err: any) {
        completedCount++;
        onProgress?.(completedCount, companies.length);
        return {
          companyDb,
          status: 'ERROR',
          errorMessage: err?.message || 'Error de red'
        };
      }
    });

    return await Promise.all(tasks);
  }
}
```

---

### 🎨 Sugerencia de Interfaz de Usuario (UI) en el Frontend

Para una experiencia de usuario óptima:
1. **Selector de Empresas (Checkboxes / Multi-select):** Permitir al admin marcar "Seleccionar Todas" o elegir empresas específicas (ej: `[x] Guatemala`, `[x] El Salvador`, `[x] Honduras`).
2. **Modal con Barra de Progreso:** Al presionar "Guardar en Empresas Seleccionadas", mostrar el estado en tiempo real (ej: `Procesando 2 de 5 empresas...`).
3. **Tabla de Resumen Post-Proceso:**
   | Empresa | Estado | Detalle |
   | :--- | :---: | :--- |
   | `TEST1_SBO_UNOCINCO` | 🟢 Éxito | Usuario creado (ID #205) |
   | `SBO_ELSALVADOR` | 🟢 Éxito | Usuario creado (ID #142) |
   | `SBO_HONDURAS` | 🔴 Error | Error SAP (-10): El código 'operador01' ya existe |
   *(Permite un botón "Reintentar en empresas fallidas" si hubo error de conexión).*

