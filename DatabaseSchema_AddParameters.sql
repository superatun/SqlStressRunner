-- Script para agregar la columna Parameters a una tabla StressTestLog existente
-- Ejecuta este script si ya creaste las tablas anteriormente y solo necesitas agregar la columna

USE [SI_BDDev_CorApp1_04]; -- Cambia esto por el nombre de tu base de datos
GO

-- Verificar si la columna ya existe
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'StressTestLog' 
    AND COLUMN_NAME = 'Parameters'
)
BEGIN
    PRINT 'Agregando columna Parameters a StressTestLog...';
    
    ALTER TABLE StressTestLog
    ADD Parameters NVARCHAR(MAX) NULL;
    
    PRINT 'Columna Parameters agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna Parameters ya existe en StressTestLog.';
END
GO

-- Verificar que la columna fue agregada
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'StressTestLog'
AND COLUMN_NAME = 'Parameters';
GO
