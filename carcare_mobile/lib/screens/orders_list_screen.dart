import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/app_theme.dart';
import '../providers/providers.dart';
import '../widgets/order_card.dart';

/// История заказов клиента (новые сверху).
class OrdersListScreen extends ConsumerWidget {
  const OrdersListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ordersAsync = ref.watch(ordersProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Мои заказы')),
      body: RefreshIndicator(
        onRefresh: () => ref.read(ordersProvider.notifier).reload(),
        child: ordersAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => ListView(children: [
            const SizedBox(height: 120),
            const Icon(Icons.error_outline, size: 56, color: Colors.red),
            const SizedBox(height: 12),
            Center(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Text('$e', textAlign: TextAlign.center),
              ),
            ),
          ]),
          data: (orders) {
            if (orders.isEmpty) {
              return ListView(children: const [
                SizedBox(height: 120),
                Icon(Icons.receipt_long_outlined,
                    size: 72, color: AppColors.textSecondary),
                SizedBox(height: 16),
                Center(
                  child: Text('Заказов пока нет',
                      style: TextStyle(
                          color: AppColors.textSecondary, fontSize: 16)),
                ),
              ]);
            }
            return ListView(
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: orders
                  .map((o) => OrderCard(
                        order: o,
                        onTap: () => context.push('/order/${o.orderId}'),
                      ))
                  .toList(),
            );
          },
        ),
      ),
    );
  }
}
