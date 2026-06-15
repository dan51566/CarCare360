import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../models/app_notification.dart';
import '../models/car.dart';
import '../models/car_model_ref.dart';
import '../models/order.dart';
import '../models/requests.dart';
import '../models/service.dart';
import '../models/user.dart';
import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../services/car_service.dart';
import '../services/catalog_service.dart';
import '../services/client_service.dart';
import '../services/avatar_service.dart';
import '../services/notification_service.dart';
import '../services/order_service.dart';
import '../services/token_storage.dart';

// ─────────────────────────────────────────────────────────────────────────────
//  Инфраструктура: хранилище, HTTP-клиент, сервисы
// ─────────────────────────────────────────────────────────────────────────────

final storageProvider = Provider<TokenStorage>((ref) => TokenStorage());

final apiClientProvider = Provider<ApiClient>((ref) {
  final client = ApiClient(ref.read(storageProvider));
  // При окончательной потере сессии переводим состояние авторизации в «не вошёл».
  client.onSessionExpired = () {
    ref.read(authProvider.notifier).handleSessionExpired();
  };
  return client;
});

final authServiceProvider =
    Provider<AuthService>((ref) => AuthService(ref.read(apiClientProvider)));
final carServiceProvider =
    Provider<CarService>((ref) => CarService(ref.read(apiClientProvider)));
final orderServiceProvider =
    Provider<OrderService>((ref) => OrderService(ref.read(apiClientProvider)));
final catalogServiceProvider = Provider<CatalogService>(
    (ref) => CatalogService(ref.read(apiClientProvider)));
final clientServiceProvider =
    Provider<ClientService>((ref) => ClientService(ref.read(apiClientProvider)));

// ─────────────────────────────────────────────────────────────────────────────
//  Состояние авторизации
// ─────────────────────────────────────────────────────────────────────────────

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  final AuthStatus status;
  final User? user;

  const AuthState({required this.status, this.user});

  bool get isAuthenticated => status == AuthStatus.authenticated;
}

class AuthNotifier extends Notifier<AuthState> {
  AuthService get _service => ref.read(authServiceProvider);

  @override
  AuthState build() {
    // Запускаем авто-вход и возвращаем «неизвестно» (показываем сплэш).
    _restore();
    return const AuthState(status: AuthStatus.unknown);
  }

  Future<void> _restore() async {
    final user = await _service.tryAutoLogin();
    state = user != null
        ? AuthState(status: AuthStatus.authenticated, user: user)
        : const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Вход. Бросает ApiException при ошибке (обрабатывается в UI).
  Future<void> login(String login, String password) async {
    final user = await _service.login(login, password);
    state = AuthState(status: AuthStatus.authenticated, user: user);
  }

  /// Регистрация с автоматическим входом.
  Future<void> register(RegisterRequest request) async {
    final user = await _service.register(request);
    state = AuthState(status: AuthStatus.authenticated, user: user);
  }

  Future<void> logout() async {
    await _service.logout();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Сессия истекла (refresh не удался) — вызывается из перехватчика.
  void handleSessionExpired() {
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Обновить данные пользователя в состоянии после редактирования профиля.
  void updateUser(User user) {
    state = AuthState(status: AuthStatus.authenticated, user: user);
  }
}

final authProvider =
    NotifierProvider<AuthNotifier, AuthState>(AuthNotifier.new);

/// Текущий пользователь (или null).
final currentUserProvider = Provider<User?>((ref) {
  return ref.watch(authProvider).user;
});

// ─────────────────────────────────────────────────────────────────────────────
//  Автомобили
// ─────────────────────────────────────────────────────────────────────────────

class CarsNotifier extends AsyncNotifier<List<Car>> {
  CarService get _service => ref.read(carServiceProvider);

  @override
  Future<List<Car>> build() => _service.getCars();

  Future<void> reload() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(_service.getCars);
  }

  Future<Car> add(CarRequest request) async {
    final car = await _service.createCar(request);
    await reload();
    return car;
  }

  Future<Car> edit(int carId, CarRequest request) async {
    final car = await _service.updateCar(carId, request);
    await reload();
    return car;
  }

  Future<void> remove(int carId) async {
    await _service.deleteCar(carId);
    await reload();
  }
}

final carsProvider =
    AsyncNotifierProvider<CarsNotifier, List<Car>>(CarsNotifier.new);

/// Справочник моделей (для формы добавления авто).
final carModelsProvider = FutureProvider<List<CarModelRef>>((ref) {
  return ref.read(carServiceProvider).getCarModels();
});

// ─────────────────────────────────────────────────────────────────────────────
//  Заказы
// ─────────────────────────────────────────────────────────────────────────────

class OrdersNotifier extends AsyncNotifier<List<Order>> {
  OrderService get _service => ref.read(orderServiceProvider);

  @override
  Future<List<Order>> build() async {
    final orders = await _service.getOrders();
    orders.sort(_byDateDesc);
    return orders;
  }

  static int _byDateDesc(Order a, Order b) {
    final da = a.createdAt ?? a.scheduledDate ?? DateTime(1900);
    final db = b.createdAt ?? b.scheduledDate ?? DateTime(1900);
    return db.compareTo(da);
  }

  Future<void> reload() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() async {
      final orders = await _service.getOrders();
      orders.sort(_byDateDesc);
      return orders;
    });
  }

  Future<Order> create(OrderCreateRequest request) async {
    final order = await _service.createOrder(request);
    await reload();
    return order;
  }

  Future<void> cancel(int orderId) async {
    await _service.cancelOrder(orderId);
    await reload();
  }
}

final ordersProvider =
    AsyncNotifierProvider<OrdersNotifier, List<Order>>(OrdersNotifier.new);

/// Детали заказа по id.
final orderDetailProvider = FutureProvider.family<Order, int>((ref, id) {
  return ref.read(orderServiceProvider).getOrder(id);
});

// ─────────────────────────────────────────────────────────────────────────────
//  Услуги
// ─────────────────────────────────────────────────────────────────────────────

final servicesProvider = FutureProvider<List<Service>>((ref) {
  return ref.read(catalogServiceProvider).getServices();
});

// ─────────────────────────────────────────────────────────────────────────────
//  Уведомления
// ─────────────────────────────────────────────────────────────────────────────

class NotificationsNotifier extends Notifier<List<AppNotification>> {
  @override
  List<AppNotification> build() {
    final sub = NotificationService.instance.stream.listen((n) {
      state = [n, ...state];
    });
    ref.onDispose(sub.cancel);
    // Стартовое состояние — уже накопленная за сессию история.
    return List<AppNotification>.from(NotificationService.instance.history);
  }
}

final notificationsProvider =
    NotifierProvider<NotificationsNotifier, List<AppNotification>>(
        NotificationsNotifier.new);

// ─────────────────────────────────────────────────────────────────────────────
//  Аватарка профиля (локальный файл)
// ─────────────────────────────────────────────────────────────────────────────

/// Нотификатор для управления локальным файлом аватарки.
/// Состояние — [File?]: null означает, что аватарка не установлена.
class AvatarNotifier extends Notifier<File?> {
  late final AvatarService _service;

  @override
  File? build() {
    _service = AvatarService();
    // Загружаем аватарку асинхронно при инициализации провайдера.
    _loadInitial();
    return null;
  }

  Future<void> _loadInitial() async {
    final file = await _service.getAvatarFile();
    if (file != null) state = file;
  }

  /// Открывает пикер ([source] = галерея или камера) и сохраняет аватарку.
  Future<void> pick(ImageSource source) async {
    final file = await _service.pickAndSave(source);
    if (file != null) state = file;
  }

  /// Удаляет аватарку (сброс до иконки-заглушки).
  Future<void> remove() async {
    await _service.deleteAvatar();
    state = null;
  }
}

/// Провайдер текущего файла аватарки. null — аватарка не установлена.
final avatarFileProvider =
    NotifierProvider<AvatarNotifier, File?>(AvatarNotifier.new);
