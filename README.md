# SysPuntoVenta - Sistema de Facturación y Punto de Venta (.NET 9 C#)

Réplica modernizada y desacoplada del proyecto Java `sys_facturacion_r`.

---

## 🏛 Arquitectura de la Solución

```
C:\proyectos\sys_punto_venta_net\
├── SysPuntoVenta.sln                  # Solución Visual Studio / .NET 9
├── sys_facturacion.sql                # Script DDL + DML para MySQL
├── nuget.config
│
├── SysPuntoVenta.Common/             # Biblioteca de Clases compartida
│   └── Models/                       # DTOs y DataContracts ([DataContract], [DataMember])
│
├── SysPuntoVenta.Backend/            # Backend de Servicios WCF (CoreWCF en .NET 9)
│   ├── Contracts/                    # Interfaces de servicio CoreWCF ([ServiceContract])
│   ├── Services/                     # Lógica de negocio y servicios WCF
│   ├── Data/                         # Acceso a base de datos MySQL (MySqlConnector)
│   └── Program.cs                    # Configuración de Kestrel (localhost:5050) y endpoints SOAP
│
└── SysPuntoVenta.Frontend/           # Cliente de Escritorio WPF (.NET 9)
    ├── App.xaml                      # Paleta de colores moderna (Azul, Negro, Violeta)
    ├── Services/                     # Proxies WCF (ChannelFactory con System.ServiceModel)
    └── Views/                        # Interfaces XAML
        ├── LoginWindow.xaml          # Inicio de sesión con validación WCF
        ├── SignUpWindow.xaml         # Registro de nuevos usuarios
        ├── MainWindow.xaml           # Panel principal con navegación lateral y reloj en vivo
        ├── FacturacionControl.xaml   # Punto de venta, detalle, subtotales, IVA 10% y emisión
        ├── ProductosControl.xaml     # CRUD de productos con cálculo de IVA y precios
        ├── BuscadorProductoDialog.xaml # Modal rápido de búsqueda y selección de artículos
        ├── CiudadesControl.xaml      # Mantenimiento de ciudades
        └── UsuariosControl.xaml      # Visualización de usuarios y accesos
```

---

## 🎨 Paleta de Estilos

* **Fondos**: `#0B0F19` (Negro Profundo), `#111625` (Superficie), `#161B2E` (Tarjetas).
* **Color Primario**: `#8B5CF6` / `#A855F7` (Violeta Elegante).
* **Color Secundario**: `#2563EB` / `#38BDF8` (Azul Eléctrico / Cian).
* **Gradiente Principal**: LinearGradient de `#2563EB` a `#7C3AED` a `#9333EA`.

---

## 🚀 Puesta en Marcha

### 1. Base de Datos MySQL
1. Abre tu gestor MySQL favorito (phpMyAdmin, MySQL Workbench, DBeaver, HeidiSQL o CLI).
2. Ejecuta el script:
   ```sql
   source C:\proyectos\sys_punto_venta_net\sys_facturacion.sql;
   ```
3. Verifica la conexión en `SysPuntoVenta.Backend\appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=sys_facturacion;Uid=root;Pwd=;AllowPublicKeyRetrieval=True;SslMode=None;"
   }
   ```

### 2. Iniciar el Backend (Servicio WCF)
Abre una terminal y ejecuta:
```powershell
cd C:\proyectos\sys_punto_venta_net\SysPuntoVenta.Backend
dotnet run
```
El servicio estará disponible en `http://localhost:5050/` y sus endpoints WSDL:
* `http://localhost:5050/AuthService.svc?wsdl`
* `http://localhost:5050/ProductoService.svc?wsdl`
* `http://localhost:5050/FacturacionService.svc?wsdl`
* `http://localhost:5050/CatalogosService.svc?wsdl`

### 3. Iniciar el Frontend (Cliente WPF)
En otra terminal o desde Visual Studio:
```powershell
cd C:\proyectos\sys_punto_venta_net\SysPuntoVenta.Frontend
dotnet run
```

### 4. Credenciales Iniciales
* **Usuario**: `admin`
* **Contraseña**: `admin`
*(También disponible usuario `vendedor` con clave `123456`)*
