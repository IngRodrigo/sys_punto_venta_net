@echo off
title SysPuntoVenta - Backend REST JSON (.NET 9)
echo ========================================================
echo   SYS FACTURACION - BACKEND REST API JSON (.NET 9)
echo ========================================================

:: Liberar puerto 5050 si quedo algun proceso colgado previo
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5050" ^| findstr "LISTENING"') do (
    echo El puerto 5050 esta ocupado por el PID %%a.
    echo Si este proceso corresponde a Visual Studio, el backend ya esta activo.
    echo Si no, procedemos a liberarlo...
    taskkill /F /PID %%a >nul 2>&1
    timeout /t 1 >nul
)

echo Iniciando servicio en http://localhost:5050 ...
cd /d "%~dp0SysPuntoVenta.Backend\bin\Debug\net9.0"
if exist "SysPuntoVenta.Backend.exe" (
    SysPuntoVenta.Backend.exe
) else (
    cd /d "%~dp0SysPuntoVenta.Backend"
    dotnet run --no-launch-profile
)
pause
