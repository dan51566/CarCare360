import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/app_theme.dart';
import '../helpers/formatters.dart';
import '../helpers/ui_feedback.dart';
import '../models/car.dart';
import '../models/requests.dart';
import '../models/service.dart';
import '../providers/providers.dart';
import '../services/api_exception.dart';

/// Запись на сервис: выбор авто, даты/времени, услуг и комментария.
class NewOrderScreen extends ConsumerStatefulWidget {
  const NewOrderScreen({super.key});

  @override
  ConsumerState<NewOrderScreen> createState() => _NewOrderScreenState();
}

class _NewOrderScreenState extends ConsumerState<NewOrderScreen> {
  int? _carId;
  DateTime? _date;
  TimeOfDay? _time;
  final Set<int> _serviceIds = {};
  final _notes = TextEditingController();
  bool _loading = false;

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  String? _timeString() {
    if (_time == null) return null;
    final h = _time!.hour.toString().padLeft(2, '0');
    final m = _time!.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date ?? now,
      firstDate: now,
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _pickTime() async {
    final picked = await showTimePicker(
      context: context,
      initialTime: _time ?? const TimeOfDay(hour: 10, minute: 0),
    );
    if (picked != null) setState(() => _time = picked);
  }

  Future<void> _submit() async {
    if (_carId == null) {
      AppSnack.error(context, 'Выберите автомобиль');
      return;
    }
    if (_serviceIds.isEmpty) {
      AppSnack.error(context, 'Выберите хотя бы одну услугу');
      return;
    }
    setState(() => _loading = true);
    try {
      await ref.read(ordersProvider.notifier).create(
            OrderCreateRequest(
              carId: _carId!,
              scheduledDate: _date,
              scheduledTime: _timeString(),
              notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
              serviceIds: _serviceIds.toList(),
            ),
          );
      if (mounted) {
        AppSnack.success(context, 'Вы записаны на сервис!');
        context.go('/home');
      }
    } on ApiException catch (e) {
      if (mounted) AppSnack.error(context, e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final carsAsync = ref.watch(carsProvider);
    final servicesAsync = ref.watch(servicesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Запись на сервис')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          _label('Автомобиль'),
          carsAsync.when(
            loading: () => const LinearProgressIndicator(),
            error: (e, _) => Text('Ошибка загрузки авто: $e',
                style: const TextStyle(color: Colors.red)),
            data: (cars) => cars.isEmpty
                ? _hint('Сначала добавьте автомобиль в разделе «Авто».')
                : _carDropdown(cars),
          ),
          const SizedBox(height: 20),
          _label('Дата и время'),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: _pickDate,
                  icon: const Icon(Icons.calendar_today),
                  label: Text(_date == null ? 'Дата' : Fmt.date(_date)),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: _pickTime,
                  icon: const Icon(Icons.access_time),
                  label: Text(_timeString() ?? 'Время'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          _label('Услуги'),
          servicesAsync.when(
            loading: () => const LinearProgressIndicator(),
            error: (e, _) => Text('Ошибка загрузки услуг: $e',
                style: const TextStyle(color: Colors.red)),
            data: (services) => _servicesList(services),
          ),
          const SizedBox(height: 20),
          _label('Комментарий'),
          TextField(
            controller: _notes,
            maxLines: 3,
            maxLength: 500,
            decoration: const InputDecoration(
              hintText: 'Опишите проблему или пожелания…',
            ),
          ),
          const SizedBox(height: 12),
          ElevatedButton(
            onPressed: _loading ? null : _submit,
            child: _loading
                ? const SizedBox(
                    height: 22,
                    width: 22,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: Colors.white),
                  )
                : const Text('Записаться'),
          ),
        ],
      ),
    );
  }

  Widget _label(String text) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(text,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
      );

  Widget _hint(String text) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Text(text, style: const TextStyle(color: AppColors.textSecondary)),
      );

  Widget _carDropdown(List<Car> cars) {
    final value = cars.any((c) => c.carId == _carId) ? _carId : null;
    return DropdownButtonFormField<int>(
      initialValue: value,
      isExpanded: true,
      decoration: const InputDecoration(
        prefixIcon: Icon(Icons.directions_car_outlined),
      ),
      items: cars
          .map((c) => DropdownMenuItem<int>(
                value: c.carId,
                child: Text('${c.title} (${c.licensePlate})',
                    overflow: TextOverflow.ellipsis),
              ))
          .toList(),
      onChanged: (v) => setState(() => _carId = v),
    );
  }

  Widget _servicesList(List<Service> services) {
    if (services.isEmpty) return _hint('Список услуг пуст.');
    return Column(
      children: services.map((s) {
        final selected = _serviceIds.contains(s.serviceId);
        return Card(
          margin: const EdgeInsets.symmetric(vertical: 4),
          child: CheckboxListTile(
            value: selected,
            activeColor: AppColors.accent,
            title: Text(s.name),
            subtitle: s.estimatedCost != null
                ? Text('≈ ${Fmt.money(s.estimatedCost)}')
                : (s.description != null ? Text(s.description!) : null),
            onChanged: (v) => setState(() {
              if (v == true) {
                _serviceIds.add(s.serviceId);
              } else {
                _serviceIds.remove(s.serviceId);
              }
            }),
          ),
        );
      }).toList(),
    );
  }
}
