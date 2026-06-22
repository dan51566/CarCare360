# Сборка установщика десктопного приложения CarCare 360

Скрипт [CarCare360Setup.iss](CarCare360Setup.iss) собирает единый установщик
`CarCare360Setup.exe` из self-contained публикации десктопного приложения.

## Требования

**На машине разработчика (для сборки установщика):**
- .NET SDK 10 — для `dotnet publish`.
- Inno Setup **6.3+** — для `ISCC.exe`. Установка, например, через winget:
  ```powershell
  winget install JRSoftware.InnoSetup
  ```
  ISCC по умолчанию: `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`.

**На машине пользователя (для установки):**
- Windows 10 версии 2004 (**сборка 19041**) и новее, x64.
- Предварительно установленный .NET НЕ требуется — он вшит в self-contained сборку.

## Шаги сборки

1. Опубликовать десктоп (self-contained, win-x64, без отладочных символов) —
   из корня репозитория:
   ```powershell
   Remove-Item -Recurse -Force CarCare360_Publish -ErrorAction SilentlyContinue
   dotnet publish CarCare360.Desktop\CarCare360.Desktop.csproj -c Release `
     -r win-x64 --self-contained true /p:DebugType=None /p:DebugSymbols=false `
     -o CarCare360_Publish
   ```
   - `--self-contained true` — вшивает .NET-рантайм: на машине сотрудника не нужен
     предварительно установленный .NET (цель ТЗ «один файл без предусловий»).
   - `/p:DebugType=None /p:DebugSymbols=false` — не включать `.pdb` в дистрибутив.

2. Скомпилировать установщик:
   ```powershell
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CarCare360Setup.iss
   ```

3. Результат: `installer\Output\CarCare360Setup.exe` (каталог `Output/` — в `.gitignore`).

## Что делает установщик
- Мастер на русском языке; путь по умолчанию `C:\Program Files\CarCare360`
  (можно изменить в мастере); установка под администратором (для всех пользователей).
- Создаёт ярлык в меню «Пуск» и (по галочке) на рабочем столе.
- Регистрируется в «Установка и удаление программ»; удаление снимает все
  установленные файлы и ярлыки.
- Учётные данные при установке не запрашиваются; структура БД не изменяется.

## Что НЕ затрагивается при удалении
- Данные пользователя в `%AppData%\CarCare360\` (`avatars.json`, `saved_login.json`)
  удаление **не трогает** — они вне папки установки.
- Строка подключения к БД (`CarCare360.Desktop.dll.config`) лежит в папке
  приложения и удаляется вместе с ним (это файл приложения, не данные пользователя).

## Проверка (на чистой машине)
Запустить `CarCare360Setup.exe`, пройти мастер, проверить ярлыки (Пуск + рабочий
стол) и запуск, затем удалить через «Программы и компоненты» и убедиться в
отсутствии артефактов.
