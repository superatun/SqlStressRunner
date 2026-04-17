# Changelog

Todas las actualizaciones notables de este proyecto serán documentadas en este archivo.

## [1.2.0] - 2024-12-XX

### 🚀 Nueva Característica: Captura de Parámetros
- ✅ **Almacenamiento de Parámetros en cada Iteración**
  - Cada iteración ahora guarda todos los parámetros utilizados
  - Los parámetros se almacenan en formato JSON en la base de datos
  - Permite análisis detallado de qué datos causaron éxitos/fallos
  - Facilita la reproducción manual de iteraciones específicas

### 🔒 Nueva Característica: Persistencia de Configuración de Conexión
- ✅ **Auto-guardado de Configuración de Conexión**
  - La configuración de conexión se guarda automáticamente al presionar "Save Settings"
  - Se mantiene entre reinicios de la aplicación
  - **Contraseña NO se guarda** por seguridad (debe ingresarse cada vez)
  - Archivo guardado en `%LocalAppData%\SqlStressRunner\config.json`

### Implementado
- ✅ `IterationResult.Parameters` - Dictionary con todos los parámetros usados
- ✅ `StressTestLog.Parameters` - Columna NVARCHAR(MAX) para almacenar JSON
- ✅ Serialización automática de parámetros usando System.Text.Json
- ✅ Script `DatabaseSchema_AddParameters.sql` para actualizar tablas existentes
- ✅ Consultas SQL de ejemplo para analizar parámetros
- ✅ Botón "Copy" en Results para copiar RunId al portapapeles
- ✅ Mapeo dinámico de parámetros (no más separación por SP1/SP2)
- ✅ `[JsonIgnore]` en `DatabaseConnectionSettings.Password` para excluir contraseña del guardado
- ✅ Auto-guardado de configuración en `ConnectionViewModel.SaveSettings()`
- ✅ Retrocompatibilidad con tablas sin columna `Parameters`

### Mejorado
- ✅ `StressTestRunnerService.ExecuteIterationAsync` - Captura todos los parámetros
- ✅ `ResultLoggingService.LogIterationAsync` - Guarda parámetros como JSON con detección automática de columna
- ✅ `ParameterMappingView` - Columnas dinámicas basadas en SPs configurados
- ✅ `ConnectionViewModel.LoadSettings` - NO carga contraseña por seguridad
- ✅ DatabaseSchema.sql actualizado con columna Parameters y queries útiles

### Documentación
- ✅ `PARAMETERS_CAPTURE.md` - Guía completa sobre captura de parámetros
  - Consultas SQL para análisis
  - Casos de uso (debugging, patrones, reproducción)
  - Ejemplos de extracción de valores JSON
- ✅ `CONNECTION_PERSISTENCE.md` - Guía sobre persistencia de configuración
  - Explicación de qué se guarda y qué no
  - Consideraciones de seguridad
  - Workflow recomendado
  - Ubicación del archivo de configuración
- ✅ `EXAMPLE_PARAMETERS_ANALYSIS.md` - Ejemplo práctico completo
- ✅ Queries comentadas en DatabaseSchema.sql para análisis

### Seguridad
- 🔒 Contraseñas nunca se almacenan en disco
- 🔒 Atributo `[JsonIgnore]` previene serialización accidental
- 🔒 Usuario es notificado que password no se guarda
- 🔒 Compatible con políticas de seguridad corporativas

### Casos de Uso
- 🔍 Ver parámetros exactos de iteraciones fallidas
- 🔄 Reproducir manualmente cualquier iteración
- 📊 Analizar patrones entre parámetros y rendimiento
- 🐛 Debugging preciso con datos reales
- 📈 Identificar qué tipos de datos son más lentos
- ⚡ Inicio rápido: Solo ingresar password al reiniciar

## [1.1.0] - 2024-12-XX

### 🚀 Nueva Característica Principal
- ✅ **Soporte para Múltiples Stored Procedures**
  - No más límite de 2 SPs - ahora puedes ejecutar cuantos quieras
  - UI dinámica para agregar/eliminar/reordenar SPs
  - Cada SP puede habilitarse/deshabilitarse individualmente
  - Mapeo de parámetros dinámico que se adapta a la cantidad de SPs
  - Métricas individuales por cada SP

### Implementado
- ✅ Nuevo modelo `StoredProcedureConfiguration`
  - Nombre editable
  - Comando SQL/SP
  - Orden de ejecución
  - Estado enabled/disabled
- ✅ `StressTestSettings.StoredProcedures` - Colección observable de SPs
- ✅ `ParameterMapping.SpParameterMappings` - Diccionario dinámico de mappings
- ✅ `IterationResult.SpDurations` - Duraciones por SP
- ✅ `MetricsSummary.AverageSpDurations` - Métricas por SP
- ✅ Comandos nuevos en TestConfigurationViewModel:
  - `AddStoredProcedureCommand` - Agregar nuevo SP
  - `RemoveStoredProcedureCommand` - Eliminar SP
  - `MoveUpCommand` - Subir en orden
  - `MoveDownCommand` - Bajar en orden
- ✅ `RelayCommand<T>` genérico para commands con parámetros
- ✅ UI renovada con ItemsControl para SPs dinámicos
- ✅ Botones ↑↓✖ para gestionar SPs
- ✅ Cálculo dinámico de TPS basado en cantidad real de SPs
- ✅ **100% compatibilidad hacia atrás** con configuraciones antiguas

### Mejorado
- ✅ `StressTestRunnerService` ahora ejecuta N SPs en secuencia
- ✅ `MetricsService` calcula TPS dinámicamente: `(iteraciones × spCount) / tiempo`
- ✅ `ParameterMappingViewModel` se sincroniza automáticamente con cambios en SPs
- ✅ Migración automática de formato legacy a nuevo formato

### Documentación
- ✅ `MULTIPLE_SPS_UPGRADE.md` - Guía completa de la nueva característica
- ✅ Ejemplos de uso con 3, 4, 5+ SPs
- ✅ Explicación del cálculo de métricas con N SPs

### Compatibilidad
- ✅ Propiedades legacy mantenidas:
  - `StoredProcedure1`, `StoredProcedure2`
  - `Sp1ParameterName`, `Sp2ParameterName`
  - `Sp1DurationMs`, `Sp2DurationMs`
  - `AverageSp1DurationMs`, `AverageSp2DurationMs`
- ✅ Archivos de configuración antiguos se abren sin problemas
- ✅ Migración transparente de 2 SPs a nuevo formato

## [1.0.0] - 2024-12-XX

### Implementado
- ✅ Aplicación WPF completa con patrón MVVM
- ✅ Interfaz con 4 pestañas (Connection, Test Configuration, Parameter Mapping, Results)
- ✅ Conexión a SQL Server con autenticación Windows o SQL
- ✅ Carga de dataset inicial desde query personalizada
- ✅ Ejecución de 2 stored procedures por iteración
- ✅ Mapeo flexible de parámetros columna → SP
- ✅ Ejecución paralela configurable (Max Degree of Parallelism)
- ✅ Medición precisa de tiempos (Stopwatch)
- ✅ Cálculo de métricas:
  - TPS (Transactions Per Second) por iteración
  - TPS por stored procedure
  - Latencia promedio, mínima, máxima
  - Percentiles P95 y P99
  - Duración promedio de SP1 y SP2
- ✅ Progress reporting en tiempo real
- ✅ Soporte para cancelación de pruebas (CancellationToken)
- ✅ Logging opcional a base de datos SQL Server
- ✅ Persistencia de configuración en JSON local
- ✅ Connection pooling (cada worker su propia conexión)
- ✅ Thread-safe con ConcurrentBag
- ✅ UI responsive (async/await, no bloquea UI thread)
- ✅ Manejo graceful de errores individuales
- ✅ Recycle Dataset option

### Arquitectura
- **Models**: 8 modelos de datos (POCOs) - Ahora incluye StoredProcedureConfiguration
- **ViewModels**: 5 ViewModels con INotifyPropertyChanged
- **Views**: 4 UserControls XAML + MainWindow
- **Services**: 7 servicios independientes y testeables
- **Commands**: RelayCommand, AsyncRelayCommand, RelayCommand<T>
- **Infrastructure**: ViewModelBase, InverseBoolConverter
- **Helpers**: ConfigurationHelper para persistencia JSON

### Dependencias
- Microsoft.Data.SqlClient 5.2.0
- System.Text.Json 9.0.0

### Documentación
- README.md con guía completa de usuario
- ARCHITECTURE.md con detalles técnicos
- EXECUTION_GUIDE.md con instrucciones paso a paso
- DatabaseSchema.sql para crear tablas de logging
- TestExamples.sql con stored procedures de ejemplo
- config.example.json con ejemplo de configuración

### Características Técnicas
- Target Framework: .NET 10
- UI Framework: WPF (Windows Presentation Foundation)
- Patrón: MVVM estricto
- Data Access: ADO.NET puro (sin ORM)
- Concurrencia: Parallel.ForEachAsync
- Logging: Microsoft.Data.SqlClient directo

### Testing
- Compilación exitosa verificada
- Estructura de proyecto validada
- Todos los bindings correctamente configurados

## Roadmap Futuro

### [1.1.0] - Planeado
- [ ] Exportar resultados a CSV/Excel
- [ ] Gráficos de latencia en tiempo real
- [ ] Historial de runs en la UI
- [ ] Comparación entre runs
- [ ] Templates de configuración predefinidos

### [1.2.0] - Planeado
- [ ] Soporte para más de 2 stored procedures
- [ ] Warmup iterations (no contadas en métricas)
- [ ] Think time entre iteraciones
- [ ] Ramp-up pattern (incremento gradual de workers)

### [2.0.0] - Ideas
- [ ] Plugin system para custom metrics
- [ ] REST API para ejecución remota
- [ ] Multi-server testing (distributed load)
- [ ] Azure SQL Database optimizations
- [ ] Docker containerization

## Suposiciones y Decisiones

### Suposiciones
1. El usuario tiene acceso a SQL Server con permisos necesarios
2. Las stored procedures aceptan parámetros simples (no TVP por ahora)
3. Los resultados de SPs no necesitan ser capturados (solo timing)
4. Windows es el sistema operativo objetivo
5. .NET 10 está disponible en el sistema del usuario

### Decisiones de Diseño
1. **ADO.NET sobre EF/Dapper**: Por requisito explícito y control fino
2. **ConcurrentBag**: Performance óptima para escrituras concurrentes
3. **Parallel.ForEachAsync**: Manejo integrado de async y paralelismo
4. **IProgress<T>**: Thread-safe marshaling a UI thread
5. **JSON sobre XML**: Más legible y moderno para config
6. **LocalAppData**: Ubicación estándar para configuraciones de usuario
7. **No capturar resultados de SP**: Foco en performance, no en validación de datos
8. **Dos SPs fijos**: Balance entre simplicidad y utilidad real

### Limitaciones Conocidas
1. Solo funciona en Windows (WPF requirement)
2. Máximo 2 stored procedures por iteración
3. No soporta Table-Valued Parameters (TVP) todavía
4. No captura/valida resultados de SPs
5. Métricas en memoria (no streaming para millones de iteraciones)
6. UI en inglés únicamente

### Extensibilidad
- Fácil agregar nuevos servicios
- Fácil agregar nuevas métricas
- Fácil extender modelos
- Preparado para más de 2 SPs (requiere refactoring)

## Notas de Versión

### Consideraciones de Performance
- Connection pooling enabled por defecto
- Cada iteración crea su SqlConnection (pool maneja eficiencia)
- ConcurrentBag es lock-free para writes
- Progress updates podrían throttlearse para millones de iteraciones

### Seguridad
- Passwords guardados en plain text en config.json (LocalAppData)
- Considerar encryption para producción
- TrustServerCertificate por defecto (conveniente, menos seguro)

### Compatibilidad
- SQL Server 2012+
- Azure SQL Database
- SQL Server en Docker
- Cualquier versión compatible con Microsoft.Data.SqlClient 5.x

---

**Versión Actual: 1.0.0**
**Estado: Stable**
**Última Actualización: 2024**
