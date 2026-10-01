# 🏢 Guía de Autenticación, Catálogo de Empresas y Headers Seguros para Frontend

Esta documentación está dirigida a los desarrolladores y agentes de Inteligencia Artificial que integran aplicaciones cliente o interfaces frontend con **SapDiApi.Bridge**.

---

## 📌 1. Resumen del Cambio de Seguridad

Para proteger la infraestructura interna y **no exponer los nombres físicos de las bases de datos o esquemas de SAP HANA** (`dbName`, ej: `SBO_EMPRESA_PROD_2026`) a clientes externos y terceros:

1. Se ha implementado un **Catálogo Maestro de Empresas** con almacenamiento local SQLite y resolución en **memoria RAM (0 ms de latencia)**.
2. Cada sociedad SAP cuenta ahora con:
   - **`CompanyId` (Numérico):** `1`, `2`, `42`...
   - **`CompanyCode` (Alfanumérico / Slug):** `"DIST_NORTE"`, `"SERV_CENTRAL"`...
3. Las peticiones a la API ahora pueden enviar **`X-Company-Id`** o **`X-Company-Code`** en lugar del nombre físico de la base de datos.

> 💡 **Compatibilidad hacia atrás:** Si una aplicación existente sigue enviando `X-Company-DB: [NombreReal]`, el Bridge continuará procesándolo de forma transparente.

---

## 🔑 2. Encabezados HTTP para Todas las Peticiones

En cada petición a los endpoints de negocio (`Users`, `BusinessPartners`, `Drafts`, `ApprovalRequests`), el frontend debe incluir:

```http
X-Api-Key: [Tu_Bridge_ApiKey]
X-Company-Id: 1
X-Audit-User: operador_frontend
X-Audit-App: PortalWebAutorizaciones
Content-Type: application/json
```

### Encabezados de Empresa Soportados (por orden de prioridad):

| Encabezado | Ejemplo | Descripción |
| :--- | :--- | :--- |
| **`X-Company-Id`** (Recomendado) | `1` o `42` | ID numérico público de la sociedad SAP. |
| **`X-Company-Code`** | `"DIST_NORTE"` | Código nemotécnico o alias de la sociedad. |
| **`X-Company-DB`** *(Legacy)* | `"SBO_PROD_2026"` | Nombre físico de la base de datos (compatibilidad). |

*(También es soportado enviarlo por Query Param: `?companyId=1` o `?companyCode=DIST_NORTE`)*.

---

## 🚀 3. Endpoints del Catálogo de Empresas

### 3.1. Listar Sociedades SAP Disponibles (Para Dropdowns y Selectores)
- **Método:** `GET`
- **Ruta:** `/api/v1/Companies`
- **Query Params:** `onlyActive=true` (opcional, default `true`)
- **Headers Requeridos:** `X-Api-Key: [BridgeApiKey]`

#### Respuesta Exitosa (200 OK):
```json
{
  "success": true,
  "message": "Se obtuvieron 52 sociedades SAP disponibles.",
  "data": [
    {
      "CompanyId": 1,
      "CompanyCode": "DIST_NORTE",
      "CompanyName": "Distribuidora del Norte S.A. de C.V.",
      "Localization": "SV",
      "IsActive": true
    },
    {
      "CompanyId": 2,
      "CompanyCode": "SERV_CENTRAL",
      "CompanyName": "Servicios Centrales de Occidente",
      "Localization": "GT",
      "IsActive": true
    }
  ],
  "errors": null
}
```

---

### 3.2. Obtener Empresa por ID o Código
- **Método:** `GET`
- **Ruta:** `/api/v1/Companies/1` o `/api/v1/Companies/DIST_NORTE`
- **Headers Requeridos:** `X-Api-Key: [BridgeApiKey]`

#### Respuesta Exitosa (200 OK):
```json
{
  "success": true,
  "message": "Sociedad 'DIST_NORTE' encontrada.",
  "data": {
    "CompanyId": 1,
    "CompanyCode": "DIST_NORTE",
    "CompanyName": "Distribuidora del Norte S.A. de C.V.",
    "Localization": "SV",
    "IsActive": true
  },
  "errors": null
}
```

---

### 3.3. Sincronizar Catálogo Automáticamente desde SAP (Admin)
Consulta la tabla maestra `SBOCOMMON.SRGC` en SAP, registra nuevas sociedades y actualiza las existentes.
- **Método:** `POST`
- **Ruta:** `/api/v1/Companies/sync`
- **Headers:** `X-Api-Key: [BridgeApiKey]`

#### Respuesta Exitosa (200 OK):
```json
{
  "success": true,
  "message": "Sincronización finalizada con éxito: 52 procesadas, 2 agregadas, 50 actualizadas.",
  "data": {
    "TotalProcessed": 52,
    "NewAdded": 2,
    "Updated": 50
  }
}
```

---

### 3.4. Recargar Caché en Caliente en Memoria RAM (Admin)
- **Método:** `POST`
- **Ruta:** `/api/v1/Companies/reload`
- **Headers:** `X-Api-Key: [BridgeApiKey]`

---

## 💻 4. Ejemplos de Consumo en JavaScript / Frontend

### A) Llenar un `<select>` de empresas al cargar la pantalla:
```javascript
async function cargarSelectorEmpresas() {
  const response = await fetch('/api/v1/Companies', {
    headers: {
      'X-Api-Key': API_KEY
    }
  });
  const result = await response.json();

  if (result.success) {
    const select = document.getElementById('cboEmpresas');
    select.innerHTML = '';
    result.data.forEach(emp => {
      const option = document.createElement('option');
      option.value = emp.CompanyId; // Guardamos el ID numérico
      option.textContent = `[${emp.CompanyCode}] ${emp.CompanyName}`;
      select.appendChild(option);
    });
  }
}
```

### B) Cambiar contraseña de un usuario usando `X-Company-Id`:
```javascript
async function cambiarPasswordUsuario(userCode, nuevaPassword, forzarCambio, companyId) {
  const payload = {
    NewPassword: nuevaPassword,
    ChangePasswordNextLogon: forzarCambio ? "tYES" : "tNO"
  };

  const response = await fetch(`/api/v1/Users('${userCode}')/ChangePassword`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY,
      'X-Company-Id': companyId, // Ej: 1 o 42
      'X-Audit-User': 'admin_portal',
      'X-Audit-App': 'PortalUsuarios'
    },
    body: JSON.stringify(payload)
  });

  return await response.json();
}
```

---

## 🛡️ 5. Control de Acceso por `ApiKey` (Seguridad para Terceros)

En `appsettings.json`, los administradores pueden restringir a qué empresas tiene acceso cada cliente tercero utilizando `CompanyId`, `CompanyCode` o `*`:

```json
{
  "ApiKeyAuth": {
    "Clients": [
      {
        "Id": "proveedor-externo-logistica",
        "Name": "Cliente Logística Terceros",
        "ApiKey": "key_logistica_secure_2026_xyz...",
        "IsActive": true,
        "AllowedCompanies": ["1", "5", "DIST_NORTE"]
      }
    ]
  }
}
```

Si el cliente intenta enviar `X-Company-Id: 2` para una empresa a la que no tiene permiso, `BridgeSap` rechazará la petición inmediatamente con **`403 Forbidden`** en memoria, protegiendo las demás bases de datos de SAP.
