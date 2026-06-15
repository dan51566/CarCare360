import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/app_theme.dart';
import '../helpers/ui_feedback.dart';
import '../models/car.dart';
import '../providers/providers.dart';
import '../services/api_exception.dart';
import '../widgets/car_card.dart';

/// Список автомобилей клиента.
class CarsListScreen extends ConsumerWidget {
  const CarsListScreen({super.key});

  Future<void> _confirmDelete(
      BuildContext context, WidgetRef ref, Car car) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Удалить автомобиль?'),
        content: Text('${car.title} (${car.licensePlate})'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Отмена')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Удалить', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(carsProvider.notifier).remove(car.carId);
      if (context.mounted) AppSnack.success(context, 'Автомобиль удалён');
    } on ApiException catch (e) {
      if (context.mounted) AppSnack.error(context, e.message);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final carsAsync = ref.watch(carsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Мои автомобили')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/add-car'),
        icon: const Icon(Icons.add),
        label: const Text('Добавить'),
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.read(carsProvider.notifier).reload(),
        child: carsAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => _error(context, ref, '$e'),
          data: (cars) {
            if (cars.isEmpty) {
              return _empty(context);
            }
            return ListView(
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: cars
                  .map((c) => CarCard(
                        car: c,
                        onEdit: () => context.push('/edit-car', extra: c),
                        onDelete: () => _confirmDelete(context, ref, c),
                      ))
                  .toList(),
            );
          },
        ),
      ),
    );
  }

  Widget _empty(BuildContext context) {
    return ListView(
      children: [
        const SizedBox(height: 120),
        const Icon(Icons.directions_car_outlined,
            size: 72, color: AppColors.textSecondary),
        const SizedBox(height: 16),
        const Center(
          child: Text('У вас пока нет автомобилей',
              style: TextStyle(color: AppColors.textSecondary, fontSize: 16)),
        ),
      ],
    );
  }

  Widget _error(BuildContext context, WidgetRef ref, String msg) {
    return ListView(
      children: [
        const SizedBox(height: 120),
        const Icon(Icons.error_outline, size: 56, color: Colors.red),
        const SizedBox(height: 12),
        Center(
            child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Text(msg, textAlign: TextAlign.center),
        )),
      ],
    );
  }
}
