@echo off
chcp 65001 >nul
setlocal enableextensions
title CarCare360 - Запуск сервера и мобильного приложения

rem ============================================================
rem  Быстрый запуск:
rem   1) серверный API (.NET) в отдельном окне (порт 5009)
rem   2) Android-эмулятор
rem   3) установка APK на эмулятор и запуск приложения
rem  Перед первым запуском выполните "Установить.bat".
rem ============================================================

rem -- Пути относительно расположения этого файла (RUN\) --
for %%I in ("%~dp0..")    do set "MOBILE=%%~fI"
for %%I in ("%~dp0..\..") do set "PROJ=%%~fI"
set "API=%PROJ%\CarCare360.Api"
set "APK=%MOBILE%\build\app\outputs\flutter-apk\app-debug.apk"
set "PKG=com.carcare360.carcare_mobile"

rem -- Android SDK (по умолчанию %LOCALAPPDATA%\Android\Sdk) --
set "SDK=%LOCALAPPDATA%\Android\Sdk"
if defined ANDROID_SDK_ROOT set "SDK=%ANDROID_SDK_ROOT%"
if defined ANDROID_HOME      set "SDK=%ANDROID_HOME%"
set "ADB=%SDK%\platform-tools\adb.exe"
set "EMULATOR=%SDK%\emulator\emulator.exe"

echo ============================================================
echo  Запуск CarCare 360 (сервер + мобильное приложение)
echo ============================================================
echo.

rem -- Проверки --
where dotnet >nul 2>nul || ( echo [ОШИБКА] .NET SDK не найден. & goto :end )
if not exist "%ADB%"      ( echo [ОШИБКА] adb не найден: %ADB% & goto :end )
if not exist "%EMULATOR%" ( echo [ОШИБКА] эмулятор не найден: %EMULATOR% & goto :end )
if not exist "%APK%" (
  echo [ОШИБКА] APK не найден: %APK%
  echo          Сначала запустите "Установить.bat".
  goto :end
)

rem -- 1) Сервер API в отдельном окне (если ещё не запущен) --
powershell -NoProfile -Command "try { (New-Object Net.Sockets.TcpClient).Connect('127.0.0.1',5009); exit 0 } catch { exit 1 }" >nul 2>nul
if errorlevel 1 (
  echo [1/4] Запуск сервера API ^(http://localhost:5009^) в отдельном окне...
  start "CarCare360 API :5009" /d "%API%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development& dotnet run --no-launch-profile --urls http://localhost:5009"
) else (
  echo [1/4] Сервер API уже работает на http://localhost:5009 - запуск не требуется.
)

rem -- 2) Выбор AVD и запуск эмулятора --
set "AVD="
for /f "usebackq delims=" %%A in (`"%EMULATOR%" -list-avds`) do if not defined AVD set "AVD=%%A"
if not defined AVD (
  echo [ОШИБКА] Не найдено ни одного AVD. Создайте эмулятор в Android Studio (Device Manager).
  goto :end
)

"%ADB%" start-server >nul 2>nul
"%ADB%" get-state >nul 2>nul
if errorlevel 1 (
  echo [2/4] Запуск эмулятора "%AVD%"...
  start "Android Emulator" "%EMULATOR%" -avd "%AVD%"
) else (
  echo [2/4] Эмулятор уже запущен.
)

rem -- 3) Ожидание загрузки Android --
echo [3/4] Ожидание загрузки Android (это может занять 1-3 минуты)...
"%ADB%" wait-for-device
set /a TRIES=0
:waitboot
"%ADB%" shell getprop sys.boot_completed 2>nul | findstr /b "1" >nul
if not errorlevel 1 goto booted
set /a TRIES+=1
if %TRIES% geq 80 ( echo [ОШИБКА] Эмулятор не загрузился за отведённое время. & goto :end )
timeout /t 3 >nul
goto waitboot
:booted

rem -- 4) Установка и запуск приложения --
echo [4/4] Установка APK и запуск приложения...
"%ADB%" install -r "%APK%"
"%ADB%" shell monkey -p %PKG% -c android.intent.category.LAUNCHER 1 >nul 2>nul

echo.
echo ============================================================
echo  ГОТОВО!
echo   - Сервер API:  http://localhost:5009  (окно "CarCare360 API :5009")
echo   - Мобильное приложение запущено на эмуляторе.
echo   - Вход клиентом:  vadim / vadim123
echo.
echo  НЕ закрывайте окно сервера API, пока пользуетесь приложением.
echo ============================================================

:end
echo.
pause
endlocal
