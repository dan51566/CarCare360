import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'models/car.dart';
import 'providers/providers.dart';
import 'screens/add_edit_car_screen.dart';
import 'screens/cars_list_screen.dart';
import 'screens/edit_profile_screen.dart';
import 'screens/home_screen.dart';
import 'screens/login_screen.dart';
import 'screens/mechanics_screen.dart';
import 'screens/new_order_screen.dart';
import 'screens/notifications_screen.dart';
import 'screens/order_detail_screen.dart';
import 'screens/orders_list_screen.dart';
import 'screens/profile_screen.dart';
import 'screens/register_screen.dart';
import 'screens/splash_screen.dart';
import 'widgets/main_shell.dart';

/// Плавный fade-переход для модальных экранов.
CustomTransitionPage<void> _fade(Widget child, GoRouterState state) {
  return CustomTransitionPage<void>(
    key: state.pageKey,
    child: child,
    transitionsBuilder: (_, animation, _, child) =>
        FadeTransition(opacity: animation, child: child),
  );
}

final _rootKey = GlobalKey<NavigatorState>();

/// Провайдер роутера: redirect зависит от состояния авторизации.
final routerProvider = Provider<GoRouter>((ref) {
  // Перестраиваем маршрутизацию при изменении статуса авторизации.
  final refresh = ValueNotifier<int>(0);
  ref.listen(authProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/splash',
    refreshListenable: refresh,
    redirect: (context, state) {
      final auth = ref.read(authProvider);
      final loc = state.matchedLocation;

      // Пока проверяется сессия — держим на сплэше.
      if (auth.status == AuthStatus.unknown) {
        return loc == '/splash' ? null : '/splash';
      }

      final loggedIn = auth.status == AuthStatus.authenticated;
      final onAuthPages =
          loc == '/login' || loc == '/register' || loc == '/splash';

      if (!loggedIn) {
        return onAuthPages && loc != '/splash' ? null : '/login';
      }
      // Авторизован, но на странице входа/сплэша → на главную.
      if (onAuthPages) return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, _) => const RegisterScreen()),

      // Нижняя навигация (каждая вкладка сохраняет своё состояние).
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => MainShell(navigationShell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/home', builder: (_, _) => const HomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/cars', builder: (_, _) => const CarsListScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
                path: '/orders', builder: (_, _) => const OrdersListScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
                path: '/profile', builder: (_, _) => const ProfileScreen()),
          ]),
        ],
      ),

      // Модальные экраны поверх каркаса.
      GoRoute(
        path: '/new-order',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(const NewOrderScreen(), s),
      ),
      GoRoute(
        path: '/mechanics',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(const MechanicsScreen(), s),
      ),
      GoRoute(
        path: '/add-car',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(const AddEditCarScreen(), s),
      ),
      GoRoute(
        path: '/edit-car',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) =>
            _fade(AddEditCarScreen(car: s.extra as Car?), s),
      ),
      GoRoute(
        path: '/order/:id',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(
          OrderDetailScreen(
              orderId: int.parse(s.pathParameters['id'] ?? '0')),
          s,
        ),
      ),
      GoRoute(
        path: '/edit-profile',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(const EditProfileScreen(), s),
      ),
      GoRoute(
        path: '/notifications',
        parentNavigatorKey: _rootKey,
        pageBuilder: (_, s) => _fade(const NotificationsScreen(), s),
      ),
    ],
  );
});
