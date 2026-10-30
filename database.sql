CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `bicicletas` (
        `id_bicicleta` int NOT NULL AUTO_INCREMENT,
        `nombre_bicicleta` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `estado` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'libre',
        CONSTRAINT `PK_bicicletas` PRIMARY KEY (`id_bicicleta`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `historialreservaciones` (
        `id_historial` int NOT NULL AUTO_INCREMENT,
        `id_reservacion` int NOT NULL,
        `id_cliente` int NOT NULL,
        `id_horario` int NOT NULL,
        `id_bicicleta` int NOT NULL,
        `nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `apellidos` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `talla` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `estado` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'finalizado',
        `fecha_registro` datetime(6) NOT NULL,
        CONSTRAINT `PK_historialreservaciones` PRIMARY KEY (`id_historial`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `horarios` (
        `id_horario` int NOT NULL AUTO_INCREMENT,
        `hora_inicio` time(6) NOT NULL,
        CONSTRAINT `PK_horarios` PRIMARY KEY (`id_horario`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `membresias` (
        `id_membresia` int NOT NULL AUTO_INCREMENT,
        `id_cliente` int NOT NULL,
        `fecha_inicio` datetime(6) NOT NULL,
        `fecha_expiracion` datetime(6) NOT NULL,
        `estado` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Activa',
        CONSTRAINT `PK_membresias` PRIMARY KEY (`id_membresia`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `reservaciones` (
        `id_reservacion` int NOT NULL AUTO_INCREMENT,
        `id_cliente` int NOT NULL,
        `nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `apellidos` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `talla` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `id_horario` int NOT NULL,
        `id_bicicleta` int NOT NULL,
        CONSTRAINT `PK_reservaciones` PRIMARY KEY (`id_reservacion`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE TABLE `usuarios` (
        `id_usuario` int NOT NULL AUTO_INCREMENT,
        `nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `apellidos` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `edad` int NOT NULL,
        `email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `password` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `telefono` varchar(20) CHARACTER SET utf8mb4 NULL,
        `tipo_usuario` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `sessionToken` varchar(100) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_usuarios` PRIMARY KEY (`id_usuario`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE INDEX `IX_membresias_id_cliente` ON `membresias` (`id_cliente`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE INDEX `IX_reservaciones_id_horario_id_bicicleta` ON `reservaciones` (`id_horario`, `id_bicicleta`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_usuarios_email` ON `usuarios` (`email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261030000219_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261030000219_InitialCreate', '8.0.8');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

