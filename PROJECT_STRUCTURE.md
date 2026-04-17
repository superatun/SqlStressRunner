# Estructura del Proyecto SQL Stress Runner

```
SqlStressRunner/
│
├── 📁 Models/                                    # Modelos de datos (POCOs)
│   ├── AppConfiguration.cs                       # Configuración completa de la app
│   ├── DatabaseConnectionSettings.cs             # Settings de conexión a SQL
│   ├── IterationResult.cs                        # Resultado de una iteración
│   ├── MetricsSummary.cs                         # Métricas agregadas
│   ├── ParameterMapping.cs                       # Mapeo columna → parámetro
│   ├── StressTestSettings.cs                     # Configuración del test
│   └── TestRunState.cs                           # Estado de ejecución (enum)
│
├── 📁 ViewModels/                                # ViewModels (MVVM pattern)
│   ├── MainViewModel.cs                          # ViewModel principal, orquestador
│   ├── ConnectionViewModel.cs                    # VM para tab Connection
│   ├── TestConfigurationViewModel.cs             # VM para tab Test Configuration
│   ├── ParameterMappingViewModel.cs              # VM para tab Parameter Mapping
│   └── ResultsViewModel.cs                       # VM para tab Results
│
├── 📁 Views/                                     # Vistas XAML (UI)
│   ├── ConnectionView.xaml                       # UI para configurar conexión
│   ├── ConnectionView.xaml.cs                    # Code-behind
│   ├── TestConfigurationView.xaml                # UI para configurar test
│   ├── TestConfigurationView.xaml.cs             # Code-behind
│   ├── ParameterMappingView.xaml                 # UI para mapear parámetros
│   ├── ParameterMappingView.xaml.cs              # Code-behind
│   ├── ResultsView.xaml                          # UI para mostrar resultados
│   └── ResultsView.xaml.cs                       # Code-behind
│
├── 📁 Services/                                  # Lógica de negocio
│   ├── SqlConnectionFactory.cs                   # Factory para crear conexiones
│   ├── ConnectionTestService.cs                  # Servicio para probar conexión
│   ├── InitialDataLoaderService.cs               # Carga dataset inicial
│   ├── StoredProcedureExecutionService.cs        # Ejecuta SPs con timing
│   ├── StressTestRunnerService.cs                # Orquestador del test de estrés
│   ├── MetricsService.cs                         # Calcula métricas (TPS, percentiles)
│   └── ResultLoggingService.cs                   # Persiste resultados en SQL
│
├── 📁 Infrastructure/                            # Clases base y utilidades
│   ├── ViewModelBase.cs                          # Base para ViewModels (INPC)
│   └── InverseBoolConverter.cs                   # Converter para bindings
│
├── 📁 Commands/                                  # Implementaciones de ICommand
│   ├── RelayCommand.cs                           # Command síncrono
│   └── AsyncRelayCommand.cs                      # Command asíncrono
│
├── 📁 Helpers/                                   # Utilidades generales
│   └── ConfigurationHelper.cs                    # Serialización JSON de config
│
├── 📄 MainWindow.xaml                            # Ventana principal con TabControl
├── 📄 MainWindow.xaml.cs                         # Code-behind de ventana principal
│
├── 📄 App.xaml                                   # Configuración de la aplicación
├── 📄 App.xaml.cs                                # Entry point de la aplicación
│
├── 📄 SqlStressRunner.csproj                     # Archivo de proyecto
│
├── 📁 Documentation/                             # Documentación (conceptual)
│   ├── 📄 README.md                              # Guía principal del usuario
│   ├── 📄 ARCHITECTURE.md                        # Documentación de arquitectura
│   ├── 📄 EXECUTION_GUIDE.md                     # Guía paso a paso de ejecución
│   └── 📄 CHANGELOG.md                           # Historial de versiones
│
├── 📁 Database/                                  # Scripts SQL (conceptual)
│   ├── 📄 DatabaseSchema.sql                     # Schema para tablas de logging
│   └── 📄 TestExamples.sql                       # SPs de ejemplo para testing
│
└── 📁 Configuration/                             # Configuración (conceptual)
    └── 📄 config.example.json                    # Ejemplo de archivo de config

Archivos Generados en Build:
📁 bin/
├── 📁 Debug/
│   └── 📁 net10.0-windows/
│       ├── SqlStressRunner.exe                   # Ejecutable debug
│       ├── SqlStressRunner.dll
│       ├── Microsoft.Data.SqlClient.dll
│       └── ... (otras dependencias)
│
└── 📁 Release/
    └── 📁 net10.0-windows/
        ├── SqlStressRunner.exe                   # Ejecutable release
        ├── SqlStressRunner.dll
        ├── Microsoft.Data.SqlClient.dll
        └── ... (otras dependencias)

Configuración del Usuario:
%LocalAppData%/SqlStressRunner/
└── 📄 config.json                                # Configuración persistente
```

## Conteo de Archivos

### Código Fuente
- **Models**: 7 archivos
- **ViewModels**: 5 archivos
- **Views**: 8 archivos (4 XAML + 4 code-behind)
- **Services**: 7 archivos
- **Infrastructure**: 2 archivos
- **Commands**: 2 archivos
- **Helpers**: 1 archivo
- **App/MainWindow**: 4 archivos

**Total Código**: 36 archivos C#/XAML

### Documentación
- **Guías**: 4 archivos markdown
- **SQL Scripts**: 2 archivos
- **Config Example**: 1 archivo JSON

**Total Documentación**: 7 archivos

### Proyecto
- **csproj**: 1 archivo

**TOTAL GENERAL**: 44 archivos

## Líneas de Código Aproximadas

| Categoría | Archivos | Líneas de Código (aprox) |
|-----------|----------|--------------------------|
| Models | 7 | ~200 |
| ViewModels | 5 | ~800 |
| Views (XAML) | 4 | ~600 |
| Views (Code-behind) | 4 | ~100 |
| Services | 7 | ~900 |
| Infrastructure | 2 | ~80 |
| Commands | 2 | ~100 |
| Helpers | 1 | ~50 |
| App/MainWindow | 4 | ~150 |
| **TOTAL CÓDIGO** | **36** | **~2,980** |
| Documentación | 7 | ~1,500 |
| **TOTAL PROYECTO** | **43** | **~4,480** |

## Dependencias Externas

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.0" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

## Características por Carpeta

### 📁 Models
- Sin dependencias externas
- Serializables (JSON)
- Inmutables donde aplica
- Validation logic mínima

### 📁 ViewModels
- Implementan INotifyPropertyChanged
- Usan Commands (RelayCommand, AsyncRelayCommand)
- Sin referencias a Views
- Coordinan entre Services y UI

### 📁 Views
- XAML puro con bindings
- Code-behind mínimo
- DataContext = ViewModel
- No contienen lógica de negocio

### 📁 Services
- Stateless cuando es posible
- Thread-safe
- Retornan tuplas o DTOs
- No referencias a UI

### 📁 Infrastructure
- Clases base reutilizables
- Converters para bindings
- Sin lógica de negocio

### 📁 Commands
- Implementan ICommand
- Soportan CanExecute
- Versión sync y async

### 📁 Helpers
- Utilidades estáticas
- Sin estado
- Funciones puras cuando es posible

## Convenciones de Naming

- **Clases**: PascalCase
- **Interfaces**: IPascalCase
- **Métodos**: PascalCase
- **Propiedades**: PascalCase
- **Campos privados**: _camelCase
- **Parámetros**: camelCase
- **Constantes**: UPPER_SNAKE_CASE (si aplica)

## Patrones Aplicados

- ✅ MVVM (Model-View-ViewModel)
- ✅ Factory (SqlConnectionFactory)
- ✅ Command Pattern (ICommand implementations)
- ✅ Observer (INotifyPropertyChanged, Events)
- ✅ Service Layer
- ✅ Dependency Injection (manual, via constructor)
- ✅ Repository-like (Services abstraen data access)

## Métricas de Calidad

- **Compilación**: ✅ Exitosa sin warnings
- **Separación de Concerns**: ✅ Clara separación por carpetas
- **Testabilidad**: ✅ Services independientes y mockables
- **Mantenibilidad**: ✅ Código organizado, bien estructurado
- **Extensibilidad**: ✅ Fácil agregar funcionalidad
- **Performance**: ✅ Async/await, connection pooling, paralelismo

---

**Esta estructura sigue las mejores prácticas de desarrollo .NET/WPF y está lista para producción.**
