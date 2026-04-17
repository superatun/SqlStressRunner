# Instrucciones de Ejecución - SQL Stress Runner

## Requisitos Previos

### Software Necesario
1. **Windows 10/11**
2. **.NET 10 SDK** (o superior)
   - Descargar desde: https://dotnet.microsoft.com/download
3. **Visual Studio 2022** (Community, Professional o Enterprise)
   - O Visual Studio Code con extensiones de C#
4. **SQL Server** (cualquier versión moderna)
   - SQL Server 2019, 2022, Azure SQL Database, etc.

### Verificar Instalación de .NET
```powershell
dotnet --version
# Debe mostrar: 10.x.x o superior
```

## Instalación

### Opción 1: Clonar desde Repositorio
```bash
git clone [URL_DEL_REPOSITORIO]
cd SqlStressRunner
```

### Opción 2: Abrir Proyecto Existente
1. Abre Visual Studio 2022
2. File → Open → Project/Solution
3. Selecciona `SqlStressRunner.csproj`

## Compilación

### Desde Visual Studio
1. Abre la solución en Visual Studio
2. Menu: Build → Rebuild Solution
3. Verifica que no haya errores en Output window

### Desde Línea de Comandos
```powershell
cd SqlStressRunner
dotnet restore
dotnet build --configuration Release
```

### Resultado Esperado
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Configuración de Base de Datos (Opcional)

Si deseas usar la función de logging a base de datos:

### 1. Crear Tablas de Logging
```sql
-- Conecta a tu base de datos SQL Server
-- Ejecuta el script DatabaseSchema.sql
```

### 2. Desde SQL Server Management Studio (SSMS)
1. Conecta a tu servidor SQL
2. Selecciona la base de datos de destino
3. File → Open → `DatabaseSchema.sql`
4. Execute (F5)

### 3. Verificar Creación
```sql
-- Verifica que las tablas existan
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('StressTestLog', 'StressTestLogSummary');
```

## Preparar Stored Procedures de Prueba

### Opción 1: Usar Ejemplos Incluidos
```sql
-- Abre y ejecuta TestExamples.sql en SSMS
-- Esto creará SPs de ejemplo que puedes usar
```

### Opción 2: Usar tus Propias SPs
- Asegúrate de que acepten parámetros
- Deben ser ejecutables múltiples veces

## Ejecución de la Aplicación

### Desde Visual Studio
1. Presiona **F5** (Debug) o **Ctrl+F5** (Sin Debug)
2. La aplicación debe abrirse

### Desde Línea de Comandos
```powershell
cd bin\Release\net10.0-windows
.\SqlStressRunner.exe
```

### Desde Windows Explorer
1. Navega a: `SqlStressRunner\bin\Release\net10.0-windows\`
2. Doble click en `SqlStressRunner.exe`

## Guía de Uso Paso a Paso

### PASO 1: Configurar Conexión

1. Ve a la pestaña **"Connection"**
2. Ingresa los datos de tu servidor:
   ```
   Server: localhost
   Database: YourTestDB
   ```
3. Selecciona el tipo de autenticación:
   - **Windows Authentication**: Marca "Use Integrated Security"
   - **SQL Authentication**: Desmarca y proporciona User/Password
4. Click en **"Test Connection"**
5. Verifica mensaje: "Connection successful!"
6. Click en **"Save Settings"**

### PASO 2: Configurar la Prueba

1. Ve a la pestaña **"Test Configuration"**

2. **Initial Query**: Escribe una query que retorne datos
   ```sql
   SELECT TOP 100 
       CustomerID, 
       ProductID, 
       Quantity 
   FROM Orders
   ```

3. **Stored Procedure 1**: Primera SP a ejecutar
   ```sql
   EXEC ValidateOrder @CustomerID, @ProductID, @Quantity
   ```

4. **Stored Procedure 2**: Segunda SP a ejecutar
   ```sql
   EXEC ProcessOrder @CustomerID
   ```

5. **Configurar Parámetros**:
   - Command Timeout: `30` (segundos)
   - Max Degree of Parallelism: `4` (workers concurrentes)
   - Total Iterations: `1000` (cuántas veces ejecutar)
   - ✅ Recycle Dataset (reutilizar datos cíclicamente)
   - ⬜ Log to Database (opcional)

6. Click en **"Load Initial Data"**
7. Verifica mensaje: "Successfully loaded X rows"

### PASO 3: Mapear Parámetros

1. Ve a la pestaña **"Parameter Mapping"**
2. Verás las columnas de tu query inicial
3. Mapea cada columna a parámetros:

| Column Name | SP1 Parameter Name | SP2 Parameter Name |
|-------------|--------------------|--------------------|
| CustomerID  | @CustomerID        | @CustomerID        |
| ProductID   | @ProductID         |                    |
| Quantity    | @Quantity          |                    |

**Notas**:
- Parámetros deben empezar con `@`
- Dejar vacío si no se usa en ese SP
- Columnas no mapeadas se ignoran

### PASO 4: Ejecutar la Prueba

1. Regresa a **"Test Configuration"**
2. Click en **"Start Test"**
3. Observa:
   - Progress bar avanzando
   - Status message actualizándose
   - "Running: X/1000 iterations"
4. Espera a que complete (o **"Cancel Test"** para detener)

### PASO 5: Ver Resultados

1. La app cambiará automáticamente a **"Results"**
2. Verás métricas como:
   ```
   Run ID: 12345678-1234-1234-1234-123456789012
   Total Duration: 45.23 seconds
   Total Iterations: 1000
   Successful: 995
   Failed: 5
   TPS (Iteration): 22.01
   TPS (Per SP): 44.02
   Avg Latency: 45.42 ms
   P95: 87 ms
   P99: 156 ms
   ```

## Escenarios de Uso Comunes

### Escenario 1: Prueba de Carga Básica
**Objetivo**: Ver cuántos TPS puede manejar tu SP

```
Parallelism: 1 (sin concurrencia)
Iterations: 1000
Recycle: Yes
```

Analiza: TPS, latencia promedio

### Escenario 2: Prueba de Concurrencia
**Objetivo**: Detectar deadlocks o contención

```
Parallelism: 10 (alta concurrencia)
Iterations: 5000
Recycle: Yes
```

Analiza: Errores, P95/P99, duración vs escenario 1

### Escenario 3: Prueba de Estrés
**Objetivo**: Llevar al límite

```
Parallelism: 20
Iterations: 50000
Recycle: Yes
Log to DB: Yes (para análisis posterior)
```

Analiza: Punto de quiebre, errores, degradación

### Escenario 4: Prueba de Datos Únicos
**Objetivo**: Cada iteración usa datos diferentes

```
Parallelism: 4
Iterations: 100 (max rows en dataset)
Recycle: No
```

Útil para: Inserts, validar unicidad

## Troubleshooting

### Error: "Connection failed: Login failed"
**Solución**:
- Verifica usuario/password
- Asegura que el usuario tenga permisos
- Revisa que SQL Server acepte autenticación SQL

### Error: "Initial query failed: Timeout"
**Solución**:
- Aumenta Command Timeout
- Optimiza tu query inicial
- Agrega índices necesarios

### Error durante ejecución: "SP1 failed: Could not find stored procedure"
**Solución**:
- Verifica que el SP existe: `SELECT * FROM sys.procedures WHERE name = 'YourSP'`
- Usa el nombre correcto (sin EXEC si es solo el nombre)
- Verifica esquema: `dbo.YourSP` o `custom.YourSP`

### Errores de parámetros
**Solución**:
- Verifica mapeo en "Parameter Mapping"
- Asegura que los tipos de datos coincidan
- Revisa que los parámetros empiecen con `@`

### La aplicación se congela
**Causa Probable**: Operación sincrónica bloqueando UI
**Solución**: Reportar bug (no debería pasar)

### TPS muy bajo
**Posibles Causas**:
1. Command Timeout muy alto
2. SP real es lento (optimizar)
3. Paralelismo = 1 (aumentar si quieres más TPS)
4. Servidor SQL sobrecargado

### Muchos errores/fallos
**Posibles Causas**:
1. Deadlocks (reduce paralelismo)
2. Constraints violated (datos inválidos)
3. Timeouts (aumenta timeout o optimiza SPs)
4. Conexiones agotadas (aumenta connection pool)

## Análisis de Resultados

### ¿Qué es un buen TPS?
Depende de:
- Complejidad del SP
- Hardware del servidor
- Red (local vs remoto)
- Paralelismo configurado

**Benchmark aproximado**:
- SP simple (SELECT): 1000-5000 TPS
- SP medio (INSERT/UPDATE): 100-1000 TPS
- SP complejo (transacciones): 10-100 TPS

### ¿Cuándo preocuparse por latencia?
- **< 10ms**: Excelente
- **10-50ms**: Bueno
- **50-100ms**: Aceptable
- **100-500ms**: Revisar
- **> 500ms**: Problema serio

### ¿Qué significan P95 y P99?
- **P95**: 95% de requests fueron más rápidos que este valor
- **P99**: 99% de requests fueron más rápidos que este valor
- Si P95/P99 son muy altos: hay outliers problemáticos

### Analizar Logs en DB (si activaste logging)

```sql
-- Ver resumen de todos los runs
SELECT * FROM vw_StressTestSummary
ORDER BY StartTime DESC;

-- Ver detalles de un run específico
SELECT * 
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE'
AND Success = 0  -- Solo fallos
ORDER BY IterationNumber;

-- Distribución de latencias
SELECT 
    CASE 
        WHEN TotalDurationMs < 10 THEN '0-10ms'
        WHEN TotalDurationMs < 50 THEN '10-50ms'
        WHEN TotalDurationMs < 100 THEN '50-100ms'
        WHEN TotalDurationMs < 500 THEN '100-500ms'
        ELSE '500ms+'
    END AS LatencyBucket,
    COUNT(*) AS Count
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE'
GROUP BY 
    CASE 
        WHEN TotalDurationMs < 10 THEN '0-10ms'
        WHEN TotalDurationMs < 50 THEN '10-50ms'
        WHEN TotalDurationMs < 100 THEN '50-100ms'
        WHEN TotalDurationMs < 500 THEN '100-500ms'
        ELSE '500ms+'
    END
ORDER BY Count DESC;
```

## Guardar y Cargar Configuración

### Guardar Configuración Actual
1. Menu: **File → Save Configuration**
2. Se guarda en: `%LocalAppData%\SqlStressRunner\config.json`

### Cargar Configuración Guardada
1. Menu: **File → Load Configuration**
2. Automáticamente carga al iniciar la app

### Compartir Configuración
1. Navega a: `%LocalAppData%\SqlStressRunner\`
2. Copia `config.json`
3. Comparte con tu equipo
4. Otros pueden ponerlo en su carpeta LocalAppData

## Mejores Prácticas

### ✅ DO
- Prueba primero con pocas iteraciones (100)
- Aumenta paralelismo gradualmente
- Usa "Recycle Dataset" para pruebas largas
- Activa logging para análisis detallado
- Guarda configuración después de ajustar

### ❌ DON'T
- No ejecutes 100,000 iteraciones sin probar primero
- No uses Parallelism > CPU cores sin necesidad
- No pruebes en producción
- No ignores errores constantes
- No compares TPS de SPs diferentes directamente

## Siguientes Pasos

Después de completar tu primera prueba:

1. **Optimiza**: Usa los resultados para identificar bottlenecks
2. **Compara**: Ejecuta antes/después de cambios
3. **Docenta**: Guarda los Run IDs importantes
4. **Automatiza**: Considera scripting con la configuración JSON

## Soporte

Si encuentras problemas:
1. Revisa esta guía
2. Revisa `README.md` y `ARCHITECTURE.md`
3. Verifica logs de SQL Server
4. Revisa Output window de Visual Studio para debug info

## Recursos Adicionales

- **DatabaseSchema.sql**: Script para crear tablas de logging
- **TestExamples.sql**: SPs de ejemplo para pruebas
- **config.example.json**: Ejemplo de configuración
- **ARCHITECTURE.md**: Documentación técnica detallada
- **README.md**: Guía general de la aplicación

---

**¡Listo! Ahora estás preparado para ejecutar SQL Stress Runner y analizar el performance de tus stored procedures. 🚀**
