@echo off
title Zatrzymaj HSRival Dashboard
echo ===================================================
echo   Zatrzymywanie HSRival Dashboard (Klon HSReplay)  
echo ===================================================
echo.

echo Wylaczanie procesow Node.js (serwera backendu i frontendu)...
taskkill /f /im node.exe >nul 2>&1

echo.
echo Sukces! Wszystkie serwery HSRival zostaly wylaczone.
echo Mozesz juz zamknac to okno.
timeout /t 3 >nul
