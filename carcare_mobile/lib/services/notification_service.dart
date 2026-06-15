import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../models/app_notification.dart';

/// Фоновый обработчик push (должен быть top-level + @pragma).
/// Реальная доставка push идёт с сервера; здесь — только приём.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Системой Firebase уже инициализирован для фонового изолята.
  // Логируем для отладки; показ системного уведомления берёт на себя ОС.
  debugPrint('BG push: ${message.notification?.title}');
}

/// Сервис уведомлений: реальный FCM + локальные уведомления.
///
/// Архитектура рассчитана на серверную отправку push. Если Firebase ещё
/// не сконфигурирован (нет google-services.json / firebase_options.dart),
/// сервис продолжает работать в режиме локальных уведомлений — это позволяет
/// запускать и тестировать приложение до полной настройки FCM.
class NotificationService {
  NotificationService._();
  static final NotificationService instance = NotificationService._();

  final FlutterLocalNotificationsPlugin _local =
      FlutterLocalNotificationsPlugin();

  final StreamController<AppNotification> _controller =
      StreamController<AppNotification>.broadcast();

  /// История полученных уведомлений за сессию (для экрана списка).
  final List<AppNotification> history = [];

  /// Поток новых уведомлений.
  Stream<AppNotification> get stream => _controller.stream;

  /// FCM-токен устройства (когда доступен).
  String? fcmToken;

  bool get isFcmAvailable => fcmToken != null;

  static const AndroidNotificationChannel _channel = AndroidNotificationChannel(
    'carcare_default',
    'Уведомления CarCare360',
    description: 'Статусы заказов и сервисные сообщения',
    importance: Importance.high,
  );

  /// Инициализация — вызывается из main() после runApp-окружения.
  Future<void> init() async {
    await _initLocal();
    await _initFirebase();
  }

  Future<void> _initLocal() async {
    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosInit = DarwinInitializationSettings();
    await _local.initialize(
      settings:
          const InitializationSettings(android: androidInit, iOS: iosInit),
    );

    await _local
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(_channel);
  }

  Future<void> _initFirebase() async {
    try {
      // Без firebase_options.dart на Android конфигурация берётся
      // из google-services.json. Если его нет — будет исключение.
      await Firebase.initializeApp();

      final messaging = FirebaseMessaging.instance;
      await messaging.requestPermission();

      fcmToken = await messaging.getToken();
      // TODO: при появлении серверного эндпоинта отправлять токен:
      //   POST /api/notifications/register-device { token: fcmToken }
      debugPrint('FCM token: $fcmToken');

      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

      FirebaseMessaging.onMessage.listen((message) {
        final n = _fromRemote(message);
        _record(n);
        _showLocal(n);
      });

      FirebaseMessaging.onMessageOpenedApp.listen((message) {
        _record(_fromRemote(message));
      });
    } catch (e) {
      // Firebase не сконфигурирован — работаем без push (только локальные).
      debugPrint('FCM недоступен (нет конфигурации Firebase): $e');
    }
  }

  AppNotification _fromRemote(RemoteMessage message) {
    final n = message.notification;
    return AppNotification(
      title: n?.title ?? message.data['title']?.toString() ?? 'CarCare 360',
      body: n?.body ?? message.data['body']?.toString() ?? '',
    );
  }

  void _record(AppNotification n) {
    history.insert(0, n);
    _controller.add(n);
  }

  /// Показать локальное уведомление (используется и для тестовой имитации push).
  Future<void> showLocalTest() async {
    final n = AppNotification(
      title: 'Тестовое уведомление',
      body: 'Так будут выглядеть push-уведомления о статусе заказа.',
    );
    _record(n);
    await _showLocal(n);
  }

  Future<void> _showLocal(AppNotification n) async {
    final details = NotificationDetails(
      android: AndroidNotificationDetails(
        _channel.id,
        _channel.name,
        channelDescription: _channel.description,
        importance: Importance.high,
        priority: Priority.high,
      ),
      iOS: const DarwinNotificationDetails(),
    );
    await _local.show(
      id: DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title: n.title,
      body: n.body,
      notificationDetails: details,
    );
  }
}
