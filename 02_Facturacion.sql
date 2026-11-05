/* =====================================================
   FACTURACION - ejecutar DESPUES de 01_Almacen.sql
   Seguro de repetir: no borra datos existentes.
   ===================================================== */
USE ALMACEN;
GO

/* ---------- TABLAS ---------- */
IF OBJECT_ID('dbo.Factura') IS NULL
CREATE TABLE dbo.Factura(
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Numero   AS ('FAC-' + RIGHT('000000' + CAST(Id AS VARCHAR(10)), 6)) PERSISTED,
    Fecha    DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    Cliente  NVARCHAR(100) NOT NULL DEFAULT N'Consumidor final',
    Usuario  NVARCHAR(50)  NOT NULL,
    Subtotal DECIMAL(12,2) NOT NULL,
    Itbis    DECIMAL(12,2) NOT NULL,
    Total    DECIMAL(12,2) NOT NULL
);
GO
/* El detalle guarda nombre y precio del momento de la venta (copia),
   y NO tiene llave foranea a Productos: asi se pueden editar o borrar
   productos sin alterar las facturas ya emitidas. */
IF OBJECT_ID('dbo.FacturaDetalle') IS NULL
CREATE TABLE dbo.FacturaDetalle(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    FacturaId INT           NOT NULL REFERENCES dbo.Factura(Id),
    Codigo    INT           NOT NULL,
    Nombre    NVARCHAR(100) NOT NULL,
    Cantidad  INT           NOT NULL CHECK (Cantidad > 0),
    Precio    DECIMAL(10,2) NOT NULL,
    Importe   DECIMAL(12,2) NOT NULL
);
GO
IF TYPE_ID('dbo.TipoDetalleFactura') IS NULL
CREATE TYPE dbo.TipoDetalleFactura AS TABLE(
    Codigo   INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0)
);
GO

/* ---------- CREAR FACTURA (todo o nada) ----------
   - Valida que existan los productos y que haya stock suficiente
   - Toma el precio actual desde la tabla Productos
   - Descuenta el stock
   - Calcula ITBIS 18%
   Devuelve el Id de la factura creada.                              */
CREATE OR ALTER PROCEDURE dbo.SP_CREA_FACTURA
    @cliente NVARCHAR(100),
    @usuario NVARCHAR(50),
    @detalle dbo.TipoDetalleFactura READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @tasa  DECIMAL(5,4) = 0.18;
    DECLARE @msg   NVARCHAR(200);
    DECLARE @id    INT;
    DECLARE @sub   DECIMAL(12,2);
    DECLARE @itbis DECIMAL(12,2);

    IF NOT EXISTS (SELECT 1 FROM @detalle)
        THROW 50001, N'La factura no tiene artículos.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT Codigo, SUM(Cantidad) AS Cantidad
        INTO #d
        FROM @detalle
        GROUP BY Codigo;

        SELECT TOP 1 @msg = N'El producto con código ' + CAST(d.Codigo AS NVARCHAR(10)) + N' ya no existe.'
        FROM #d d
        LEFT JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.Codigo = d.Codigo
        WHERE p.Codigo IS NULL;
        IF @msg IS NOT NULL THROW 50002, @msg, 1;

        SELECT TOP 1 @msg = N'Stock insuficiente de "' + p.Nombre + N'". Disponible: ' + CAST(p.Stock AS NVARCHAR(10)) + N'.'
        FROM #d d
        JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.Codigo = d.Codigo
        WHERE d.Cantidad > p.Stock;
        IF @msg IS NOT NULL THROW 50003, @msg, 1;

        INSERT INTO dbo.Factura(Cliente, Usuario, Subtotal, Itbis, Total)
        VALUES (ISNULL(NULLIF(LTRIM(RTRIM(@cliente)), N''), N'Consumidor final'), @usuario, 0, 0, 0);
        SET @id = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.FacturaDetalle(FacturaId, Codigo, Nombre, Cantidad, Precio, Importe)
        SELECT @id, p.Codigo, p.Nombre, d.Cantidad, p.Precio, d.Cantidad * p.Precio
        FROM #d d
        JOIN dbo.Productos p ON p.Codigo = d.Codigo;

        UPDATE p SET p.Stock = p.Stock - d.Cantidad
        FROM dbo.Productos p
        JOIN #d d ON d.Codigo = p.Codigo;

        SELECT @sub = SUM(Importe) FROM dbo.FacturaDetalle WHERE FacturaId = @id;
        SET @itbis = ROUND(@sub * @tasa, 2);

        UPDATE dbo.Factura
        SET Subtotal = @sub, Itbis = @itbis, Total = @sub + @itbis
        WHERE Id = @id;

        COMMIT TRANSACTION;
        SELECT @id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ---------- OBTENER FACTURA: devuelve 2 tablas (cabecera y detalle) ---------- */
CREATE OR ALTER PROCEDURE dbo.SP_OBTENER_FACTURA
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Numero, Fecha, Cliente, Usuario, Subtotal, Itbis, Total
    FROM dbo.Factura WHERE Id = @id;

    SELECT Codigo, Nombre, Cantidad, Precio, Importe
    FROM dbo.FacturaDetalle WHERE FacturaId = @id ORDER BY Id;
END
GO
