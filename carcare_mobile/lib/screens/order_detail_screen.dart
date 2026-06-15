import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/app_theme.dart';
import '../helpers/formatters.dart';
import '../helpers/order_status.dart';
import '../helpers/ui_feedback.dart';
import '../models/order.dart';
import '../providers/providers.dart';
import '../services/api_exception.dart';
import '../widgets/status_badge.dart';

/// Детали заказа: статус, услуги, запчасти, отмена записи.
class OrderDetailScreen extends ConsumerWidget {
  final int orderId;
  const OrderDetailScreen({super.key, required this.orderId});

  Future<void> _cancel(BuildContext context, WidgetRef ref) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Отменить запись?'),
        content: const Text('Запись будет отменена. Продолжить?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Нет')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Отменить запись',
                style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(ordersProvider.notifier).cancel(orderId);
      if (context.mounted) {
        AppSnack.success(context, 'Запись отменена');
        context.pop();
      }
    } on ApiException catch (e) {
      // Если сервер запретил (например, статус уже «В работе») — сообщаем.
      if (context.mounted) {
        AppSnack.error(context, 'Не удалось отменить: ${e.message}');
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orderAsync = ref.watch(orderDetailProvider(orderId));

    return Scaffold(
      appBar: AppBar(title: Text('Заказ №$orderId')),
      body: orderAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text('$e', textAlign: TextAlign.center),
          ),
        ),
        data: (order) => RefreshIndicator(
          onRefresh: () async => ref.invalidate(orderDetailProvider(orderId)),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _statusHeader(order),
              const SizedBox(height: 16),
              _infoCard(order),
              const SizedBox(height: 16),
              _servicesCard(order),
              if (order.parts.isNotEmpty) ...[
                const SizedBox(height: 16),
                _partsCard(order),
              ],
              if (OrderStatuses.isCancellable(order.status)) ...[
                const SizedBox(height: 24),
                OutlinedButton.icon(
                  onPressed: () => _cancel(context, ref),
                  icon: const Icon(Icons.cancel_outlined, color: Colors.red),
                  label: const Text('Отменить запись',
                      style: TextStyle(color: Colors.red)),
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                    side: const BorderSide(color: Colors.red),
                  ),
                ),
              ],
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _statusHeader(Order order) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        const Text('Статус',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        StatusBadge(order.status),
      ],
    );
  }

  Widget _infoCard(Order order) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            _kv(Icons.directions_car, 'Автомобиль',
                order.carInfo ?? 'Авто #${order.carId}'),
            _kv(Icons.event, 'Запись',
                Fmt.dateAndTime(order.scheduledDate, order.scheduledTime)),
            _kv(Icons.schedule, 'Создан', Fmt.dateTime(order.createdAt)),
            if (order.mileage != null)
              _kv(Icons.speed, 'Пробег', '${order.mileage} км'),
            if (order.notes != null && order.notes!.isNotEmpty)
              _kv(Icons.notes, 'Комментарий', order.notes!),
            if (order.partsTotal > 0)
              _kv(Icons.payments_outlined, 'Запчасти',
                  Fmt.money(order.partsTotal)),
          ],
        ),
      ),
    );
  }

  Widget _servicesCard(Order order) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Услуги',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const Divider(),
            if (order.services.isEmpty)
              const Text('Услуги ещё не добавлены',
                  style: TextStyle(color: AppColors.textSecondary))
            else
              ...order.services.map(
                (s) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  dense: true,
                  leading: const Icon(Icons.build_outlined,
                      color: AppColors.primary),
                  title: Text(s.serviceName ?? 'Услуга #${s.serviceId}'),
                  subtitle: s.mechanicName != null
                      ? Text('Механик: ${s.mechanicName}')
                      : null,
                  trailing: s.status != null ? StatusChipMini(s.status!) : null,
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _partsCard(Order order) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Запчасти',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const Divider(),
            ...order.parts.map(
              (p) => ListTile(
                contentPadding: EdgeInsets.zero,
                dense: true,
                leading: const Icon(Icons.settings_outlined,
                    color: AppColors.primary),
                title: Text(p.partName ?? 'Запчасть #${p.partId}'),
                subtitle: Text('${p.quantity} шт.'),
                trailing: Text(Fmt.money(p.lineTotal)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _kv(IconData icon, String key, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: AppColors.textSecondary),
          const SizedBox(width: 10),
          SizedBox(
            width: 100,
            child: Text(key,
                style: const TextStyle(color: AppColors.textSecondary)),
          ),
          Expanded(
              child: Text(value,
                  style: const TextStyle(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}

/// Мелкий бейдж статуса услуги.
class StatusChipMini extends StatelessWidget {
  final String status;
  const StatusChipMini(this.status, {super.key});

  @override
  Widget build(BuildContext context) {
    final color = OrderStatuses.color(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(status,
          style: TextStyle(
              color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}
