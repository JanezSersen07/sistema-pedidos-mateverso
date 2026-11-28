@echo off
chcp 65001 >nul
title Panel Mateverso
cd /d "%~dp0"
echo Iniciando el panel de Mateverso...
echo Cuando termines, cerra esta ventana o apreta Ctrl+C.
echo.
rem Abre el navegador a los pocos segundos, cuando el panel ya esta listo
start "" /min cmd /c "ping -n 6 127.0.0.1 >/dev/null & start http://localhost:5080"
dotnet run --project SistemaPedidos.Web
echo.
echo El panel se cerro. Si hubo un error, copialo o sacale una captura.
pause
