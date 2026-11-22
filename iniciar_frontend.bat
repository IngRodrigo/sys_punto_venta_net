@echo off
title SysPuntoVenta - Cliente WPF
echo ========================================================
echo   SYS FACTURACION - CLIENTE WPF (.NET 9)
echo ========================================================
echo Iniciando cliente de escritorio...
cd /d "%~dp0SysPuntoVenta.Frontend\bin\Debug\net9.0-windows"
if exist "SysPuntoVenta.Frontend.exe" (
    SysPuntoVenta.Frontend.exe
) else (
    cd /d "%~dp0SysPuntoVenta.Frontend"
    dotnet run
)
pause
