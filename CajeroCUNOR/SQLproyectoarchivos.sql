-- Insertar el Administrador
INSERT INTO Usuarios (Nombre, NumeroTarjeta, PIN, SaldoActual, LimiteDiario, EsAdmin) 
VALUES ('Administrador Jefe', '0000000000000000', '1234', 0, 0, 1);

-- Insertar un Usuario de Prueba
INSERT INTO Usuarios (Nombre, NumeroTarjeta, PIN, SaldoActual, LimiteDiario, EsAdmin) 
VALUES ('Edgar Prado', '1234567890123456', '1111', 5000.00, 2000.00, 0);