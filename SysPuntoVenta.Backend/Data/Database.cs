using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SysPuntoVenta.Backend.Data
{
    public class Database
    {
        private readonly string _connectionString;
        private readonly string _serverConnectionString;

        public Database(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Server=localhost;Port=3306;Database=sys_facturacion;Uid=root;Pwd=;AllowPublicKeyRetrieval=True;SslMode=None;";

            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                Database = ""
            };
            _serverConnectionString = builder.ConnectionString;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("Verificando existencia de base de datos 'sys_facturacion' en MySQL...");

                // 1. Crear base de datos si no existe
                await using (var serverConn = new MySqlConnection(_serverConnectionString))
                {
                    await serverConn.OpenAsync();
                    const string createDbSql = "CREATE DATABASE IF NOT EXISTS `sys_facturacion` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                    await using var cmdDb = new MySqlCommand(createDbSql, serverConn);
                    await cmdDb.ExecuteNonQueryAsync();
                }

                // 2. Crear tablas si no existen
                await using (var conn = CreateConnection())
                {
                    await conn.OpenAsync();

                    // Tabla clientes
                    const string sqlClientes = @"
                        CREATE TABLE IF NOT EXISTS `clientes` (
                            `id` INT AUTO_INCREMENT PRIMARY KEY,
                            `documento` VARCHAR(30) NOT NULL UNIQUE,
                            `nombre` VARCHAR(150) NOT NULL,
                            `telefono` VARCHAR(50) NULL,
                            `direccion` VARCHAR(255) NULL,
                            `email` VARCHAR(150) NULL,
                            `create_at` DATETIME DEFAULT CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                        
                        INSERT IGNORE INTO `clientes` (`id`, `documento`, `nombre`, `telefono`, `direccion`, `email`) 
                        VALUES (1, '4444444-1', 'Consumidor Final', '0981-111222', 'Asunción', 'cliente@sistema.com'),
                               (2, '80012345-6', 'Comercial del Este S.A.', '061-555888', 'Av. San Blas 456', 'ventas@comercialeste.com');";
                    await using (var cmdClientes = new MySqlCommand(sqlClientes, conn))
                    {
                        await cmdClientes.ExecuteNonQueryAsync();
                    }

                    const string checkTableSql = "SHOW TABLES LIKE 'seg_usuarios';";
                    await using var checkCmd = new MySqlCommand(checkTableSql, conn);
                    var tableExists = await checkCmd.ExecuteScalarAsync();

                    if (tableExists == null)
                    {
                        Console.WriteLine("Creando esquema inicial de tablas en 'sys_facturacion'...");
                        await CrearTablasPorDefectoAsync(conn);
                    }
                    else
                    {
                        Console.WriteLine("✔ Tablas verificadas en 'sys_facturacion'.");
                        await AsegurarColumnasAsync(conn);
                        await AsegurarUsuarioAdminAsync(conn);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO BD] Error al inicializar MySQL: {ex.Message}");
            }
        }

        private async Task AsegurarColumnasAsync(MySqlConnection conn)
        {
            try
            {
                // Asegurar columna email en clientes si la tabla fue heredada sin esa columna
                const string checkColSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'sys_facturacion' 
                      AND TABLE_NAME = 'clientes' 
                      AND COLUMN_NAME = 'email';";
                await using var cmdCheck = new MySqlCommand(checkColSql, conn);
                var exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    const string alterSql = "ALTER TABLE `clientes` ADD COLUMN `email` VARCHAR(150) NULL AFTER `telefono`;";
                    await using var cmdAlter = new MySqlCommand(alterSql, conn);
                    await cmdAlter.ExecuteNonQueryAsync();
                    Console.WriteLine("✔ Columna 'email' agregada a tabla 'clientes'.");
                }

                // Asegurar tabla seg_refresh_tokens para JWT
                const string createRefreshTokensSql = @"
                    CREATE TABLE IF NOT EXISTS `seg_refresh_tokens` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `id_usuario` INT NOT NULL,
                        `token` VARCHAR(255) NOT NULL UNIQUE,
                        `expira_el` DATETIME NOT NULL,
                        `revocado` BOOLEAN NOT NULL DEFAULT FALSE,
                        `creado_el` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        `creado_por_ip` VARCHAR(50) NULL,
                        INDEX idx_user_token (`id_usuario`, `token`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await using var cmdRefresh = new MySqlCommand(createRefreshTokensSql, conn);
                await cmdRefresh.ExecuteNonQueryAsync();

                // Asegurar tabla seg_password_resets para recuperación por correo
                const string createPasswordResetsSql = @"
                    CREATE TABLE IF NOT EXISTS `seg_password_resets` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `id_usuario` INT NOT NULL,
                        `codigo_otp` VARCHAR(10) NOT NULL,
                        `expira_el` DATETIME NOT NULL,
                        `utilizado` BOOLEAN NOT NULL DEFAULT FALSE,
                        `creado_el` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        `creado_por_ip` VARCHAR(50) NULL,
                        INDEX idx_user_otp (`id_usuario`, `codigo_otp`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                await using var cmdResets = new MySqlCommand(createPasswordResetsSql, conn);
                await cmdResets.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] No se pudo verificar o alterar columnas: {ex.Message}");
            }
        }

        private async Task AsegurarUsuarioAdminAsync(MySqlConnection conn)
        {
            try
            {
                const string countSql = "SELECT COUNT(*) FROM `seg_usuarios`;";
                await using var cmd = new MySqlCommand(countSql, conn);
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    Console.WriteLine("Insertando usuario admin por defecto...");
                    const string insertSql = @"
                        INSERT INTO `seg_usuarios` (`id`, `nombre`, `apellido`, `userName`, `userPassword`, `email`, `server`)
                        VALUES (1, 'Administrador', 'Sistema', 'admin', 'admin', 'admin@sistema.com', '127.0.0.1');";
                    await using var cmdInsert = new MySqlCommand(insertSql, conn);
                    await cmdInsert.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar usuario admin: {ex.Message}");
            }
        }

        private async Task CrearTablasPorDefectoAsync(MySqlConnection conn)
        {
            const string ddl = @"
                CREATE TABLE IF NOT EXISTS `paises` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `descripcion` VARCHAR(100) NOT NULL,
                    `codigo_iso` VARCHAR(10) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `ciudades` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `descripcion` VARCHAR(100) NOT NULL,
                    `idPais` INT NOT NULL DEFAULT 1
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `seg_grupos` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `nombre` VARCHAR(50) NOT NULL,
                    `descripcion` VARCHAR(255) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `seg_usuarios` (
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
                    `W` VARCHAR(50) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `seg_grupo_usuario` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `id_usuario` INT NOT NULL,
                    `id_grupo` INT NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `impuestos` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `descripcion` VARCHAR(50) NOT NULL,
                    `porcentaje` DECIMAL(5,2) NOT NULL DEFAULT 10.00
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `proveedores` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `razon_social` VARCHAR(150) NOT NULL,
                    `ruc` VARCHAR(30) NULL,
                    `telefono` VARCHAR(50) NULL,
                    `direccion` VARCHAR(255) NULL,
                    `email` VARCHAR(150) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `productos` (
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
                    `idUsuario` INT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `facturas` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `numero_factura` VARCHAR(50) NOT NULL,
                    `fecha` DATETIME DEFAULT CURRENT_TIMESTAMP,
                    `documento_cliente` VARCHAR(30) NOT NULL,
                    `nombre_cliente` VARCHAR(150) NULL,
                    `total_bruto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                    `total_impuesto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                    `total_neto` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                    `idUsuario` INT NULL,
                    `estado` VARCHAR(20) DEFAULT 'EMITIDA'
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `factura_detalles` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `idFactura` INT NOT NULL,
                    `idProducto` INT NOT NULL,
                    `codigo` VARCHAR(50) NOT NULL,
                    `descripcion` VARCHAR(255) NOT NULL,
                    `cantidad` INT NOT NULL DEFAULT 1,
                    `precio_unitario` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                    `subtotal` DECIMAL(12,2) NOT NULL DEFAULT 0.00
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                INSERT IGNORE INTO `paises` (`id`, `descripcion`, `codigo_iso`) VALUES (1, 'Paraguay', 'PRY');
                INSERT IGNORE INTO `ciudades` (`id`, `descripcion`, `idPais`) VALUES (1, 'Asunción', 1);
                INSERT IGNORE INTO `seg_grupos` (`id`, `nombre`, `descripcion`) VALUES (1, 'Administradores', 'Control total');
                INSERT IGNORE INTO `seg_usuarios` (`id`, `nombre`, `apellido`, `userName`, `userPassword`, `email`, `server`) VALUES (1, 'Administrador', 'Sistema', 'admin', 'admin', 'admin@sistema.com', '127.0.0.1');
                INSERT IGNORE INTO `seg_grupo_usuario` (`id`, `id_usuario`, `id_grupo`) VALUES (1, 1, 1);
                INSERT IGNORE INTO `impuestos` (`id`, `descripcion`, `porcentaje`) VALUES (1, 'IVA 10%', 10.00);
                INSERT IGNORE INTO `proveedores` (`id`, `razon_social`) VALUES (1, 'Distribuidora Central S.A.');
                INSERT IGNORE INTO `productos` (`id`, `codigo`, `descripcion`, `bruto`, `precio_impuesto`, `precio_neto`, `idImpuesto`, `cantidad`, `idProveedor`) 
                VALUES (1, 'PROD001', 'Notebook Dell Inspiron 15', 5000000.00, 500000.00, 5500000.00, 1, 15, 1);
            ";

            await using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("✔ Tablas por defecto creadas.");
        }
    }
}
