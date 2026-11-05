/* =====================================================
   BASE DE DATOS ALMACEN - SQL Server 2019 Express
   Ejecutar completo en SSMS
   ===================================================== */
IF DB_ID('ALMACEN') IS NULL CREATE DATABASE ALMACEN;
GO
USE ALMACEN;
GO

/* ---------- TABLAS ---------- */
IF OBJECT_ID('dbo.Productos') IS NULL
CREATE TABLE dbo.Productos(
    Codigo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,   -- numeración automática
    Nombre NVARCHAR(100)  NOT NULL,
    Precio DECIMAL(10,2)  NOT NULL CHECK (Precio >= 0),
    Stock  INT            NOT NULL CHECK (Stock  >= 0),
    Marca  NVARCHAR(60)   NOT NULL
);
GO
IF OBJECT_ID('dbo.usuario') IS NULL
CREATE TABLE dbo.usuario(
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    NombreUsuario NVARCHAR(50) NOT NULL UNIQUE,
    Clave         CHAR(64)     NOT NULL,              -- SHA256 en hexadecimal
    Estado        NVARCHAR(20) NOT NULL
                  CHECK (Estado IN ('Administrador','Invitado'))
);
GO

/* ---------- USUARIOS INICIALES (cambia las claves luego) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.usuario)
INSERT INTO dbo.usuario(NombreUsuario, Clave, Estado) VALUES
 ('admin',    CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONVERT(VARCHAR(50),'Admin123')),2),    'Administrador'),
 ('invitado', CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONVERT(VARCHAR(50),'Invitado123')),2), 'Invitado');
GO

/* ---------- PROCEDIMIENTOS ---------- */
CREATE OR ALTER PROCEDURE dbo._iniciosesion
    @usuario NVARCHAR(50), @clave CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Estado FROM dbo.usuario
    WHERE NombreUsuario = @usuario AND Clave = @clave;
END
GO
CREATE OR ALTER PROCEDURE dbo.Sp_ListarProductos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Nombre, Precio, Stock, Marca FROM dbo.Productos ORDER BY Codigo;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_SiguienteCodigo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(CONVERT(INT,last_value) + CONVERT(INT,increment_value), CONVERT(INT,seed_value)) AS Siguiente
    FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Productos');
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_INSERTA_PRODUCTO
    @nombre NVARCHAR(100), @precio DECIMAL(10,2), @stock INT, @marca NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Productos(Nombre, Precio, Stock, Marca) VALUES(@nombre,@precio,@stock,@marca);
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_ACTUALIZA_PRODUCTO
    @codigo INT, @nombre NVARCHAR(100), @precio DECIMAL(10,2), @stock INT, @marca NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Productos SET Nombre=@nombre, Precio=@precio, Stock=@stock, Marca=@marca
    WHERE Codigo=@codigo;
    SELECT @@ROWCOUNT;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_EliminarProducto
    @codigo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Productos WHERE Codigo=@codigo;
    SELECT @@ROWCOUNT;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_BuscarProductoPorCodigo1
    @codigo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Nombre, Precio, Stock, Marca FROM dbo.Productos WHERE Codigo=@codigo;
END
GO
/* Filtro en tiempo real: @campo = 'Codigo' | 'Nombre' | 'Marca' */
CREATE OR ALTER PROCEDURE dbo.SP_BUSCA_PRODUCTO
    @campo NVARCHAR(10), @valor NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Nombre, Precio, Stock, Marca FROM dbo.Productos
    WHERE (@campo='Codigo' AND CONVERT(VARCHAR(20),Codigo) LIKE @valor + '%')
       OR (@campo='Nombre' AND Nombre LIKE '%' + @valor + '%')
       OR (@campo='Marca'  AND Marca  LIKE '%' + @valor + '%')
    ORDER BY Codigo;
END
GO
CREATE OR ALTER PROCEDURE dbo.Sp_BuscarProductoPorPrecio1
    @precio DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Nombre, Precio, Stock, Marca FROM dbo.Productos WHERE Precio=@precio;
END
GO
CREATE OR ALTER PROCEDURE dbo.Sp_BuscarProductoPorStock1
    @stock INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Nombre, Precio, Stock, Marca FROM dbo.Productos WHERE Stock=@stock;
END
GO

/* ---------- DATOS DE PRUEBA ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Productos)
INSERT INTO dbo.Productos(Nombre,Precio,Stock,Marca) VALUES
 (N'Laptop 15"',  35000.00, 10, N'Lenovo'),
 (N'Mouse USB',     350.00, 50, N'Logitech'),
 (N'Teclado',       800.00, 30, N'HP');
GO
