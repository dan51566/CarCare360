import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/app_theme.dart';
import '../helpers/order_status.dart';
import '../providers/providers.dart';
import '../widgets/order_card.dart';

/// Главный экран: приветствие, ближайшие активные записи, быстрая запись.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider);
    final ordersAsync = ref.watch(ordersProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('CarCare 360'),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            tooltip: 'Уведомления',
            onPressed: () => context.push('/notifications'),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.read(ordersProvider.notifier).reload(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            _greeting(user?.fullName ?? 'Гость'),
            _quickBookButton(context),
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 8, 16, 8),
              child: Text(
                'Ближайшие записи',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
            ),
            ordersAsync.when(
              loading: () => const Padding(
                padding: EdgeInsets.all(32),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (e, _) => _message(Icons.error_outline, '$e'),
              data: (orders) {
                final active =
                    orders.where((o) => OrderStatuses.isActive(o.status)).toList();
                if (active.isEmpty) {
                  return _message(Icons.event_available,
                      'Нет активных записей.\nЗапишитесь на обслуживание.');
                }
                return Column(
                  children: active
                      .map((o) => OrderCard(
                            order: o,
                            onTap: () => context.push('/order/${o.orderId}'),
                          ))
                      .toList(),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _greeting(String name) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
      decoration: const BoxDecoration(
        color: AppColors.primary,
        borderRadius: BorderRadius.vertical(bottom: Radius.circular(24)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Здравствуйте,',
              style: TextStyle(color: Colors.white70, fontSize: 16)),
          const SizedBox(height: 4),
          Text(
            name,
            style: const TextStyle(
                color: Colors.white,
                fontSize: 24,
                fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _quickBookButton(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
      child: ElevatedButton.icon(
        onPressed: () => context.push('/new-order'),
        icon: const Icon(Icons.add_circle_outline),
        label: const Text('Записаться на сервис'),
      ),
    );
  }

  Widget _message(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        children: [
          Icon(icon, size: 56, color: AppColors.textSecondary),
          const SizedBox(height: 12),
          Text(text,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary)),
        ],
      ),
    );
  }
}
