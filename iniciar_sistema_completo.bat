@echo off
title SysPuntoVenta Launcher
echo ========================================================
echo   SYS FACTURACION - INICIADOR COMPLETO
echo ========================================================
echo 1. Levantando Backend REST JSON en segundo plano...
start "SysPuntoVenta Backend REST" cmd /k "%~dp0iniciar_backend.bat"

echo 2. Esperando a que el Backend este listo en el puerto 5050...
timeout /t 4 /nobreak >nul

echo 3. Iniciando Cliente WPF...
start "SysPuntoVenta Frontend WPF" cmd /c "%~dp0iniciar_frontend.bat"
echo Sistema levantado. Puedes cerrar esta ventana.
