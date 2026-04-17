# Arquitectura de SQL Stress Runner

## Visión General

SQL Stress Runner es una aplicación WPF que implementa el patrón MVVM (Model-View-ViewModel) para mantener una separación clara entre la interfaz de usuario y la lógica de negocio.

## Principios de Diseño

1. **Separación de Responsabilidades**: Cada capa tiene una responsabilidad específica
2. **Async/Await**: Todas las operaciones de I/O son asíncronas
3. **Thread Safety**: Uso de estructuras thread-safe para operaciones concurrentes
4. **No Bloquear UI**: La interfaz permanece responsive durante operaciones largas
5. **Testeable**: Servicios independientes fáciles de probar

## Capas de la Aplicación

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                    │
│                     (Views - XAML)                       │
├─────────────────────────────────────────────────────────┤
│                    VIEWMODEL LAYER                       │
│              (ViewModels - Binding Logic)                │
├─────────────────────────────────────────────────────────┤
│                    SERVICE LAYER                         │
│              (Business Logic & Orchestration)            │
├─────────────────────────────────────────────────────────┤
│                     DATA LAYER                           │
│           (ADO.NET - Microsoft.Data.SqlClient)           │
└─────────────────────────────────────────────────────────┘
```

## Componentes Principales

### 1. Models (Modelos de Datos)

**Propósito**: Representar datos y estado de la aplicación

- `DatabaseConnectionSettings`: Configuración de conexión a SQL Server
- `StressTestSettings`: Configuración de la prueba de estrés
- `ParameterMapping`: Mapeo de columnas a parámetros
- `IterationResult`: Resultado de una iteración individual
- `MetricsSummary`: Métricas agregadas del test
- `TestRunState`: Estado de ejecución del test
- `AppConfiguration`: Configuración persistente de la app

**Características**:
- POCOs (Plain Old CLR Objects)
- Sin lógica de negocio
- Serializables a JSON

### 2. ViewModels

**Propósito**: Intermediar entre Views y Services, manejar lógica de presentación

#### MainViewModel
- Orquesta los ViewModels hijos
- Maneja navegación entre tabs
- Coordina eventos entre ViewModels
- Persiste/carga configuración

#### ConnectionViewModel
- Gestiona credenciales de conexión
- Valida y prueba conexión
- Notifica cambios a otros ViewModels

#### TestConfigurationViewModel
- Configura parámetros de la prueba
- Carga dataset inicial
- Inicia/cancela ejecución del test
- Reporta progreso

#### ParameterMappingViewModel
- Gestiona mapeo de parámetros
- Inicializa mappings desde dataset
- Valida mappings

#### ResultsViewModel
- Muestra métricas calculadas
- Formatea resultados para UI
- Puede mostrar detalles de iteraciones

**Características**:
- Heredan de `ViewModelBase` (INotifyPropertyChanged)
- Usan `RelayCommand` y `AsyncRelayCommand`
- No tienen referencias directas a Views
- Operaciones asíncronas para no bloquear UI

### 3. Services (Capa de Servicios)

**Propósito**: Encapsular lógica de negocio y acceso a datos

#### SqlConnectionFactory
```csharp
Responsabilidad: Crear conexiones SQL configuradas
Input: DatabaseConnectionSettings
Output: SqlConnection
Thread-Safe: Sí (cada llamada crea nueva conexión)
```

#### ConnectionTestService
```csharp
Responsabilidad: Probar conectividad a SQL Server
Método Principal: TestConnectionAsync()
Retorno: (bool success, string message)
```

#### InitialDataLoaderService
```csharp
Responsabilidad: Cargar dataset inicial
Método Principal: LoadDataAsync(query, timeout)
Retorno: DataTable con datos de prueba
```

#### StoredProcedureExecutionService
```csharp
Responsabilidad: Ejecutar SP con parámetros
Método Principal: ExecuteAsync(sql, parameters, timeout)
Retorno: (bool success, long durationMs, string? error)
Características:
  - Crea nueva conexión por llamada
  - Mide tiempo de ejecución
  - Maneja errores gracefully
```

#### StressTestRunnerService
```csharp
Responsabilidad: Orquestar ejecución del test de estrés
Método Principal: RunTestAsync(settings, dataset, mappings, progress, token)
Características:
  - Usa Parallel.ForEachAsync para concurrencia
  - Thread-safe con ConcurrentBag
  - Soporta CancellationToken
  - Reporta progreso vía IProgress<T>
  - No comparte conexiones entre threads
```

#### MetricsService
```csharp
Responsabilidad: Calcular métricas agregadas
Método Principal: CalculateMetrics(results, startTime, endTime)
Métricas Calculadas:
  - TPS (Transactions Per Second)
  - Latencias (avg, min, max, P95, P99)
  - Estadísticas de éxito/fallo
```

#### ResultLoggingService
```csharp
Responsabilidad: Persistir resultados en SQL Server
Métodos:
  - LogIterationAsync(): Log individual de iteración
  - LogSummaryAsync(): Log del resumen
Características:
  - Opcional (solo si LogToDatabase = true)
  - No falla el test si logging falla
```

### 4. Infrastructure

#### ViewModelBase
```csharp
Implementa: INotifyPropertyChanged
Métodos:
  - OnPropertyChanged()
  - SetProperty<T>()
Propósito: Base para todos los ViewModels
```

#### InverseBoolConverter
```csharp
Implementa: IValueConverter
Propósito: Invertir bool para bindings (ej: disabled cuando checked)
```

### 5. Commands

#### RelayCommand
```csharp
Implementa: ICommand
Propósito: Ejecutar Action síncrona
Características:
  - Soporta CanExecute
  - Integrado con CommandManager
```

#### AsyncRelayCommand
```csharp
Implementa: ICommand
Propósito: Ejecutar Func<Task> asíncrona
Características:
  - Previene ejecución concurrente
  - Actualiza CanExecute durante ejecución
```

### 6. Helpers

#### ConfigurationHelper
```csharp
Propósito: Serializar/deserializar configuración
Ubicación: %LocalAppData%\SqlStressRunner\config.json
Formato: JSON
```

## Flujo de Datos

### Flujo de Conexión
```
User Input (View)
  → ConnectionViewModel
  → ConnectionTestService
  → SqlConnection.OpenAsync()
  → Result → ConnectionViewModel
  → Update UI
```

### Flujo de Carga de Datos
```
User clicks "Load Initial Data"
  → TestConfigurationViewModel.LoadInitialDataAsync()
  → InitialDataLoaderService.LoadDataAsync()
  → SqlCommand.ExecuteReader()
  → DataTable filled
  → Event raised: InitialDataLoaded
  → ParameterMappingViewModel.InitializeMappings()
  → UI updated with column names
```

### Flujo de Ejecución de Test
```
User clicks "Start Test"
  → TestConfigurationViewModel.StartTestAsync()
  → Create StressTestRunnerService
  → RunTestAsync() with Progress<T> and CancellationToken
  
  For each iteration (Parallel.ForEachAsync):
    → Create new SqlConnection (from pool)
    → Build parameters from DataRow
    → Execute SP1
    → If success: Execute SP2
    → Store IterationResult in ConcurrentBag
    → Report progress to UI thread
    → Close connection (return to pool)
  
  After all iterations:
    → MetricsService.CalculateMetrics()
    → Optional: ResultLoggingService.LogSummaryAsync()
    → Event raised: TestCompleted
    → ResultsViewModel.UpdateResults()
    → UI switches to Results tab
```

## Concurrencia y Thread Safety

### Estrategia de Conexiones
- **NO** se comparte una sola conexión
- Cada iteración crea su propia `SqlConnection`
- Aprovecha SQL Server Connection Pooling
- Evita contención de locks

### Thread Safety
```csharp
// Uso de ConcurrentBag para resultados
var results = new ConcurrentBag<IterationResult>();

// Parallel.ForEachAsync con MaxDegreeOfParallelism
var parallelOptions = new ParallelOptions
{
    MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
    CancellationToken = cancellationToken
};

// Interlocked para contador atómico
var current = Interlocked.Increment(ref iterationCounter);
```

### Progress Reporting
```csharp
// IProgress<T> marshals automáticamente al UI thread
var progress = new Progress<(int, int, TestRunState)>(p =>
{
    // Este código se ejecuta en UI thread
    CurrentIteration = p.current;
    StatusMessage = $"Running: {p.current}/{p.total}";
});
```

## Cálculo de Métricas

### TPS (Transactions Per Second)
```
TPS por Iteración = Iteraciones Exitosas / Tiempo Total (seg)
TPS por SP = (Iteraciones Exitosas × 2) / Tiempo Total (seg)
```

### Percentiles (P95, P99)
```csharp
private long CalculatePercentile(List<long> sortedValues, double percentile)
{
    var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
    index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
    return sortedValues[index];
}
```

## Manejo de Errores

### Estrategia por Capa

**ViewModel Layer**:
- Try-catch alrededor de operaciones async
- Muestra MessageBox al usuario
- Actualiza StatusMessage
- No propaga excepciones a UI

**Service Layer**:
- Retorna tuplas `(bool success, string? error)`
- No lanza excepciones para errores de negocio
- Solo lanza para errores críticos/inesperados

**Stress Test Runner**:
- Errores individuales NO detienen el test
- Marca iteración como fallida
- Continúa con siguientes iteraciones
- `OperationCanceledException` manejada gracefully

## Persistencia

### Configuración Local
```json
{
  "ConnectionSettings": { ... },
  "TestSettings": { ... },
  "ParameterMappings": [ ... ]
}
```
Ubicación: `%LocalAppData%\SqlStressRunner\config.json`

### Logs en Base de Datos
Tablas:
- `StressTestLog`: Detalles por iteración
- `StressTestLogSummary`: Resumen por run
- `vw_StressTestSummary`: Vista consolidada

## Extensibilidad

### Agregar Nueva Métrica
1. Agregar propiedad a `MetricsSummary`
2. Calcular en `MetricsService.CalculateMetrics()`
3. Agregar propiedad formateada en `ResultsViewModel`
4. Binding en `ResultsView.xaml`

### Agregar Nuevo Tipo de Test
1. Crear nuevo Model para settings
2. Crear Service específico
3. Extender `TestConfigurationViewModel`
4. Agregar UI en `TestConfigurationView`

### Agregar Tercera Stored Procedure
1. Agregar `StoredProcedure3` a `StressTestSettings`
2. Agregar `Sp3ParameterName` a `ParameterMapping`
3. Agregar `Sp3DurationMs` a `IterationResult`
4. Modificar `StressTestRunnerService.ExecuteIterationAsync()`
5. Actualizar cálculo de TPS en `MetricsService`

## Performance Considerations

### Connection Pooling
- Enabled por defecto en SqlConnection
- Pool Size: Default (100 min, unlimited max)
- Connection Lifetime: Default (0 - no expira)

### Memory Management
- DataTable puede ser grande: considerar streaming para millones de rows
- ConcurrentBag: eficiente para escrituras concurrentes
- Progress updates: throttling podría mejorar con muchas iteraciones

### UI Responsiveness
- Todas las operaciones I/O son async
- Progress reportado mediante IProgress<T>
- CancellationToken permite cancelación responsive

## Testing Recommendations

### Unit Tests
- Services: mockeables fácilmente
- ViewModels: testear lógica sin UI
- Metrics: verificar cálculos

### Integration Tests
- Conexión real a SQL Server de test
- Stored procedures de prueba
- Validar flow completo

### Performance Tests
- Probar con diferentes grados de paralelismo
- Medir overhead de logging
- Validar thread-safety bajo carga

## Decisiones de Diseño

### ¿Por qué ADO.NET y no EF?
- Control fino sobre conexiones
- Performance en escenarios de alta concurrencia
- No necesitamos mapeo objeto-relacional complejo

### ¿Por qué no Dapper?
- Requisito explícito del proyecto
- ADO.NET es suficiente para este caso

### ¿Por qué ConcurrentBag?
- Excelente performance para escrituras concurrentes
- No necesitamos orden específico
- Thread-safe sin locks explícitos

### ¿Por qué Parallel.ForEachAsync?
- Built-in scheduling
- CancellationToken support
- MaxDegreeOfParallelism control
- Exception handling integrado

## Patrones Utilizados

- **MVVM**: Separación UI/Logic
- **Factory**: SqlConnectionFactory
- **Service Layer**: Encapsulación de lógica
- **Command Pattern**: RelayCommand, AsyncRelayCommand
- **Observer Pattern**: INotifyPropertyChanged, Events
- **Repository Pattern**: (implícito en Services)

## Conclusión

La arquitectura de SQL Stress Runner está diseñada para ser:
- **Mantenible**: Separación clara de responsabilidades
- **Testeable**: Servicios independientes
- **Escalable**: Preparada para extensiones
- **Performante**: Async/await, connection pooling, paralelismo
- **Robusta**: Manejo de errores en todas las capas
