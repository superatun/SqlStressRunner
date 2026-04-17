# Captura de Parámetros en Iteraciones

## Descripción

A partir de esta versión, la aplicación ahora captura y guarda todos los parámetros utilizados en cada iteración del stress test. Esto te permite analizar exactamente qué datos causaron éxitos o fallos en tus pruebas.

## Características

### 1. Almacenamiento de Parámetros

Cada iteración ahora guarda:
- Todos los parámetros usados en los stored procedures
- Los valores específicos de cada parámetro
- Formato JSON para fácil consulta y análisis

### 2. Estructura de Datos

Los parámetros se almacenan en formato JSON en la columna `Parameters` de la tabla `StressTestLog`:

```json
{
  "@idCliente": 12345,
  "@idSolicitud": 67890,
  "@idUsuarioLA": 100,
  "@idEvalCred": 200,
  "@idEtapa": 2,
  "@cDocumentoID": "DNI123456",
  "@idTipoDocumento": 1,
  "@idUsuario": 100
}
```

### 3. Actualización de Base de Datos

#### Opción A: Crear Tablas Nuevas
Si aún no has creado las tablas, ejecuta `DatabaseSchema.sql` que ya incluye la columna `Parameters`.

#### Opción B: Actualizar Tabla Existente
Si ya tienes las tablas creadas, ejecuta el script `DatabaseSchema_AddParameters.sql`:

```sql
USE [TuBaseDeDatos];
GO

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'StressTestLog' 
    AND COLUMN_NAME = 'Parameters'
)
BEGIN
    ALTER TABLE StressTestLog
    ADD Parameters NVARCHAR(MAX) NULL;
    
    PRINT 'Columna Parameters agregada exitosamente.';
END
```

## Consultas Útiles

### Ver Iteraciones Fallidas con Parámetros

```sql
SELECT 
    IterationNumber, 
    ErrorMessage, 
    Parameters, 
    TotalDurationMs, 
    ExecutedAt 
FROM StressTestLog 
WHERE RunId = 'YOUR-GUID-HERE' AND Success = 0
ORDER BY IterationNumber;
```

### Analizar Rendimiento por Éxito/Fallo

```sql
SELECT 
    Success,
    COUNT(*) AS TotalIterations,
    AVG(TotalDurationMs) AS AvgDuration,
    MIN(TotalDurationMs) AS MinDuration,
    MAX(TotalDurationMs) AS MaxDuration
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE'
GROUP BY Success;
```

### Extraer Valores Específicos de Parámetros (SQL Server 2016+)

```sql
SELECT 
    IterationNumber,
    Success,
    TotalDurationMs,
    JSON_VALUE(Parameters, '$.@idCliente') AS idCliente,
    JSON_VALUE(Parameters, '$.@idSolicitud') AS idSolicitud,
    JSON_VALUE(Parameters, '$.@idUsuarioLA') AS idUsuarioLA,
    Parameters AS AllParameters
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE'
ORDER BY IterationNumber;
```

### Buscar Iteraciones con un Parámetro Específico

```sql
SELECT 
    IterationNumber,
    Success,
    TotalDurationMs,
    Parameters
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE'
  AND Parameters LIKE '%"@idCliente":12345%'
ORDER BY IterationNumber;
```

### Comparar Parámetros de Iteraciones Exitosas vs Fallidas

```sql
-- Ver los primeros 10 casos exitosos
SELECT TOP 10 
    IterationNumber,
    TotalDurationMs,
    Parameters
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE' AND Success = 1
ORDER BY IterationNumber;

-- Ver todos los casos fallidos
SELECT 
    IterationNumber,
    TotalDurationMs,
    ErrorMessage,
    Parameters
FROM StressTestLog
WHERE RunId = 'YOUR-GUID-HERE' AND Success = 0
ORDER BY IterationNumber;
```

## Casos de Uso

### 1. Debugging de Errores Específicos

Si una iteración falla, puedes ver exactamente qué parámetros se usaron:

```sql
SELECT 
    IterationNumber,
    ErrorMessage,
    Parameters,
    ExecutedAt
FROM StressTestLog
WHERE RunId = 'TU-RUN-ID' 
  AND Success = 0
  AND IterationNumber = 1234;
```

Luego puedes copiar esos parámetros y ejecutar manualmente el SP para reproducir el error.

### 2. Análisis de Patrones

Identificar si ciertos valores de parámetros causan más errores:

```sql
SELECT 
    JSON_VALUE(Parameters, '$.@idTipoDocumento') AS TipoDocumento,
    COUNT(*) AS Total,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS Fallos,
    AVG(TotalDurationMs) AS AvgDuration
FROM StressTestLog
WHERE RunId = 'TU-RUN-ID'
GROUP BY JSON_VALUE(Parameters, '$.@idTipoDocumento')
ORDER BY Fallos DESC;
```

### 3. Reproducir Iteración Específica

Puedes copiar los parámetros JSON y ejecutar manualmente:

```sql
-- 1. Obtener los parámetros
SELECT Parameters 
FROM StressTestLog 
WHERE RunId = 'TU-RUN-ID' AND IterationNumber = 100;

-- 2. Ejecutar manualmente con esos parámetros
EXEC sp_TuStoredProcedure 
    @idCliente = 12345,
    @idSolicitud = 67890,
    @idUsuarioLA = 100,
    -- ... resto de parámetros
```

## Beneficios

1. **Debugging Preciso**: Sabes exactamente qué datos causaron errores
2. **Reproducibilidad**: Puedes reproducir cualquier iteración manualmente
3. **Análisis de Patrones**: Identifica correlaciones entre parámetros y rendimiento
4. **Auditoría Completa**: Historial completo de todas las pruebas ejecutadas
5. **Optimización Basada en Datos**: Identifica qué tipos de datos son más lentos

## Notas Técnicas

- Los parámetros se serializan usando `System.Text.Json`
- Se almacenan en formato JSON compacto (sin indentación)
- Todos los SPs en una iteración comparten los mismos parámetros base (pueden variar por SP según el mapeo)
- Si un parámetro es `NULL`, se guarda como `null` en el JSON
- La columna `Parameters` es `NVARCHAR(MAX)` para soportar cualquier cantidad de parámetros

## Compatibilidad

- Compatible con versiones anteriores de la base de datos (columna es `NULL`able)
- Si ejecutas en una BD sin la columna `Parameters`, el logging falla silenciosamente (no detiene la prueba)
- Funciona con cualquier cantidad de stored procedures configurados
- Soporta parámetros de cualquier tipo (se serializa el valor como string en JSON)
