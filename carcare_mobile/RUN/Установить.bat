@echo off
chcp 65001 >nul
setlocal enableextensions
title CarCare360 - Установка зависимостей (сервер + мобильное)

rem ============================================================
rem  Скачивает и подготавливает всё необходимое:
rem   - NuGet-пакеты серверного API (.NET)
rem   - пакеты Flutter для мобильного приложения
rem   - собирает отладочный APK
rem  Запускать ОДИН раз перед первым использованием "Запустить.bat".
rem ============================================================

rem -- Пути относительно расположения этого файла (RUN\) --
for %%I in ("%~dp0..")    do set "MOBILE=%%~fI"
for %%I in ("%~dp0..\..") do set "PROJ=%%~fI"
set "API=%PROJ%\CarCare360.Api"

echo ============================================================
echo  Установка зависимостей CarCare 360
echo  Сервер (API):  %API%
echo  Мобильное:     %MOBILE%
echo ============================================================
echo.

rem -- Проверка .NET SDK --
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ОШИБКА] .NET SDK не найден. Установите .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
  goto :end
)

rem -- Поиск Flutter SDK (в PATH либо в C:\src\flutter) --
set "FLUTTER="
where flutter >nul 2>nul && set "FLUTTER=flutter"
if not defined FLUTTER if exist "C:\src\flutter\bin\flutter.bat" set "FLUTTER=C:\src\flutter\bin\flutter.bat"
if not defined FLUTTER (
  echo [ОШИБКА] Flutter SDK не найден ни в PATH, ни в C:\src\flutter.
  echo          Установите Flutter: https://docs.flutter.dev/get-started/install/windows
  goto :end
)

echo [1/4] Восстановление NuGet-пакетов сервера (dotnet restore)...
dotnet restore "%API%\CarCare360.Api.csproj"
if errorlevel 1 ( echo [ОШИБКА] dotnet restore завершился с ошибкой. & goto :end )
echo.

rem -- Обход кириллицы в пути: junction C:\cc360 для flutter/gradle --
set "WORK=%MOBILE%"
echo %MOBILE%| findstr /r "[^ -~]" >nul
if not errorlevel 1 (
  if not exist "C:\cc360\pubspec.yaml" mklink /J "C:\cc360" "%MOBILE%" >nul 2>nul
  if exist "C:\cc360\pubspec.yaml" set "WORK=C:\cc360"
)
echo Рабочая папка для Flutter: %WORK%
echo.

echo [2/4] Загрузка пакетов Flutter (flutter pub get)...
pushd "%WORK%"
call "%FLUTTER%" pub get
if errorlevel 1 ( echo [ОШИБКА] flutter pub get завершился с ошибкой. & popd & goto :end )
echo.

echo [3/4] Сборка отладочного APK (flutter build apk --debug).
echo       Это самый долгий шаг, может занять несколько минут...
call "%FLUTTER%" build apk --debug
if errorlevel 1 ( echo [ОШИБКА] Сборка APK завершилась с ошибкой. & popd & goto :end )
popd
echo.

echo [4/4] Готово.
echo.
echo APK собран: %MOBILE%\build\app\outputs\flutter-apk\app-debug.apk
echo Теперь запустите "Запустить.bat" для старта сервера и мобильного приложения.

:end
echo.
pause
endlocal
