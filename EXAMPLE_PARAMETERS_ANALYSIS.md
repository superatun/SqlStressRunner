# Ejemplo de Uso: Análisis de Parámetros

## Escenario Real

Estás ejecutando un stress test con el siguiente Initial Query:

```sql
SELECT
    c.idCli AS idCliente,
    s.idSolicitud AS idSolicitud,
    s.idUsuario AS idUsuarioLA,
    ec.idEvalCred AS idEvalCred,
    2 AS idEtapa,
    c.cDocumentoID AS cDocumentoID,
    c.idTipoDocumento AS idTipoDocumento,
    s.idUsuario AS idUsuario
FROM dbo.SI_FinSolicitud s
JOIN dbo.SI_FinCliIntervCre cic ON cic.idSolicitud = s.idSolicitud
JOIN dbo.SI_FinCliente c ON c.idCli = cic.idCli
JOIN dbo.SI_FinEvalCred ec ON ec.idSolicitud = s.idSolicitud
WHERE cic.idTipoInterv = 6
  AND c.IdTipoPersona = 1
ORDER BY s.idSolicitud DESC
OFFSET 4400 ROWS
FETCH NEXT 1400 ROWS ONLY;
```

Y tienes configurado este Stored Procedure:

```sql
EXEC sp_ProcesarSolicitud 
    @idCliente,
    @idSolicitud,
    @idUsuarioLA,
    @idEvalCred,
    @idEtapa,
    @cDocumentoID,
    @idTipoDocumento,
    @idUsuario
```

## Mapeo de Parámetros

En la pestaña **Parameter Mapping**:

| Column Name      | sp_ProcesarSolicitud Parameter |
|------------------|--------------------------------|
| idCliente        | @idCliente                     |
| idSolicitud      | @idSolicitud                   |
| idUsuarioLA      | @idUsuarioLA                   |
| idEvalCred       | @idEvalCred                    |
| idEtapa          | @idEtapa                       |
| cDocumentoID     | @cDocumentoID                  |
| idTipoDocumento  | @idTipoDocumento               |
| idUsuario        | @idUsuario                     |

## Ejecución del Test

Ejecutas un stress test con **5000 iteraciones** sobre esas **1400 filas**.

## Resultados en Base de Datos

### Tabla StressTestLog - Ejemplo de 3 iteraciones

| Id | RunId | IterationNumber | Success | TotalDurationMs | ErrorMessage | Parameters |
|----|-------|-----------------|---------|-----------------|--------------|------------|
| 1  | 550e8400-e29b-... | 1 | 1 | 145 | NULL | `{"@idCliente":12345,"@idSolicitud":67890,"@idUsuarioLA":100,"@idEvalCred":200,"@idEtapa":2,"@cDocumentoID":"DNI123456","@idTipoDocumento":1,"@idUsuario":100}` |
| 2  | 550e8400-e29b-... | 2 | 1 | 152 | NULL | `{"@idCliente":12346,"@idSolicitud":67891,"@idUsuarioLA":101,"@idEvalCred":201,"@idEtapa":2,"@cDocumentoID":"DNI123457","@idTipoDocumento":1,"@idUsuario":101}` |
| 3  | 550e8400-e29b-... | 3 | 0 | 1520 | sp_ProcesarSolicitud failed: Timeout | `{"@idCliente":12347,"@idSolicitud":67892,"@idUsuarioLA":102,"@idEvalCred":202,"@idEtapa":2,"@cDocumentoID":"DNI123458","@idTipoDocumento":1,"@idUsuario":102}` |

## Análisis de Errores

### 1. Ver iteraciones fallidas con sus parámetros

```sql
SELECT 
    IterationNumber, 
    ErrorMessage, 
    TotalDurationMs,
    Parameters
FROM StressTestLog 
WHERE RunId = '550e8400-e29b-41d4-a716-446655440000' 
  AND Success = 0
ORDER BY IterationNumber;
```

**Resultado:**
```
IterationNumber | ErrorMessage                              | TotalDurationMs | Parameters
----------------|-------------------------------------------|-----------------|------------
3               | sp_ProcesarSolicitud failed: Timeout      | 1520            | {"@idCliente":12347,"@idSolicitud":67892,...}
157             | sp_ProcesarSolicitud failed: Deadlock     | 3042            | {"@idCliente":13501,"@idSolicitud":70234,...}
892             | sp_ProcesarSolicitud failed: FK violation | 89              | {"@idCliente":15678,"@idSolicitud":75123,...}
```

### 2. Reproducir una iteración fallida manualmente

Copias el JSON de la iteración 3:
```json
{
  "@idCliente":12347,
  "@idSolicitud":67892,
  "@idUsuarioLA":102,
  "@idEvalCred":202,
  "@idEtapa":2,
  "@cDocumentoID":"DNI123458",
  "@idTipoDocumento":1,
  "@idUsuario":102
}
```

Y ejecutas manualmente:
```sql
EXEC sp_ProcesarSolicitud 
    @idCliente = 12347,
    @idSolicitud = 67892,
    @idUsuarioLA = 102,
    @idEvalCred = 202,
    @idEtapa = 2,
    @cDocumentoID = 'DNI123458',
    @idTipoDocumento = 1,
    @idUsuario = 102
```

### 3. Extraer valores específicos con JSON_VALUE

```sql
SELECT 
    IterationNumber,
    Success,
    TotalDurationMs,
    JSON_VALUE(Parameters, '$.@idCliente') AS idCliente,
    JSON_VALUE(Parameters, '$.@idSolicitud') AS idSolicitud,
    JSON_VALUE(Parameters, '$.@cDocumentoID') AS DocumentoID,
    ErrorMessage
FROM StressTestLog
WHERE RunId = '550e8400-e29b-41d4-a716-446655440000'
  AND Success = 0
ORDER BY TotalDurationMs DESC;
```

**Resultado:**
```
IterationNumber | Success | TotalDurationMs | idCliente | idSolicitud | DocumentoID | ErrorMessage
----------------|---------|-----------------|-----------|-------------|-------------|-------------
157             | 0       | 3042            | 13501     | 70234       | DNI789012   | Deadlock
3               | 0       | 1520            | 12347     | 67892       | DNI123458   | Timeout
892             | 0       | 89              | 15678     | 75123       | DNI456789   | FK violation
```

### 4. Analizar patrones - ¿Hay clientes específicos que fallan más?

```sql
SELECT 
    JSON_VALUE(Parameters, '$.@idCliente') AS idCliente,
    COUNT(*) AS TotalIterations,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS Fallos,
    AVG(TotalDurationMs) AS AvgDuration,
    MAX(TotalDurationMs) AS MaxDuration
FROM StressTestLog
WHERE RunId = '550e8400-e29b-41d4-a716-446655440000'
GROUP BY JSON_VALUE(Parameters, '$.@idCliente')
HAVING SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) > 0
ORDER BY Fallos DESC;
```

### 5. Buscar todas las iteraciones con un documento específico

```sql
SELECT 
    IterationNumber,
    Success,
    TotalDurationMs,
    ErrorMessage,
    Parameters
FROM StressTestLog
WHERE RunId = '550e8400-e29b-41d4-a716-446655440000'
  AND Parameters LIKE '%"@cDocumentoID":"DNI123458"%'
ORDER BY IterationNumber;
```

## Beneficios Prácticos

1. **Debugging**: Sabes exactamente qué datos causaron el timeout en la iteración 3
2. **Reproducibilidad**: Puedes ejecutar manualmente esos mismos parámetros
3. **Patrones**: Descubres que ciertos `idCliente` causan más errores
4. **Optimización**: Identificas que documentos con cierto patrón son más lentos
5. **Reporte**: Puedes generar reportes detallados con datos específicos que fallaron

## Integración con el Botón Copy

1. Ejecutas tu test
2. Ves el RunId en la pestaña Results: `550e8400-e29b-41d4-a716-446655440000`
3. Haces clic en el botón **📋 Copy** 
4. Pegas el RunId en tus queries SQL para analizar los parámetros

## Workflow Completo

```
1. Ejecutar Test → Stress Test se ejecuta con 5000 iteraciones
2. Ver Resultados → RunId: 550e8400-e29b-41d4-a716-446655440000, 48 fallos
3. Copy RunId → Clic en 📋 Copy
4. Consultar BD → Pegar RunId en query para ver iteraciones fallidas
5. Analizar Parámetros → Ver qué datos específicos causaron errores
6. Reproducir → Ejecutar manualmente con esos parámetros
7. Debuggear → Encontrar y corregir el problema en el SP
8. Re-ejecutar → Stress test nuevamente para validar fix
```

## Notas Importantes

- Los parámetros se capturan **antes** de ejecutar el SP
- Si un parámetro es NULL en el DataTable, se guarda como `null` en el JSON
- Los valores numéricos se guardan sin comillas: `"@idCliente":12345`
- Los strings se guardan con comillas: `"@cDocumentoID":"DNI123456"`
- Si tienes múltiples SPs, todos comparten la misma captura de parámetros base (pueden variar según el mapeo)
