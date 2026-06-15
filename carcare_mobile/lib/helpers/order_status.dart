import 'package:flutter/material.dart';

/// Канонические статусы заказа-наряда (точные строки из API: Helpers/OrderStatuses.cs).
class OrderStatuses {
  OrderStatuses._();

  static const String newOrder = 'Новый';
  static const String assigned = 'Назначен';
  static const String inProgress = 'В работе';
  static const String waitingParts = 'Ожидает запчасти';
  static const String ready = 'Готов';
  static const String issued = 'Выдан';
  static const String cancelled = 'Отменён';

  /// Статусы, при которых клиент ещё может отменить запись (через DELETE /orders/{id}).
  static const Set<String> cancellable = {newOrder, assigned};

  /// Активные (незавершённые) статусы — для подсветки на главном экране.
  static const Set<String> active = {
    newOrder,
    assigned,
    inProgress,
    waitingParts,
    ready,
  };

  /// Цвет статуса (как в десктоп-приложении).
  static Color color(String status) {
    switch (status) {
      case newOrder:
        return const Color(0xFF2196F3);
      case assigned:
        return const Color(0xFFFF9800);
      case inProgress:
        return const Color(0xFFFFC107);
      case waitingParts:
        return const Color(0xFF9C27B0);
      case ready:
        return const Color(0xFF4CAF50);
      case issued:
        return const Color(0xFF607D8B);
      case cancelled:
        return const Color(0xFFF44336);
      default:
        return const Color(0xFF9E9E9E);
    }
  }

  static bool isCancellable(String status) => cancellable.contains(status);
  static bool isActive(String status) => active.contains(status);
}

/// Роли пользователей (точные строки из API: Helpers/Roles.cs).
class UserRoles {
  UserRoles._();

  static const String admin = 'Администратор';
  static const String mechanic = 'Механик';
  static const String client = 'Клиент';
}
