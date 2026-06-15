# CarCare 360 — Мобильное приложение

Кроссплатформенное мобильное приложение для клиентов автосервиса CarCare 360. Позволяет записываться на обслуживание, отслеживать статус заказов и управлять автомобилями. Работает с [CarCare360 API](../CarCare360.Api/README.md).

> **Связанные проекты:** [CarCare360.Desktop](../README.md) · [CarCare360.Api](../CarCare360.Api/README.md) · [Корневой README](../README.md)

---

## Содержание

1. [Общая информация](#1-общая-информация)
2. [Функциональные возможности](#2-функциональные-возможности)
3. [Архитектура и технологии](#3-архитектура-и-технологии)
4. [Структура проекта](#4-структура-проекта)
5. [Инструкция по настройке и запуску](#5-инструкция-по-настройке-и-запуску)
6. [Экраны и навигация](#6-экраны-и-навигация)
7. [Аватарка профиля](#7-аватарка-профиля)
8. [Сборка релизного APK](#8-сборка-релизного-apk)
9. [Примечания и известные особенности](#9-примечания-и-известные-особенности)
10. [Дополнительные сведения](#10-дополнительные-сведения)

---

## 1. Общая информация

| Параметр | Значение |
|----------|----------|
| **Фреймворк** | Flutter 3.44 / Dart 3.12 |
| **Платформы** | Android (API 23+), iOS (готово к сборке) |
| **Роль пользователя** | Только «Клиент» (механики и администраторы используют десктопное приложение) |
| **API** | `http://localhost:5009/api` (эмулятор: `http://10.0.2.2:5009/api`) |
| **Управление состоянием** | Riverpod 3 |
| **HTTP-клиент** | Dio 5 с JWT-интерцептором |
| **Навигация** | go_router 17 |

### Фирменные цвета

| Цвет | HEX | Применение |
|------|-----|------------|
| Тёмно-синий | `#1A237E` | AppBar, заголовки, иконки авторизации |
| Оранжевый | `#FF6B00` | Кнопки действий, FAB, активные вкладки |
| Белый | `#FFFFFF` | Фон экранов, текст на тёмном |
| Светло-серый | `#F5F5F5` | Поля ввода, карточки |

---

## 2. Функциональные возможности

### Авторизация

- Экран входа: поля логин/пароль, кнопка «Войти», переход к регистрации
- Экран регистрации: ФИО, логин, пароль, email (опционально), телефон (опционально)
- **Авто-вход** при запуске: токен сохраняется в `flutter_secure_storage`, при протухании access-токена автоматически вызывается `/auth/refresh`
- Выход: отзыв refresh-токена на сервере, очистка локального хранилища

### Автомобили

- Список авто клиента: марка, модель, госномер, год, VIN
- Добавление: выбор марки/модели из справочника `/api/car-models` (366+ моделей), ввод госномера, VIN, года, цвета, пробега
- Редактирование и удаление (с подтверждением)

### Запись на сервис

- Выбор автомобиля из своих
- Выбор даты (DatePicker) и времени (TimePicker)
- Мультивыбор услуг из каталога с ориентировочной стоимостью
- Поле для комментария (до 500 символов)
- Подтверждение через SnackBar + переход на главный экран

### История заказов

- Список всех заказов, новые сверху
- Карточка заказа: цветная полоса статуса, авто, дата, услуги, стоимость запчастей
- Детали заказа: полная информация, услуги (с механиком, если назначен), запчасти
- **Отмена записи:** доступна при статусах «Новый» и «Назначен» через `DELETE /orders/{id}` (мягкое удаление)

### Цвета статусов заказа

| Статус | Цвет |
|--------|------|
| Новый | `#2196F3` (синий) |
| Назначен | `#FF9800` (оранжевый) |
| В работе | `#FFC107` (жёлтый) |
| Ожидает запчасти | `#9C27B0` (фиолетовый) |
| Готов | `#4CAF50` (зелёный) |
| Выдан | `#607D8B` (серый) |
| Отменён | `#F44336` (красный) |

### Профиль

- Просмотр: ФИО, логин, роль, email, телефон
- Редактирование: ФИО, email, телефон → `PUT /api/clients/{userId}`
- Аватарка: выбор из галереи или камеры, хранится локально (см. [раздел 7](#7-аватарка-профиля))
- Смена пароля: заглушка (API не поддерживает)
- Выход из аккаунта

### Уведомления

- Интеграция с **Firebase Cloud Messaging** (FCM): при наличии `google-services.json` получает push-уведомления с сервера
- При отсутствии Firebase-конфигурации — работает в режиме **локальных уведомлений** (остальное функционирует)
- Экран истории уведомлений с возможностью отправки тестового уведомления
- Иконка уведомлений в AppBar главного экрана

---

## 3. Архитектура и технологии

### Управление состоянием — Riverpod 3

Используются современные `Notifier` / `AsyncNotifier`:

- `authProvider` — авторизация, данные текущего пользователя
- `carsProvider` — список авто с методами `add()`, `edit()`, `remove()`
- `ordersProvider` — список заказов, сортировка по дате (новые сверху)
- `servicesProvider` — каталог услуг (FutureProvider)
- `carModelsProvider` — справочник марок/моделей (FutureProvider)
- `notificationsProvider` — история уведомлений + stream
- `avatarFileProvider` — локальный файл аватарки (StateNotifier)

### HTTP-клиент — Dio 5

`lib/services/api_client.dart` содержит singleton-экземпляр `Dio` с двумя перехватчиками:

1. **Request interceptor** — добавляет `Authorization: Bearer <token>` в каждый запрос
2. **Error interceptor (401)** — при получении 401 на не-auth эндпоинте автоматически обновляет токен и повторяет запрос; при неудаче разлогинивает

### Навигация — go_router 17

`lib/router.dart` — `GoRouter` с `StatefulShellRoute.indexedStack`. Redirect-логика:
- `AuthStatus.unknown` → `/splash`
- Неавторизован → `/login`
- Авторизован на `/login`/`/splash` → `/home`

### JSON-сериализация

Все модели используют `json_serializable` с явными `@JsonKey(name: 'camelCaseWithAcronyms')`. API возвращает camelCase с сохранением акронимов: `userID`, `carID`, `orderID`.

### Зависимости (`pubspec.yaml`)

| Пакет | Версия | Назначение |
|-------|--------|------------|
| `flutter_riverpod` | ^3.3.1 | Управление состоянием |
| `dio` | ^5.9.2 | HTTP-клиент |
| `go_router` | ^17.2.3 | Навигация |
| `flutter_secure_storage` | ^10.3.1 | Хранилище токенов |
| `json_annotation` | ^4.12.0 | JSON-сериализация |
| `intl` | ^0.20.2 | Локализация, форматирование дат/чисел |
| `firebase_core` | ^4.9.0 | Firebase инициализация |
| `firebase_messaging` | ^16.2.2 | Push-уведомления (FCM) |
| `flutter_local_notifications` | ^21.0.0 | Локальные уведомления |
| `image_picker` | ^1.1.2 | Выбор фото из галереи/камеры |
| `path_provider` | ^2.1.5 | Доступ к директориям файловой системы |
| `flutter_localizations` | SDK | Русский язык в DatePicker/TimePicker |

---

## 4. Структура проекта

```
carcare_mobile/
├── android/
│   └── app/
│       ├── build.gradle.kts          # minSdk=23, desugaring, multiDex
│       ├── src/main/AndroidManifest.xml
│       └── google-services.json      # Firebase (создаётся flutterfire configure)
├── lib/
│   ├── main.dart                     # Точка входа, инициализация
│   ├── router.dart                   # GoRouter: маршруты + redirect
│   ├── helpers/
│   │   ├── app_theme.dart            # ThemeData, AppColors
│   │   ├── constants.dart            # ApiConfig.baseUrl
│   │   ├── formatters.dart           # Fmt.date(), Fmt.money()
│   │   ├── order_status.dart         # OrderStatuses, UserRoles
│   │   └── ui_feedback.dart          # AppSnack (SnackBar)
│   ├── models/                       # json_serializable модели
│   │   ├── user.dart, auth_response.dart
│   │   ├── car.dart, car_model_ref.dart
│   │   ├── order.dart, order_service_item.dart, order_part_item.dart
│   │   ├── service.dart, app_notification.dart
│   │   └── requests.dart             # DTO запросов
│   ├── services/
│   │   ├── api_client.dart           # Dio + JWT interceptors
│   │   ├── token_storage.dart        # flutter_secure_storage
│   │   ├── auth_service.dart         # login/register/logout/autoLogin
│   │   ├── car_service.dart          # CRUD + getCarModels()
│   │   ├── order_service.dart        # list/create/cancel (DELETE)
│   │   ├── catalog_service.dart      # GET /services
│   │   ├── client_service.dart       # PUT /clients/{id}
│   │   ├── notification_service.dart # FCM + local notifications
│   │   └── avatar_service.dart       # Локальная аватарка
│   ├── providers/
│   │   └── providers.dart            # Все Riverpod-провайдеры
│   ├── screens/                      # 12 экранов
│   └── widgets/                      # StatusBadge, OrderCard, CarCard, MainShell
├── test/
│   └── widget_test.dart              # Тесты OrderStatuses
└── pubspec.yaml
```

---

## 5. Инструкция по настройке и запуску

### Предварительные требования

- **Flutter SDK** 3.29+: `git clone https://github.com/flutter/flutter.git -b stable C:\src\flutter`, добавить `C:\src\flutter\bin` в PATH
- **Android Studio** с Android SDK (API 36), эмулятором `Medium_Phone_API_36.1`
- **JDK 17+**: встроенный в Android Studio (`C:\Program Files\Android\Android Studio\jbr`)
- Запущенный [CarCare360.Api](../CarCare360.Api/README.md) на `localhost:5009`

Проверить:
```bash
flutter doctor -v
```

### Установка зависимостей и генерация кода

```bash
cd "путь к проекту"
flutter pub get
dart run build_runner build --delete-conflicting-outputs
```

> При кириллическом пути использовать ASCII junction (см. [раздел 9](#9-примечания-и-известные-особенности)).

### Настройка API URL

Файл `lib/helpers/constants.dart` — по умолчанию:
- Android-эмулятор: `http://10.0.2.2:5009/api` (хост-машина изнутри эмулятора)
- Реальное устройство: заменить на IP своей машины в сети

### Настройка Firebase (опционально)

```bash
npm install -g firebase-tools
dart pub global activate flutterfire_cli
flutterfire configure   # создаст google-services.json и firebase_options.dart
```

### Запуск

```bash
flutter emulators --launch Medium_Phone_API_36.1
flutter run -d emulator-5554
```

---

## 6. Экраны и навигация

### Схема маршрутов

```
/splash ──→ /login ──→ /register
               │
               ↓ (после входа)
      ┌────────────────────────────┐
      │  [🏠 Главная]  [🚗 Авто]  │
      │  [📋 Заказы]  [👤 Профиль]│
      └────────────────────────────┘
         │          │       │      │
    /new-order  /add-car  /order/:id  /edit-profile
    /edit-car             /notifications
```

### Описание экранов

| Маршрут | Экран | Основные возможности |
|---------|-------|----------------------|
| `/splash` | SplashScreen | Авто-вход, перенаправление |
| `/login` | LoginScreen | Форма входа |
| `/register` | RegisterScreen | Регистрация клиента |
| `/home` | HomeScreen | Приветствие, активные заказы, «Записаться» |
| `/cars` | CarsListScreen | Список авто, добавить/редактировать/удалить |
| `/orders` | OrdersListScreen | История заказов |
| `/profile` | ProfileScreen | Аватарка, данные, выход |
| `/new-order` | NewOrderScreen | Выбор авто/даты/услуг, создание заказа |
| `/order/:id` | OrderDetailScreen | Детали, статус, отмена записи |
| `/edit-profile` | EditProfileScreen | Изменить ФИО/email/телефон |
| `/notifications` | NotificationsScreen | История push/локальных уведомлений |

---

## 7. Аватарка профиля

Хранится **локально** на устройстве. Файл: `<app_documents>/profile_avatar.jpg`.

**Использование:**
1. Откройте экран «Профиль»
2. Нажмите на аватар (появится оранжевая иконка камеры)
3. Выберите: «Из галереи» или «Сделать фото»
4. Изображение сохранится и будет доступно после перезапуска

**Технические детали:**
- `AvatarService` (lib/services/avatar_service.dart) — файловые операции
- `avatarFileProvider` — `StateNotifier<File?>` в Riverpod
- Разрешения Android: `READ_MEDIA_IMAGES`, `CAMERA`

---

## 8. Сборка релизного APK

```bash
# Debug (для тестирования)
flutter build apk --debug
# → build/app/outputs/flutter-apk/app-debug.apk (~155 МБ)

# Release
flutter build apk --release
# → build/app/outputs/flutter-apk/app-release.apk (~60-80 МБ)

# App Bundle для Google Play
flutter build appbundle
# → build/app/outputs/bundle/release/app-release.aab

# Установить через adb
adb install -r build/app/outputs/flutter-apk/app-debug.apk
```

Для реального устройства не забудьте изменить `ApiConfig.baseUrl` на IP сервера в локальной сети.

---

## 9. Примечания и известные особенности

### Кириллица в пути проекта

Если проект в папке с кириллицей — два обходных пути уже настроены в репозитории:

1. **build_runner** — запускать через ASCII junction:
   ```cmd
   mklink /J C:\cc360 "e:\Прокет ПП\Projects\CarCare360\carcare_mobile"
   cd C:\cc360
   dart run build_runner build
   ```

2. **Gradle/Kotlin** — в `android/gradle.properties`:
   ```properties
   android.overridePathCheck=true
   kotlin.incremental=false
   ```

### camelCase в API-ответах

API возвращает camelCase с акронимами (`userID`, не `userId`). После изменения `@JsonKey`:
```bash
dart run build_runner build --delete-conflicting-outputs
```

### Авто-обновление токена

При HTTP 401 интерцептор автоматически вызывает `POST /api/auth/refresh`. Экраны 401 не обрабатывают — это прозрачно.

### Отмена заказа = DELETE

`PUT /orders/{id}/status` возвращает 403 для клиентов. Отмена = `DELETE /orders/{id}` (мягкое удаление). Кнопка видна только при статусах «Новый» и «Назначен».

### HTTP в разработке

`android:usesCleartextTraffic="true"` разрешает HTTP для локального API. В production использовать HTTPS и убрать этот атрибут.

### FCM без конфигурации

Без `google-services.json` — приложение запускается, push-уведомления недоступны, локальные работают.

---

## 10. Дополнительные сведения

### Связанные проекты

| Проект | Описание |
|--------|----------|
| [CarCare360.Desktop](../README.md) | WPF для администратора и механика |
| [CarCare360.Api](../CarCare360.Api/README.md) | ASP.NET Core REST API |

### Статус разработки

- ✅ Реализовано: авторизация, авто, заказы, профиль, уведомления, аватарка
- 🔲 Запланировано: смена пароля, FCM device-token на сервере, iOS-конфигурация

### Образовательный контекст

Проект создан в учебных целях как демонстрация Flutter + ASP.NET Core API с JWT, Riverpod и FCM.
