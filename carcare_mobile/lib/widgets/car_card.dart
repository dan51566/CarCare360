import 'package:flutter/material.dart';

import '../helpers/app_theme.dart';
import '../models/car.dart';

/// Карточка автомобиля клиента.
class CarCard extends StatelessWidget {
  final Car car;
  final VoidCallback? onTap;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  const CarCard({
    super.key,
    required this.car,
    this.onTap,
    this.onEdit,
    this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: onTap,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        leading: const CircleAvatar(
          backgroundColor: AppColors.primary,
          child: Icon(Icons.directions_car, color: Colors.white),
        ),
        title: Text(car.title,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 2),
            Text('Госномер: ${car.licensePlate}'),
            if (car.year != null) Text('Год: ${car.year}'),
            if (car.vin != null && car.vin!.isNotEmpty)
              Text('VIN: ${car.vin}', overflow: TextOverflow.ellipsis),
          ],
        ),
        trailing: PopupMenuButton<String>(
          onSelected: (v) {
            if (v == 'edit') onEdit?.call();
            if (v == 'delete') onDelete?.call();
          },
          itemBuilder: (_) => const [
            PopupMenuItem(value: 'edit', child: Text('Редактировать')),
            PopupMenuItem(value: 'delete', child: Text('Удалить')),
          ],
        ),
      ),
    );
  }
}
