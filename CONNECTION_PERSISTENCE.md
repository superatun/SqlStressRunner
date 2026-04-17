# Persistencia de Configuración de Conexión

## Descripción

La aplicación ahora **guarda automáticamente** la configuración de conexión a la base de datos cuando presionas el botón **"Save Settings"** en la pestaña de **Connection**. La configuración se mantiene entre reinicios de la aplicación.

## Configuración que se Guarda

✅ **Se guarda automáticamente:**
- Servidor (Server)
- Base de datos (Database)
- Nombre de usuario (Username)
- Tipo de autenticación (Windows Auth / SQL Auth)
- Trust Server Certificate
- Connection Timeout

❌ **NO se guarda (por seguridad):**
- **Contraseña (Password)** - Debes ingresarla cada vez que inicies la aplicación

## Cómo Funciona

### 1. Al Iniciar la Aplicación

La aplicación automáticamente:
- Carga la configuración guardada desde `%LocalAppData%\SqlStressRunner\config.json`
- Rellena los campos de conexión con los valores guardados
- El campo **Password** siempre estará vacío por seguridad

### 2. Al Configurar la Conexión

1. Ve a la pestaña **Connection**
2. Ingresa o modifica:
   - Server: `10.5.81.142\DESARROLLO`
   - Database: `SI_BDDev_CorApp1_04`
   - Username: `arodriguezf` (si usas SQL Auth)
   - Password: `tu-contraseña` (debes ingresarla)
   - Trust Server Certificate: ✓ (si es necesario)

3. Presiona **"Test Connection"** para verificar

4. Presiona **"Save Settings"**
   - La configuración se guarda automáticamente
   - Verás un mensaje: _"Connection settings saved! Note: Password is not saved..."_

### 3. Al Reiniciar la Aplicación

1. Abre la aplicación nuevamente
2. La configuración de conexión ya está cargada
3. **Solo necesitas ingresar la contraseña** (si usas SQL Auth)
4. Presiona **"Test Connection"** para verificar
5. Continúa con tu trabajo

## Ubicación del Archivo de Configuración

La configuración se guarda en:

```
C:\Users\[TuUsuario]\AppData\Local\SqlStressRunner\config.json
```

### Ejemplo del archivo config.json

```json
{
  "ConnectionSettings": {
    "Server": "10.5.81.142\\DESARROLLO",
    "Database": "SI_BDDev_CorApp1_04",
    "Username": "arodriguezf",
    "UseIntegratedSecurity": false,
    "TrustServerCertificate": true,
    "ConnectionTimeout": 30
  },
  "TestSettings": {
    "TotalIterations": 5000,
    "MaxDegreeOfParallelism": 10,
    "CommandTimeout": 300,
    ...
  },
  "ParameterMappings": [
    ...
  ]
}
```

**Nota:** Observa que `Password` **no aparece** en el JSON por seguridad.

## Seguridad

### ¿Por qué NO se guarda la contraseña?

1. **Seguridad**: Almacenar contraseñas en texto plano es una mala práctica
2. **Cumplimiento**: Muchas organizaciones prohíben guardar credenciales
3. **Auditoría**: Facilita el cumplimiento de políticas de seguridad
4. **Protección**: Evita que otros usuarios con acceso al equipo vean la contraseña

### Recomendaciones de Seguridad

✅ **Usa Windows Authentication cuando sea posible**
   - No requiere contraseña
   - Más seguro
   - Se autentica con tu usuario de Windows

✅ **Si usas SQL Authentication:**
   - Ingresa la contraseña cada vez
   - No la compartas
   - Usa contraseñas robustas
   - Considera usar un administrador de contraseñas

## Workflow Recomendado

### Primera Vez (Configuración Inicial)

```
1. Abrir aplicación
2. Ir a pestaña "Connection"
3. Configurar:
   - Server: 10.5.81.142\DESARROLLO
   - Database: SI_BDDev_CorApp1_04
   - Username: arodriguezf
   - Password: [tu-contraseña]
   - ✓ Trust Server Certificate
4. Test Connection
5. Save Settings ← Configuración guardada permanentemente
```

### Usos Posteriores (Rápido)

```
1. Abrir aplicación ← Configuración ya cargada
2. Ir a pestaña "Connection"
3. Ingresar solo: Password: [tu-contraseña]
4. Test Connection
5. ¡Listo para trabajar!
```

## Configuración Completa (Todo se Guarda)

Además de la configuración de conexión, también se guarda automáticamente:

### Test Settings
- Initial Query
- Stored Procedures configurados
- Total Iterations
- Max Degree of Parallelism
- Command Timeout
- Recycle Dataset
- Log to Database

### Parameter Mappings
- Mapeo completo de columnas a parámetros de SPs

Todos estos se guardan al presionar **"Save Configuration"** en el menú File o con `Ctrl+S`.

## Solución de Problemas

### La configuración no se carga al iniciar

1. Verifica que el archivo existe:
   ```
   %LocalAppData%\SqlStressRunner\config.json
   ```

2. Verifica que el JSON es válido (abre con Notepad++)

3. Revisa el Output de Debug en Visual Studio para errores

### ¿Qué hacer si cambio de servidor/base de datos?

1. Ve a "Connection"
2. Modifica los campos necesarios
3. Test Connection
4. Save Settings ← Sobreescribe la configuración anterior

### ¿Puedo compartir mi configuración con un compañero?

Sí, pero **ten cuidado**:

✅ **Puedes compartir:**
- config.json (no contiene contraseñas)
- Solo asegúrate de que tu compañero tenga acceso a la misma BD

❌ **NO compartas:**
- Tu contraseña de SQL
- Credenciales de Windows

### ¿Puedo tener múltiples configuraciones?

Actualmente no hay soporte nativo, pero puedes:

1. Copiar `config.json` con diferentes nombres:
   - `config_DEV.json`
   - `config_QA.json`
   - `config_PROD.json`

2. Renombrar el que necesites a `config.json`

3. Reiniciar la aplicación

## Flujo de Auto-Save

```
┌─────────────────────────────────────┐
│  Usuario presiona "Save Settings"  │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ Evento ConnectionSettingsChanged    │
│ se dispara con los settings         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ Auto-save a config.json             │
│ (Password excluida por [JsonIgnore])│
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ Configuración persistida             │
│ Lista para próximo reinicio         │
└─────────────────────────────────────┘
```

## Compatibilidad

- ✅ Compatible con versiones anteriores
- ✅ Si no existe config.json, se crea automáticamente
- ✅ Si falta algún campo, usa valores por defecto
- ✅ Migración automática de configuraciones antiguas

## Notas Técnicas

- La contraseña se marca con `[JsonIgnore]` en `DatabaseConnectionSettings`
- El archivo se guarda en formato JSON indentado (fácil de leer)
- La carpeta `%LocalAppData%\SqlStressRunner` se crea automáticamente si no existe
- Los errores de carga/guardado se escriben en el Output de Debug pero no detienen la aplicación
