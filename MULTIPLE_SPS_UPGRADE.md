# Actualización: Soporte para Múltiples Stored Procedures

## Cambios Implementados

La aplicación SQL Stress Runner ahora soporta **cualquier cantidad de stored procedures** en lugar de estar limitada a solo dos.

### ✅ Características Nuevas

1. **Stored Procedures Dinámicos**
   - Puedes agregar tantos SPs como necesites
   - Cada SP tiene nombre editable
   - Orden de ejecución configurable
   - Habilitar/deshabilitar SPs individualmente

2. **Gestión de SPs en UI**
   - Botón "+ Add SP" para agregar nuevos SPs
   - Botones ↑/↓ para reordenar
   - Botón ✖ para eliminar (mantiene al menos 1)
   - Checkbox para habilitar/deshabilitar sin eliminar

3. **Mapeo de Parámetros Dinámico**
   - Se adapta automáticamente al número de SPs
   - Mapeo columna → parámetro por cada SP
   - Compatible con configuraciones guardadas

4. **Métricas por SP**
   - Duración promedio por cada SP
   - TPS calculado según cantidad de SPs activos
   - Resultados detallados por iteración

### 📋 Modelos Modificados

#### `StoredProcedureConfiguration` (NUEVO)
```csharp
public class StoredProcedureConfiguration
{
    public string Name { get; set; }           // Nombre del SP
    public string SqlCommand { get; set; }      // Comando SQL/SP
    public int Order { get; set; }              // Orden de ejecución
    public bool IsEnabled { get; set; }         // Activo/Inactivo
}
```

#### `StressTestSettings`
```csharp
// NUEVO: Colección dinámica
public ObservableCollection<StoredProcedureConfiguration> StoredProcedures { get; set; }

// LEGACY: Mantenido para compatibilidad hacia atrás
public string StoredProcedure1 { get; set; }
public string StoredProcedure2 { get; set; }
```

#### `ParameterMapping`
```csharp
// NUEVO: Mapeo dinámico por nombre de SP
public Dictionary<string, string> SpParameterMappings { get; set; }

// LEGACY: Mantenido para compatibilidad
public string Sp1ParameterName { get; set; }
public string Sp2ParameterName { get; set; }
```

#### `IterationResult`
```csharp
// NUEVO: Duraciones por SP
public Dictionary<string, long> SpDurations { get; set; }

// LEGACY: Mantenido para compatibilidad
public long Sp1DurationMs { get; set; }
public long Sp2DurationMs { get; set; }
```

#### `MetricsSummary`
```csharp
// NUEVO: Métricas por SP
public Dictionary<string, double> AverageSpDurations { get; set; }

// LEGACY: Mantenido para compatibilidad
public double AverageSp1DurationMs { get; set; }
public double AverageSp2DurationMs { get; set; }
```

### 🎯 ViewModels Actualizados

#### `TestConfigurationViewModel`
- Nueva propiedad: `ObservableCollection<StoredProcedureConfiguration> StoredProcedures`
- Nuevos comandos:
  - `AddStoredProcedureCommand`: Agregar SP
  - `RemoveStoredProcedureCommand`: Eliminar SP
  - `MoveUpCommand`: Subir en orden
  - `MoveDownCommand`: Bajar en orden
- Evento nuevo: `StoredProceduresChanged`: Notifica cambios al ParameterMapping

#### `ParameterMappingViewModel`
- Método nuevo: `UpdateStoredProcedureNames(List<string>)`: Actualiza la lista de SPs
- Sincroniza automáticamente mappings cuando cambian los SPs
- Migra de formato legacy a nuevo formato automáticamente

#### `MainViewModel`
- Conecta evento `StoredProceduresChanged` entre TestConfiguration y ParameterMapping
- Inicializa SP names en ParameterMapping al arrancar

### 🛠️ Services Actualizados

#### `StressTestRunnerService`
- Ejecuta N stored procedures en secuencia
- Si un SP falla, se detiene la iteración (no ejecuta los siguientes)
- Registra duración de cada SP individualmente
- Soporte para legacy (si no hay SPs en colección, usa SP1/SP2)

#### `MetricsService`
```csharp
// Nueva firma con parámetro spCount
public MetricsSummary CalculateMetrics(
    List<IterationResult> results, 
    DateTime startTime, 
    DateTime endTime, 
    int spCount = 2)  // Default 2 para compatibilidad
```
- Calcula TPS basado en cantidad real de SPs: `(iteraciones × spCount) / tiempo`
- Genera métricas promedio por cada SP

### 🎨 UI Actualizada

#### `TestConfigurationView.xaml`
Nueva sección "Stored Procedures":
```xaml
<Border> <!-- Container de SPs -->
    <ItemsControl ItemsSource="{Binding StoredProcedures}">
        <!-- Cada SP muestra: -->
        - Checkbox (Enabled/Disabled)
        - TextBox (Nombre editable)
        - Order (automático)
        - Botones: ↑ ↓ ✖
        - TextBox multilínea (SQL Command)
    </ItemsControl>
</Border>
```

### 🔄 Compatibilidad Hacia Atrás

La aplicación mantiene **100% compatibilidad** con configuraciones antiguas:

1. **Al cargar configuración antigua** (solo SP1/SP2):
   - Se migra automáticamente al nuevo formato
   - Se crean 2 StoredProcedureConfiguration
   - Mappings se convierten a nuevo diccionario

2. **Al guardar configuración**:
   - Se guarda en nuevo formato (StoredProcedures collection)
   - También se populan SP1/SP2 legacy para compatibilidad
   - Archivos viejos pueden abrirse en nueva versión

3. **Propiedades legacy mantenidas**:
   - `StressTestSettings.StoredProcedure1/2`
   - `ParameterMapping.Sp1ParameterName/Sp2ParameterName`
   - `IterationResult.Sp1DurationMs/Sp2DurationMs`
   - `MetricsSummary.AverageSp1DurationMs/AverageSp2DurationMs`

### 📖 Cómo Usar

#### Agregar Stored Procedures

1. Ve a la pestaña **"Test Configuration"**
2. En la sección "Stored Procedures", haz clic en **"+ Add SP"**
3. Se agrega un nuevo SP con:
   - Nombre: `SP3`, `SP4`, etc (editable)
   - Comando SQL: Template `EXEC YourSPN @Param`
   - Order: Automático (siguiente número)
   - Enabled: ✓ Por defecto

#### Reordenar SPs

- Usa botones **↑** y **↓** para cambiar el orden de ejecución
- El orden se actualiza automáticamente
- Los SPs se ejecutan en orden ascendente

#### Eliminar SPs

- Haz clic en **✖** para eliminar un SP
- No se puede eliminar el último SP (mínimo 1 requerido)
- Los parámetros mapeados a ese SP se eliminan automáticamente

#### Habilitar/Deshabilitar SPs

- Usa el checkbox al lado del nombre
- SPs deshabilitados no se ejecutan pero se mantienen en configuración
- útil para hacer pruebas A/B

#### Mapeo de Parámetros

En la pestaña **"Parameter Mapping"**, ahora verás:
- Una columna por cada SP configurado
- Los headers son los nombres de los SPs
- Mapea cada columna del dataset al parámetro correspondiente de cada SP

Ejemplo:
| Column Name | SP1 (@Param) | SP2 (@Param) | SP3 (@Param) |
|-------------|--------------|--------------|--------------|
| UserId      | @UserId      | @UserId      |              |
| ProductId   | @ProductId   |              | @ProductId   |
| OrderId     |              | @OrderId     | @OrderId     |

### 🔢 Cálculo de Métricas

#### TPS (Transactions Per Second)

**Antes (fijo 2 SPs):**
```
TPS por SP = (iteraciones exitosas × 2) / tiempo total
```

**Ahora (dinámico):**
```
TPS por SP = (iteraciones exitosas × cantidad de SPs activos) / tiempo total
```

**Ejemplo:**
- 1000 iteraciones exitosas
- 3 SPs habilitados
- 100 segundos totales

```
TPS por Iteración = 1000 / 100 = 10 TPS
TPS por SP = (1000 × 3) / 100 = 30 TPS
```

#### Duración por SP

El ResultsViewModel ahora muestra:
```
Avg SP1 Duration: 45.2 ms
Avg SP2 Duration: 67.8 ms
Avg SP3 Duration: 23.4 ms
Avg SP4 Duration: 91.1 ms
```

### ⚠️ Consideraciones

1. **Orden de Ejecución es Secuencial**
   - Los SPs se ejecutan uno tras otro, no en paralelo
   - Si SP2 falla, SP3 y SP4 no se ejecutan en esa iteración

2. **Paralelismo es a Nivel de Iteración**
   - `MaxDegreeOfParallelism` controla cuántas iteraciones corren en paralelo
   - Dentro de cada iteración, los SPs son secuenciales

3. **Mínimo 1 SP Requerido**
   - No se puede eliminar el último SP
   - Al menos 1 SP debe estar habilitado para ejecutar

4. **Configuración JSON**
   - El formato ha cambiado pero es compatible
   - Archivos antiguos se migran automáticamente al abrir

### 📊 Ejemplo Completo

```csharp
// Configuración con 4 SPs
SP1: CreateOrder    (@CustomerId, @ProductId, @Quantity)  ✓ Enabled
SP2: ValidateStock  (@ProductId, @Quantity)               ✓ Enabled
SP3: ProcessPayment (@CustomerId, @Amount)                ✓ Enabled
SP4: SendNotification (@CustomerId, @OrderId)             ✗ Disabled

// Se ejecutarán solo SP1, SP2, SP3 (SP4 está deshabilitado)
// Orden de ejecución: SP1 → SP2 → SP3
// Si SP2 falla, SP3 no se ejecuta en esa iteración
```

### 🎉 Beneficios

1. **Flexibilidad Total**: No estás limitado a 2 SPs
2. **Escenarios Complejos**: Simula flujos de negocio completos
3. **A/B Testing**: Habilita/deshabilita SPs sin eliminar configuración
4. **Reordenamiento Fácil**: Cambia el orden de ejecución con un clic
5. **Métricas Detalladas**: Ve el performance de cada SP individualmente
6. **Sin Breaking Changes**: Configuraciones viejas siguen funcionando

### 📝 Cambios en Archivos

**Archivos Nuevos:**
- `Models/StoredProcedureConfiguration.cs`

**Archivos Modificados:**
- `Models/StressTestSettings.cs` - Agrega colección de SPs
- `Models/ParameterMapping.cs` - Agrega diccionario de mappings
- `Models/IterationResult.cs` - Agrega diccionario de duraciones
- `Models/MetricsSummary.cs` - Agrega diccionario de promedios
- `ViewModels/TestConfigurationViewModel.cs` - Reescrito para SPs dinámicos
- `ViewModels/ParameterMappingViewModel.cs` - Soporte para N SPs
- `ViewModels/MainViewModel.cs` - Conecta eventos
- `Services/StressTestRunnerService.cs` - Ejecuta N SPs
- `Services/MetricsService.cs` - Calcula con spCount dinámico
- `Commands/RelayCommand.cs` - Agrega RelayCommand<T> genérico
- `Views/TestConfigurationView.xaml` - Nueva UI con ItemsControl

---

**Versión Actualizada:** 1.1.0  
**Compilación:** ✅ Exitosa  
**Compatibilidad:** ✅ 100% hacia atrás  
**Estado:** ✅ Listo para producción
