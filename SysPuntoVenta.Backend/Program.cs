using System;
using System.Text;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SysPuntoVenta.Backend.Contracts;
using SysPuntoVenta.Backend.Data;
using SysPuntoVenta.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios REST API JSON y CORS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 2. Base de Datos y Servicios de Dominio
builder.Services.AddSingleton<Database>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 3. Autenticación y Autorización JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SysPuntoVenta_SuperSecretKey_2026_Net9_JWT_Token_Key!#*99";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SysPuntoVenta.Backend",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SysPuntoVenta.Clients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();

// 4. Servicios CoreWCF (Compatibilidad SOAP)
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<ProductoService>();
builder.Services.AddTransient<FacturacionService>();
builder.Services.AddTransient<CatalogosService>();

var app = builder.Build();

// Inicialización Automática de Base de Datos MySQL y Tablas
var database = app.Services.GetRequiredService<Database>();
await database.InitializeDatabaseAsync();

// Middlewares
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de Controladores REST JSON
app.MapControllers();

// Configuración de CoreWCF (Endpoints SOAP)
app.UseServiceModel(serviceBuilder =>
{
    var binding = new BasicHttpBinding
    {
        MaxReceivedMessageSize = 10 * 1024 * 1024
    };

    serviceBuilder.AddService<AuthService>()
        .AddServiceEndpoint<AuthService, IAuthService>(binding, "/AuthService.svc");

    serviceBuilder.AddService<ProductoService>()
        .AddServiceEndpoint<ProductoService, IProductoService>(binding, "/ProductoService.svc");

    serviceBuilder.AddService<FacturacionService>()
        .AddServiceEndpoint<FacturacionService, IFacturacionService>(binding, "/FacturacionService.svc");

    serviceBuilder.AddService<CatalogosService>()
        .AddServiceEndpoint<CatalogosService, ICatalogosService>(binding, "/CatalogosService.svc");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

// Página de Bienvenida y Portal de APIs
app.MapGet("/", () => Results.Text(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>SysPuntoVenta API Portal</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0B0F19; color: #F8FAFC; margin: 40px; }
        .card { background: #161B2E; border: 1px solid #2E2856; border-radius: 12px; padding: 24px; max-width: 800px; margin-bottom: 20px; }
        h1 { color: #8B5CF6; margin-top: 0; }
        .badge { display: inline-block; background: #2563EB; color: white; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: bold; }
        .badge-jwt { display: inline-block; background: #7C3AED; color: white; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: bold; margin-left: 6px; }
        ul { line-height: 1.8; color: #94A3B8; }
        code { background: #0F1423; color: #38BDF8; padding: 2px 6px; border-radius: 4px; font-family: monospace; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>SysPuntoVenta - Servidor Backend .NET 9</h1>
        <p><span class='badge'>REST API JSON ACTIVO</span><span class='badge-jwt'>JWT + REFRESH TOKENS</span></p>
        <p>Servicios disponibles para clientes Desktop (WPF) y Aplicaciones Móviles:</p>
        <ul>
            <li><code>POST /api/auth/login</code> - Autenticación con JWT Bearer Token y Refresh Token</li>
            <li><code>POST /api/auth/refresh</code> - Renovación automática de tokens para apps móviles</li>
            <li><code>GET /api/dashboard/stats</code> - Métricas de ventas y KPIs en tiempo real [Protegido]</li>
            <li><code>GET /api/clientes</code> - Gestión y cartera de clientes [Protegido]</li>
            <li><code>GET /api/productos</code> - Catálogo de productos y cálculo de IVA [Protegido]</li>
            <li><code>POST /api/facturacion/crear</code> - Emisión de comprobantes y ventas POS [Protegido]</li>
        </ul>
    </div>
</body>
</html>
", "text/html"));

Console.WriteLine("===================================================================");
Console.WriteLine("Servidor SysPuntoVenta Backend (REST JSON JWT + WCF) iniciado en http://localhost:5050");
Console.WriteLine("===================================================================");

app.Run();
