import 'package:flutter/material.dart';

import '../helpers/app_theme.dart';
import '../helpers/formatters.dart';
import '../helpers/order_status.dart';
import '../models/order.dart';
import 'status_badge.dart';

/// Карточка заказа с левой цветной полосой статуса.
class OrderCard extends StatelessWidget {
  final Order order;
  final VoidCallback? onTap;

  const OrderCard({super.key, required this.order, this.onTap});

  @override
  Widget build(BuildContext context) {
    final statusColor = OrderStatuses.color(order.status);
    final servicesText = order.services.isEmpty
        ? 'Услуги не выбраны'
        : order.services
            .map((s) => s.serviceName ?? 'Услуга #${s.serviceId}')
            .join(', ');

    return Card(
      child: InkWell(
        onTap: onTap,
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(width: 6, color: statusColor),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Text(
                            'Заказ №${order.orderId}',
                            style: const TextStyle(
                                fontWeight: FontWeight.bold, fontSize: 16),
                          ),
                          const Spacer(),
                          StatusBadge(order.status),
                        ],
                      ),
                      const SizedBox(height: 8),
                      _row(Icons.directions_car,
                          order.carInfo ?? 'Авто #${order.carId}'),
                      const SizedBox(height: 4),
                      _row(Icons.event,
                          Fmt.dateAndTime(order.scheduledDate, order.scheduledTime)),
                      const SizedBox(height: 4),
                      _row(Icons.build_outlined, servicesText, maxLines: 2),
                      if (order.partsTotal > 0) ...[
                        const SizedBox(height: 4),
                        _row(Icons.payments_outlined,
                            'Запчасти: ${Fmt.money(order.partsTotal)}'),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _row(IconData icon, String text, {int maxLines = 1}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 16, color: AppColors.textSecondary),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            maxLines: maxLines,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.textPrimary, fontSize: 13),
          ),
        ),
      ],
    );
  }
}
