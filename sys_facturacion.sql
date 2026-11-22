-- =============================================================================
-- SCRIPT DE BASE DE DATOS: sys_facturacion
-- Motor: MySQL 5.7+ / 8.0+ / MariaDB
-- Proyecto: SysPuntoVenta (.NET 9 C# - CoreWCF + WPF)
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `sys_facturacion` 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

USE `sys_facturacion`;

SET FOREIGN_KEY_CHECKS = 0;

-- 1. TABLA: paises
DROP TABLE IF EXISTS `paises`;
CREATE TABLE `paises` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `descripcion` VARCHAR(100) NOT NULL,
    `codigo_iso` VARCHAR(10) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. TABLA: ciudades
DROP TABLE IF EXISTS `ciudades`;
CREATE TABLE `ciudades` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `descripcion` VARCHAR(100) NOT NULL,
    `idPais` INT NOT NULL DEFAULT 1,
    CONSTRAINT `fk_ciudades_paises` FOREIGN KEY (`idPais`) REFERENCES `paises`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. TABLA: seg_grupos (Roles del sistema)
DROP TABLE IF EXISTS `seg_grupos`;
CREATE TABLE `seg_grupos` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `nombre` VARCHAR(50) NOT NULL,
    `descripcion` VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. TABLA: seg_usuarios
DROP TABLE IF EXISTS `seg_usuarios`;
CREATE TABLE `seg_usuarios` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `nombre` VARCHAR(100) NOT NULL,
    `apellido` VARCHAR(100) NOT NULL,
    `userName` VARCHAR(50) NOT NULL UNIQUE,
    `userPassword` VARCHAR(255) NOT NULL,
    `email` VARCHAR(150) NULL,
    `edad` VARCHAR(10) NULL,
    `sexo` VARCHAR(20) NULL,
    `direccion` VARCHAR(255) NULL,
    `idCiudad` INT NULL,
    `idPais` INT NULL,
    `create_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `update_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `server` VARCHAR(50) NULL,
    `W` VARCHAR(50) NULL,
    CONSTRAINT `fk_usuarios_ciudades` FOREIGN KEY (`idCiudad`) REFERENCES `ciudades`(`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_usuarios_paises` FOREIGN KEY (`idPais`) REFERENCES `paises`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. TABLA: seg_grupo_usuario
DROP TABLE IF EXISTS `seg_grupo_usuario`;
CREATE TABLE `seg_grupo_usuario` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `id_usuario` INT NOT NULL,
    `id_grupo` INT NOT NULL,
    CONSTRAINT `fk_gu_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `seg_usuarios`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_gu_grupo` FOREIGN KEY (`id_grupo`) REFERENCES `seg_grupos`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. TABLA: impuestos
DROP TABLE IF EXISTS `impuestos`;
CREATE TABLE `impuestos` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `descripcion` VARCHAR(50) NOT NULL,
    `porcentaje` DECIMAL(5,2) NOT NULL DEFAULT 10.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 7. TABLA: proveedores
DROP TABLE IF EXISTS `proveedores`;
CREATE TABLE `proveedores` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `razon_social` VARCHAR(150) NOT NULL,
    `ruc` VARCHAR(30) NULL,
    `telefono` VARCHAR(50) NULL,
    `direccion` VARCHAR(255) NULL,
    `email` VARCHAR(150) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 8. TABLA: productos
DROP TABLE IF EXISTS `productos`;
CREATE TABLE `productos` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `codigo` VARCHAR(50) NOT NULL UNIQUE,
    `descripcion` VARCHAR(255) NOT NULL,
    `bruto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `precio_impuesto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `precio_neto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `idImpuesto` INT NOT NULL DEFAULT 1,
    `cantidad` INT NOT NULL DEFAULT 0,
    `idProveedor` INT NOT NULL DEFAULT 1,
    `create_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `ipCreacion` VARCHAR(50) NULL,
    `idUsuario` INT NULL,
    CONSTRAINT `fk_productos_impuesto` FOREIGN KEY (`idImpuesto`) REFERENCES `impuestos`(`id`),
    CONSTRAINT `fk_productos_proveedor` FOREIGN KEY (`idProveedor`) REFERENCES `proveedores`(`id`),
    CONSTRAINT `fk_productos_usuario` FOREIGN KEY (`idUsuario`) REFERENCES `seg_usuarios`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 9. TABLA: facturas (Ventas)
DROP TABLE IF EXISTS `facturas`;
CREATE TABLE `facturas` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `numero_factura` VARCHAR(50) NOT NULL,
    `fecha` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `documento_cliente` VARCHAR(30) NOT NULL,
    `nombre_cliente` VARCHAR(150) NULL,
    `total_bruto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `total_impuesto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `total_neto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `idUsuario` INT NULL,
    `estado` VARCHAR(20) DEFAULT 'EMITIDA',
    CONSTRAINT `fk_factura_usuario` FOREIGN KEY (`idUsuario`) REFERENCES `seg_usuarios`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 10. TABLA: factura_detalles
DROP TABLE IF EXISTS `factura_detalles`;
CREATE TABLE `factura_detalles` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `idFactura` INT NOT NULL,
    `idProducto` INT NOT NULL,
    `codigo` VARCHAR(50) NOT NULL,
    `descripcion` VARCHAR(255) NOT NULL,
    `cantidad` INT NOT NULL DEFAULT 1,
    `precio_unitario` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `subtotal` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT `fk_detalle_factura` FOREIGN KEY (`idFactura`) REFERENCES `facturas`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_detalle_producto` FOREIGN KEY (`idProducto`) REFERENCES `productos`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
-- INSERCIONES SEMILLA (DATOS DE PRUEBA Y CONFIGURACIÓN)
-- =============================================================================

INSERT INTO `paises` (`id`, `descripcion`, `codigo_iso`) VALUES
(1, 'Paraguay', 'PRY'),
(2, 'Argentina', 'ARG'),
(3, 'Brasil', 'BRA'),
(4, 'Chile', 'CHL');

INSERT INTO `ciudades` (`id`, `descripcion`, `idPais`) VALUES
(1, 'Asunción', 1),
(2, 'Ciudad del Este', 1),
(3, 'Encarnación', 1),
(4, 'Buenos Aires', 2),
(5, 'Córdoba', 2),
(6, 'Foz de Iguazú', 3);

INSERT INTO `seg_grupos` (`id`, `nombre`, `descripcion`) VALUES
(1, 'Administradores', 'Control total del sistema'),
(2, 'Cajeros / Vendedores', 'Acceso a facturación y consulta de stock'),
(3, 'Almacén', 'Gestión de productos y proveedores');

INSERT INTO `impuestos` (`id`, `descripcion`, `porcentaje`) VALUES
(1, 'IVA 10%', 10.00),
(2, 'IVA 5%', 5.00),
(3, 'Exento', 0.00);

INSERT INTO `proveedores` (`id`, `razon_social`, `ruc`, `telefono`, `direccion`, `email`) VALUES
(1, 'Distribuidora Central S.A.', '80012345-6', '021-555123', 'Av. Eusebio Ayala 1234', 'ventas@distribuidora.com'),
(2, 'Importadora del Este S.R.L.', '80098765-4', '061-500789', 'Monseñor Rodríguez 567', 'contacto@importadora.com');

INSERT INTO `seg_usuarios` (`id`, `nombre`, `apellido`, `userName`, `userPassword`, `email`, `edad`, `sexo`, `direccion`, `idCiudad`, `idPais`, `server`) VALUES
(1, 'Rodrigo', 'Admin', 'admin', 'admin', 'admin@sistema.com', '30', 'Masculino', 'Centro', 1, 1, '127.0.0.1'),
(2, 'Operador', 'Ventas', 'vendedor', '123456', 'vendedor@sistema.com', '25', 'Femenino', 'Villa Morra', 1, 1, '127.0.0.1');

INSERT INTO `seg_grupo_usuario` (`id`, `id_usuario`, `id_grupo`) VALUES
(1, 1, 1),
(2, 2, 2);

INSERT INTO `productos` (`id`, `codigo`, `descripcion`, `bruto`, `precio_impuesto`, `precio_neto`, `idImpuesto`, `cantidad`, `idProveedor`, `create_at`, `ipCreacion`, `idUsuario`) VALUES
(1, 'PROD001', 'Notebook Dell Inspiron 15', 5000000.00, 500000.00, 5500000.00, 1, 15, 1, NOW(), '127.0.0.1', 1),
(2, 'PROD002', 'Mouse Inalámbrico Logitech', 120000.00, 12000.00, 132000.00, 1, 50, 1, NOW(), '127.0.0.1', 1),
(3, 'PROD003', 'Teclado Mecánico RGB Redragon', 280000.00, 28000.00, 308000.00, 1, 30, 2, NOW(), '127.0.0.1', 1),
(4, 'PROD004', 'Monitor Samsung 24 FHD 75Hz', 950000.00, 95000.00, 1045000.00, 1, 20, 2, NOW(), '127.0.0.1', 1),
(5, 'PROD005', 'Disco Sólido SSD 1TB Kingston NV2', 450000.00, 45000.00, 495000.00, 1, 40, 1, NOW(), '127.0.0.1', 1);
