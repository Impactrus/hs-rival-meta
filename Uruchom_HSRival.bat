@echo off
title Uruchom HSRival Dashboard
echo ===================================================
echo   Uruchamianie HSRival Dashboard (Klon HSReplay)   
echo ===================================================
echo.

:: Uruchomienie backendu w osobnym, zminimalizowanym oknie
echo [1/3] Uruchamianie serwera Backend w tle...
start "HSRival Backend" /min cmd /c "cd /d "%~dp0HSRivalDashboard\backend" && node server.js"

:: Uruchomienie frontendu w osobnym, zminimalizowanym oknie
echo [2/3] Uruchamianie serwera Frontend (Vite) w tle...
start "HSRival Frontend" /min cmd /c "cd /d "%~dp0HSRivalDashboard\frontend" && npm run dev"

:: Oczekiwanie na inicjalizację serwerów (3 sekundy)
echo [3/3] Oczekiwanie na start serwerow...
timeout /t 3 /nobreak >nul

:: Otwarcie przeglądarki na lokalnym adresie frontendu
echo Otwieranie przegladarki...
start http://localhost:5173/

echo.
echo Gotowe! Serwery dzialaja w zminimalizowanych oknach.
echo Mozesz zamknac to okno. Aby wylaczyc serwery, uzyj pliku "Zatrzymaj_HSRival.bat".
timeout /t 4 >nul
