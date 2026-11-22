# SysFacturación - Sistema de Punto de Venta & Gestión Comercial (.NET 9 C#)

Solución empresarial moderna de punto de venta (POS), gestión de inventario, facturación y auditoría, desarrollada con arquitectura desacoplada y multicapa en **.NET 9**, consumible tanto por clientes de escritorio (**WPF**) como por aplicaciones móviles (**Flutter, React Native, MAUI o Kotlin**).

---

## 🏛 Arquitectura de la Solución

```
C:\proyectos\sys_punto_venta_net\
├── SysPuntoVenta.sln                  # Solución unificada en .NET 9
├── sys_facturacion.sql                # Script DDL + DML inicial para MySQL
├── .gitignore                         # Exclusión de binarios y temporales
├── nuget.config
│
├── SysPuntoVenta.Common/             # Biblioteca de Clases compartida (Desktop & Móvil)
│   └── Models/                       # DTOs y modelos serializables JSON ([DataContract], [DataMember])
│
├── SysPuntoVenta.Backend/            # API RESTful JSON (.NET 9 Web API) + CoreWCF
│   ├── Controllers/                  # Controladores REST JSON (Auth, Clientes, Facturación, Informes, etc.)
│   ├── Services/                     # Lógica de negocio, Tokens JWT y Servicio de Email
│   ├── Data/                         # Acceso a datos MySQL con MySqlConnector y auto-migraciones
│   └── Program.cs                    # Configuración de Kestrel (localhost:5050), JWT y middlewares
│
└── SysPuntoVenta.Frontend/           # Cliente de Escritorio WPF (.NET 9)
    ├── App.xaml                      # Estilos y controles dinámicos con ThemeManager
    ├── Themes/                       # Diccionarios de temas (DarkTheme.xaml y LightTheme.xaml)
    ├── Services/                     # Cliente HTTP (ApiClient), Exportador PDF (QuestPDF) y Sesión
    └── Views/                        # Interfaces de usuario compactas y minimalistas
        ├── LoginWindow.xaml          # Inicio de sesión con JWT y recuperación de clave
        ├── ForgotPasswordWindow.xaml # Diálogo de recuperación con código OTP por correo
        ├── MainWindow.xaml           # Panel principal con navegación lateral y selector de tema
        ├── DashboardControl.xaml     # Panel de control con métricas en tiempo real
        ├── FacturacionControl.xaml   # Punto de venta, detalle, IVA 10% y emisión de comprobantes
        ├── ProductosControl.xaml     # Catálogo de productos, cálculo automático de impuestos y stock
        ├── BuscadorProductoDialog.xaml # Modal de búsqueda asistida de artículos
        ├── ClientesControl.xaml      # Gestión de cartera de clientes
        ├── InformesControl.xaml      # Módulo de informes en grillas y exportación a PDF
        ├── UsuariosControl.xaml      # Administración de usuarios y roles
        └── CiudadesControl.xaml      # Catálogo geográfico de ciudades
```

---

## 🔒 Seguridad y Autenticación

* **Autenticación JWT**: Emisión de Bearer Access Tokens y Refresh Tokens de rotación automática.
* **Cifrado de Contraseñas**: Hashing criptográfico robusto con **BCrypt** (reemplazo de hashes heredados MD5).
* **Recuperación Segura por Correo**: Verificación de identidad mediante código OTP de 6 dígitos de un solo uso.
* **Separación de Privilegios**: Restricción estricta de roles; solo los administradores autenticados pueden crear o asignar nuevos usuarios administradores.
* **Protección de Datos Sensibles**: Exclusión obligatoria de contraseñas de producción, archivos de volcado, secretos locales y claves privadas mediante `.gitignore`.

---

## 🎨 Temas Visuales Dinámicos

La interfaz cuenta con dos paletas completas conmutables al instante sin reiniciar la aplicación:
* **Modo Oscuro (Cyber Dark & Violet)**: Tonos `#0B0F19`, `#111625` y acentos violetas/azules.
* **Modo Claro (Modern Slate & Violet)**: Fondos `#F1F5F9`, `#FFFFFF` con textos en gris pizarra oscuro `#0F172A` para máximo contraste y legibilidad.

---

## 🚀 Puesta en Marcha en Entornos de Desarrollo

### 1. Base de Datos MySQL
1. Abre tu gestor MySQL preferido (MySQL Workbench, DBeaver, phpMyAdmin o CLI).
2. Ejecuta el script de estructura inicial:
   ```sql
   source sys_facturacion.sql;
   ```
3. Configura tus credenciales locales en `SysPuntoVenta.Backend\appsettings.json` o mediante variables de entorno:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=sys_facturacion;Uid=TU_USUARIO_MYSQL;Pwd=TU_PASSWORD_MYSQL;AllowPublicKeyRetrieval=True;SslMode=None;"
   }
   ```
   > **Nota de Seguridad**: Nunca guardes contraseñas de producción en archivos de configuración rastreados por Git. Puedes utilizar un archivo `appsettings.Local.json` (ignorado por `.gitignore`) o variables de entorno del sistema operativo.

### 2. Iniciar el Backend (API REST)
Ejecuta el script o mediante consola:
```powershell
.\iniciar_backend.bat
```
O directamente con el CLI de .NET:
```powershell
cd SysPuntoVenta.Backend
dotnet run
```
La API estará disponible en `http://localhost:5050/api` y responderá al endpoint de diagnóstico en `http://localhost:5050/api/dashboard/ping`.

### 3. Iniciar el Frontend (Cliente Desktop WPF)
Ejecuta el script o mediante consola:
```powershell
.\iniciar_frontend.bat
```
O directamente con el CLI de .NET:
```powershell
cd SysPuntoVenta.Frontend
dotnet run
```

---

## 📄 Licencia y Uso
Proyecto desarrollado con propósitos empresariales y de gestión comercial. Todos los derechos reservados.
