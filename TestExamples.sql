-- Ejemplos de Stored Procedures para pruebas
-- Estos son ejemplos que puedes usar para probar la aplicación

-- ============================================
-- EJEMPLO 1: Sistema de Órdenes Simple
-- ============================================

-- Tabla de prueba
CREATE TABLE TestOrders
(
    OrderId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWGUID(),
    CustomerId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    TotalAmount DECIMAL(18,2),
    OrderStatus VARCHAR(50) DEFAULT 'Pending',
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- SP1: Crear orden
CREATE OR ALTER PROCEDURE CreateOrder
    @CustomerId INT,
    @ProductId INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalAmount DECIMAL(18,2) = @Quantity * 99.99;
    
    INSERT INTO TestOrders (CustomerId, ProductId, Quantity, TotalAmount)
    VALUES (@CustomerId, @ProductId, @Quantity, @TotalAmount);
    
    -- Simular un poco de trabajo
    WAITFOR DELAY '00:00:00.010';
END;
GO

-- SP2: Procesar orden
CREATE OR ALTER PROCEDURE ProcessOrder
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE TestOrders
    SET OrderStatus = 'Processed'
    WHERE CustomerId = @CustomerId
    AND OrderStatus = 'Pending';
    
    -- Simular procesamiento
    WAITFOR DELAY '00:00:00.015';
END;
GO

-- Query inicial para este ejemplo
-- SELECT TOP 1000 
--     ABS(CHECKSUM(NEWID())) % 10000 AS CustomerId,
--     ABS(CHECKSUM(NEWID())) % 100 AS ProductId,
--     ABS(CHECKSUM(NEWID())) % 10 + 1 AS Quantity

-- ============================================
-- EJEMPLO 2: Sistema de Inventario
-- ============================================

-- Tablas de prueba
CREATE TABLE Products
(
    ProductId INT PRIMARY KEY,
    ProductName VARCHAR(100),
    Stock INT,
    Price DECIMAL(18,2)
);

CREATE TABLE InventoryLog
(
    LogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT,
    Operation VARCHAR(50),
    Quantity INT,
    LogDate DATETIME2 DEFAULT GETDATE()
);

-- Insertar datos de prueba
INSERT INTO Products (ProductId, ProductName, Stock, Price)
SELECT TOP 100
    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)),
    'Product ' + CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS VARCHAR),
    1000000,
    RAND() * 1000
FROM sys.objects;

-- SP1: Reservar inventario
CREATE OR ALTER PROCEDURE ReserveInventory
    @ProductId INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE Products
        SET Stock = Stock - @Quantity
        WHERE ProductId = @ProductId
        AND Stock >= @Quantity;
        
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Insufficient stock', 16, 1);
            ROLLBACK;
            RETURN;
        END;
        
        INSERT INTO InventoryLog (ProductId, Operation, Quantity)
        VALUES (@ProductId, 'Reserved', @Quantity);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- SP2: Confirmar reserva
CREATE OR ALTER PROCEDURE ConfirmReservation
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO InventoryLog (ProductId, Operation, Quantity)
    VALUES (@ProductId, 'Confirmed', 0);
    
    WAITFOR DELAY '00:00:00.005';
END;
GO

-- Query inicial para este ejemplo
-- SELECT 
--     ABS(CHECKSUM(NEWID())) % 100 + 1 AS ProductId,
--     ABS(CHECKSUM(NEWID())) % 5 + 1 AS Quantity
-- FROM sys.objects
-- WHERE type = 'U'

-- ============================================
-- EJEMPLO 3: Sistema de Usuarios (Sin tablas reales)
-- ============================================

-- SP1: Validar usuario (solo WAITFOR para simular)
CREATE OR ALTER PROCEDURE ValidateUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Simular validación compleja
    WAITFOR DELAY '00:00:00.020';
    
    -- Simular ocasionalmente un error (5% del tiempo)
    IF @UserId % 20 = 0
    BEGIN
        RAISERROR('User validation failed', 16, 1);
        RETURN;
    END;
END;
GO

-- SP2: Registrar acceso
CREATE OR ALTER PROCEDURE LogUserAccess
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Simular logging
    WAITFOR DELAY '00:00:00.010';
END;
GO

-- Query inicial para este ejemplo
-- SELECT TOP 10000
--     ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS UserId

-- ============================================
-- EJEMPLO 4: Prueba de Performance Pura
-- ============================================

-- SP1: Solo cálculo
CREATE OR ALTER PROCEDURE CalculateChecksum
    @InputValue INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Result INT;
    SET @Result = CHECKSUM(@InputValue);
    
    -- No WAITFOR, mide performance real
END;
GO

-- SP2: Verificar hash
CREATE OR ALTER PROCEDURE VerifyHash
    @InputValue INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Hash VARCHAR(64);
    SET @Hash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(@InputValue AS VARCHAR)), 2);
END;
GO

-- Query inicial para este ejemplo
-- SELECT TOP 50000
--     ABS(CHECKSUM(NEWID())) AS InputValue

-- ============================================
-- LIMPIEZA
-- ============================================

-- Para limpiar después de las pruebas:
-- DROP TABLE IF EXISTS TestOrders;
-- DROP TABLE IF EXISTS InventoryLog;
-- DROP TABLE IF EXISTS Products;
-- DROP PROCEDURE IF EXISTS CreateOrder;
-- DROP PROCEDURE IF EXISTS ProcessOrder;
-- DROP PROCEDURE IF EXISTS ReserveInventory;
-- DROP PROCEDURE IF EXISTS ConfirmReservation;
-- DROP PROCEDURE IF EXISTS ValidateUser;
-- DROP PROCEDURE IF EXISTS LogUserAccess;
-- DROP PROCEDURE IF EXISTS CalculateChecksum;
-- DROP PROCEDURE IF EXISTS VerifyHash;
