# SQL Stress Runner

Una aplicación de escritorio para pruebas de carga y estrés en stored procedures de SQL Server.

## Características

- ✅ Interfaz gráfica WPF con patrón MVVM
- ✅ Conexión flexible a SQL Server (Windows Auth o SQL Auth)
- ✅ Ejecución concurrente configurable
- ✅ Mapeo flexible de parámetros
- ✅ Métricas detalladas (TPS, latencias, percentiles)
- ✅ Logging opcional a base de datos
- ✅ Persistencia de configuración

## Requisitos

- .NET 10 SDK
- SQL Server (cualquier versión compatible con Microsoft.Data.SqlClient)
- Windows 10/11

## Instalación

1. Clona el repositorio o extrae el código
2. Abre el proyecto en Visual Studio 2022 o superior
3. Restaura los paquetes NuGet
4. Compila el proyecto
5. Ejecuta la aplicación

## Uso

### 1. Configurar Conexión

En la pestaña **Connection**:
- Ingresa los datos del servidor SQL
- Selecciona el tipo de autenticación
- Prueba la conexión con el botón "Test Connection"
- Guarda la configuración

### 2. Configurar Prueba

En la pestaña **Test Configuration**:
- **Initial Query**: Escribe una query que devuelva datos para alimentar tus stored procedures
  ```sql
  SELECT UserId, ProductId, Quantity FROM TestData
  ```
- **SP1**: Primera stored procedure o SQL a ejecutar
  ```sql
  EXEC CreateOrder @UserId, @ProductId, @Quantity
  ```
- **SP2**: Segunda stored procedure o SQL a ejecutar
  ```sql
  EXEC ProcessOrder @OrderId
  ```
- Configura:
  - Command Timeout (segundos)
  - Max Degree of Parallelism (workers concurrentes)
  - Total Iterations (cuántas veces ejecutar)
  - Recycle Dataset (reusar los datos cíclicamente)
- Opcionalmente activa "Log to Database" para guardar resultados

Carga los datos con "Load Initial Data"

### 3. Mapear Parámetros

En la pestaña **Parameter Mapping**:
- Verás las columnas devueltas por tu Initial Query
- Mapea cada columna a los parámetros de SP1 y SP2
- Ejemplo:
  - Column: `UserId` → SP1 Param: `@UserId` → SP2 Param: ``
  - Column: `ProductId` → SP1 Param: `@ProductId` → SP2 Param: ``
  - Column: `OrderId` → SP1 Param: `` → SP2 Param: `@OrderId`

### 4. Ejecutar Prueba

- Regresa a **Test Configuration**
- Haz clic en "Start Test"
- Observa el progreso en tiempo real
- Puedes cancelar con "Cancel Test"

### 5. Ver Resultados

En la pestaña **Results**:
- Run ID único
- Duración total
- Iteraciones totales/exitosas/fallidas
- **TPS (Transactions Per Second)**:
  - Por iteración
  - Por stored procedure
- Latencias (promedio, min, max, P95, P99)
- Duración promedio de SP1 y SP2

## Flujo de Ejecución

Para cada iteración:
1. Se toma una fila del dataset inicial
2. Se ejecuta SP1 con parámetros mapeados
3. Si SP1 falla → iteración marcada como fallida, SP2 no se ejecuta
4. Si SP1 funciona → se ejecuta SP2
5. Si SP2 falla → iteración marcada como fallida
6. Se registran métricas individuales

## Logging a Base de Datos

Si activas "Log to Database":
1. Ejecuta el script `DatabaseSchema.sql` en tu base de datos
2. Esto crea las tablas:
   - `StressTestLog`: detalles de cada iteración
   - `StressTestLogSummary`: resumen de cada run
   - `vw_StressTestSummary`: vista para consultas

## Persistencia de Configuración

La aplicación guarda automáticamente tu última configuración en:
```
%LocalAppData%\SqlStressRunner\config.json
```

Puedes cargarla desde el menú: File → Load Configuration

## Estructura del Proyecto

```
SqlStressRunner/
├── Models/                          # Modelos de datos
│   ├── DatabaseConnectionSettings.cs
│   ├── StressTestSettings.cs
│   ├── ParameterMapping.cs
│   ├── IterationResult.cs
│   ├── MetricsSummary.cs
│   ├── TestRunState.cs
│   └── AppConfiguration.cs
├── ViewModels/                      # ViewModels MVVM
│   ├── MainViewModel.cs
│   ├── ConnectionViewModel.cs
│   ├── TestConfigurationViewModel.cs
│   ├── ParameterMappingViewModel.cs
│   └── ResultsViewModel.cs
├── Views/                           # Vistas XAML
│   ├── ConnectionView.xaml
│   ├── TestConfigurationView.xaml
│   ├── ParameterMappingView.xaml
│   └── ResultsView.xaml
├── Services/                        # Lógica de negocio
│   ├── SqlConnectionFactory.cs
│   ├── ConnectionTestService.cs
│   ├── InitialDataLoaderService.cs
│   ├── StoredProcedureExecutionService.cs
│   ├── StressTestRunnerService.cs
│   ├── MetricsService.cs
│   └── ResultLoggingService.cs
├── Infrastructure/                  # Base classes y converters
│   ├── ViewModelBase.cs
│   └── InverseBoolConverter.cs
├── Commands/                        # ICommand implementations
│   ├── RelayCommand.cs
│   └── AsyncRelayCommand.cs
└── Helpers/                         # Utilidades
    └── ConfigurationHelper.cs
```

## Definiciones de Métricas

- **TPS por Iteración**: Iteraciones exitosas / tiempo total en segundos
- **TPS por Stored Procedure**: (Iteraciones exitosas × 2) / tiempo total en segundos
- **Latencia**: Tiempo total de ejecución de SP1 + SP2
- **P95**: El 95% de las iteraciones completaron en este tiempo o menos
- **P99**: El 99% de las iteraciones completaron en este tiempo o menos

## Ejemplos de Uso

### Ejemplo 1: Prueba simple
```sql
-- Initial Query
SELECT TOP 1000 CustomerId, ProductId FROM Orders

-- SP1
EXEC ValidateCustomer @CustomerId

-- SP2
EXEC CheckProductStock @ProductId
```

### Ejemplo 2: Con creación de datos
```sql
-- Initial Query
SELECT NEWID() AS OrderId, CustomerId, ProductId, RAND() * 100 AS Amount
FROM Customers CROSS JOIN Products

-- SP1
EXEC CreateOrder @OrderId, @CustomerId, @ProductId, @Amount

-- SP2
EXEC ProcessPayment @OrderId, @Amount
```

## Troubleshooting

### Error de conexión
- Verifica que SQL Server esté corriendo
- Revisa el firewall
- Confirma las credenciales
- Asegúrate que "Trust Server Certificate" esté activado si usas certificados auto-firmados

### Errores durante la prueba
- Los errores individuales no detienen la prueba completa
- Revisa los mensajes de error en el log
- Verifica que tus stored procedures acepten los parámetros correctos
- Asegúrate que el mapeo de parámetros sea correcto

### Performance lenta
- Reduce Max Degree of Parallelism si tienes muchos errores de timeout
- Aumenta Command Timeout
- Verifica que tu base de datos pueda manejar la carga

## Tecnologías Utilizadas

- **.NET 10**
- **WPF** (Windows Presentation Foundation)
- **MVVM** (Model-View-ViewModel pattern)
- **ADO.NET** con Microsoft.Data.SqlClient
- **System.Text.Json** para serialización

## Arquitectura

- **Patrón MVVM**: Separación completa entre UI y lógica
- **Async/Await**: Operaciones asíncronas para no bloquear UI
- **Connection Pooling**: Cada worker abre su propia conexión
- **Thread-Safe**: Uso de ConcurrentBag y Parallel.ForEachAsync
- **Progress Reporting**: Actualización de UI sin romper MVVM

## Licencia

Código libre para uso personal y comercial.

## Autor

Desarrollado como herramienta de pruebas de carga para SQL Server.

## Contribuciones

Pull requests son bienvenidos. Para cambios mayores, abre un issue primero.
