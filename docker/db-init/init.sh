#!/usr/bin/env bash
# ============================================================================
#  CarCare360 — инициализация базы данных в контейнере SQL Server
#  ----------------------------------------------------------------------------
#  Выполняется ОДИН РАЗ сервисом db-init из docker-compose.yml:
#    1) ждёт готовности SQL Server;
#    2) создаёт базу данных CarCare360, если её ещё нет;
#    3) применяет СУЩЕСТВУЮЩИЕ скрипты схемы 01 -> 02 -> 03 без их изменения.
#
#  Скрипты 01_Create_Database.sql и 02_Create_Triggers.sql НЕ идемпотентны
#  (CREATE TABLE/TRIGGER без проверок существования) и НЕ транзакционны
#  (содержат разделители GO). Поэтому состояние схемы определяется по «маркерам
#  конца» каждого скрипта, а не по первому созданному объекту:
#    - dbo.AddPartToOrder  — последняя процедура в 01 (признак «01 применён до конца»);
#    - dbo.trg_Audit_Users — последний триггер в 02   (признак «02 применён до конца»);
#    - dbo.Roles           — первый объект 01          (признак «инициализация началась»).
#
#  Состояния:
#    READY   — оба маркера на месте → схема полна, 01/02 пропускаем (идемпотентность).
#    EMPTY   — нет даже Roles → чистая БД, применяем 01 и 02.
#    PARTIAL — есть Roles, но нет одного из маркеров конца → прерванная инициализация.
#              Повторный прогон 01/02 упал бы на «объект уже существует», а молчаливый
#              пропуск оставил бы схему битой — поэтому ОСТАНАВЛИВАЕМСЯ с понятной
#              инструкцией (это и есть защита от сценария «сбой на середине 02»).
#
#  Скрипт 03_Create_RefreshTokens.sql идемпотентен сам по себе (IF OBJECT_ID IS NULL)
#  и применяется всегда, кроме состояния PARTIAL.
# ============================================================================
set -uo pipefail

SQLCMD="/opt/mssql-tools/bin/sqlcmd"
SERVER="db,1433"
DBUSER="sa"
DBPASS="${MSSQL_SA_PASSWORD:?Переменная MSSQL_SA_PASSWORD не задана}"

# Обёртка над sqlcmd с общими параметрами подключения.
sql() {
  "$SQLCMD" -S "$SERVER" -U "$DBUSER" -P "$DBPASS" "$@"
}

echo "[db-init] Ожидание готовности SQL Server..."
for i in $(seq 1 60); do
  if sql -Q "SELECT 1" -b >/dev/null 2>&1; then
    echo "[db-init] SQL Server готов."
    break
  fi
  if [ "$i" -eq 60 ]; then
    echo "[db-init] ОШИБКА: SQL Server не ответил за отведённое время." >&2
    exit 1
  fi
  sleep 2
done

echo "[db-init] Создание базы данных CarCare360 (если отсутствует)..."
sql -b -Q "IF DB_ID('CarCare360') IS NULL CREATE DATABASE CarCare360;" \
  || { echo "[db-init] ОШИБКА создания базы данных." >&2; exit 1; }

# Определяем состояние схемы (READY / EMPTY / PARTIAL) одним запросом.
STATE_SQL="SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID('dbo.AddPartToOrder') IS NOT NULL AND OBJECT_ID('dbo.trg_Audit_Users') IS NOT NULL THEN 'READY' WHEN OBJECT_ID('dbo.Roles') IS NULL THEN 'EMPTY' ELSE 'PARTIAL' END"
STATE="$(sql -d CarCare360 -h -1 -W -Q "$STATE_SQL" 2>/dev/null | tr -d '[:space:]')"

echo "[db-init] Состояние схемы: ${STATE:-<не определено>}"

case "$STATE" in
  READY)
    echo "[db-init] Схема уже создана полностью — пропуск скриптов 01 и 02."
    ;;
  EMPTY)
    echo "[db-init] Применение 01_Create_Database.sql..."
    sql -d CarCare360 -b -i /sql/01_Create_Database.sql \
      || { echo "[db-init] ОШИБКА в 01_Create_Database.sql." >&2; exit 1; }
    echo "[db-init] Применение 02_Create_Triggers.sql..."
    sql -d CarCare360 -b -i /sql/02_Create_Triggers.sql \
      || { echo "[db-init] ОШИБКА в 02_Create_Triggers.sql." >&2; exit 1; }
    ;;
  PARTIAL)
    {
      echo "[db-init] ОШИБКА: база в ЧАСТИЧНО инициализированном состоянии"
      echo "          (предыдущий прогон прервался на середине скрипта схемы)."
      echo "          Скрипты 01/02 не идемпотентны — безопасный автоповтор невозможен."
      echo "          Варианты восстановления:"
      echo "            • восстановить базу данных из резервной копии; ЛИБО"
      echo "            • выполнить полный сброс тома данных (ВНИМАНИЕ: данные удалятся):"
      echo "                docker compose down -v"
      echo "                docker compose up -d --build"
    } >&2
    exit 1
    ;;
  *)
    echo "[db-init] ОШИБКА: не удалось определить состояние схемы." >&2
    exit 1
    ;;
esac

echo "[db-init] Применение 03_Create_RefreshTokens.sql..."
sql -d CarCare360 -b -i /sql/03_Create_RefreshTokens.sql \
  || { echo "[db-init] ОШИБКА в 03_Create_RefreshTokens.sql." >&2; exit 1; }

echo "[db-init] Инициализация базы данных завершена успешно."
