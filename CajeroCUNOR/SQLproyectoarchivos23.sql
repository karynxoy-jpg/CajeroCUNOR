CREATE DATABASE CajeroATM;
GO

USE CajeroATM;
GO

CREATE TABLE Usuarios (
    UsuarioID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    NumeroTarjeta VARCHAR(16) UNIQUE NOT NULL,
    PIN VARCHAR(4) NOT NULL,
    TokenRFID VARCHAR(50) NULL,
    SaldoActual DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    LimiteDiario DECIMAL(10,2) NOT NULL,
    LimiteUsadoHoy DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    EsAdmin BIT DEFAULT 0,
    CambioPIN BIT DEFAULT 0,
    UltimoAcceso DATETIME NULL
);

CREATE TABLE BilletesCajero (
    Denominacion INT PRIMARY KEY,
    Cantidad INT NOT NULL DEFAULT 0
);
INSERT INTO BilletesCajero (Denominacion, Cantidad) VALUES (200,0), (100,0), (50,0), (20,0), (10,0), (5,0), (1,0);

CREATE TABLE Transacciones (
    TransaccionID INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Tipo VARCHAR(10) CHECK (Tipo IN ('RETIRO', 'DEPOSITO')),
    Monto DECIMAL(10,2) NOT NULL,
    FechaHora DATETIME DEFAULT GETDATE()
);

-- Crear Administrador 
INSERT INTO Usuarios (Nombre, NumeroTarjeta, PIN, SaldoActual, LimiteDiario, EsAdmin) 
VALUES ('Administrador Jefe', '0000000000000000', '1234', 0, 0, 1);

-- Crear Usuario Normal 
INSERT INTO Usuarios (Nombre, NumeroTarjeta, PIN, SaldoActual, LimiteDiario, EsAdmin) 
VALUES ('Edgar Prado', '1234567890123456', '1111', 5000.00, 2000.00, 0);