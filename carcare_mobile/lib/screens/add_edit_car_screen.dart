import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../helpers/ui_feedback.dart';
import '../models/car.dart';
import '../models/car_model_ref.dart';
import '../models/requests.dart';
import '../providers/providers.dart';
import '../services/api_exception.dart';

/// Форма добавления / редактирования автомобиля.
class AddEditCarScreen extends ConsumerStatefulWidget {
  /// null — добавление; иначе — редактирование.
  final Car? car;
  const AddEditCarScreen({super.key, this.car});

  @override
  ConsumerState<AddEditCarScreen> createState() => _AddEditCarScreenState();
}

class _AddEditCarScreenState extends ConsumerState<AddEditCarScreen> {
  final _formKey = GlobalKey<FormState>();
  final _plate = TextEditingController();
  final _vin = TextEditingController();
  final _year = TextEditingController();
  final _color = TextEditingController();
  final _mileage = TextEditingController();
  int? _modelId;
  bool _loading = false;

  bool get _isEdit => widget.car != null;

  @override
  void initState() {
    super.initState();
    final c = widget.car;
    if (c != null) {
      _plate.text = c.licensePlate;
      _vin.text = c.vin ?? '';
      _year.text = c.year?.toString() ?? '';
      _color.text = c.color ?? '';
      _mileage.text = c.mileage?.toString() ?? '';
      _modelId = c.modelId;
    }
  }

  @override
  void dispose() {
    _plate.dispose();
    _vin.dispose();
    _year.dispose();
    _color.dispose();
    _mileage.dispose();
    super.dispose();
  }

  int? _toInt(String s) => s.trim().isEmpty ? null : int.tryParse(s.trim());

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_modelId == null) {
      AppSnack.error(context, 'Выберите марку и модель');
      return;
    }
    setState(() => _loading = true);
    final request = CarRequest(
      modelId: _modelId!,
      licensePlate: _plate.text.trim(),
      vin: _vin.text.trim().isEmpty ? null : _vin.text.trim(),
      year: _toInt(_year.text),
      color: _color.text.trim().isEmpty ? null : _color.text.trim(),
      mileage: _toInt(_mileage.text),
    );
    try {
      final notifier = ref.read(carsProvider.notifier);
      if (_isEdit) {
        await notifier.edit(widget.car!.carId, request);
      } else {
        await notifier.add(request);
      }
      if (mounted) {
        AppSnack.success(
            context, _isEdit ? 'Автомобиль обновлён' : 'Автомобиль добавлен');
        context.pop();
      }
    } on ApiException catch (e) {
      if (mounted) AppSnack.error(context, e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final modelsAsync = ref.watch(carModelsProvider);

    return Scaffold(
      appBar: AppBar(
          title: Text(_isEdit ? 'Редактировать авто' : 'Добавить авто')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              modelsAsync.when(
                loading: () => const Padding(
                  padding: EdgeInsets.symmetric(vertical: 12),
                  child: LinearProgressIndicator(),
                ),
                error: (e, _) => Text('Не удалось загрузить марки: $e',
                    style: const TextStyle(color: Colors.red)),
                data: (models) => _modelDropdown(models),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _plate,
                textCapitalization: TextCapitalization.characters,
                decoration: const InputDecoration(
                  labelText: 'Госномер *',
                  prefixIcon: Icon(Icons.confirmation_number_outlined),
                ),
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Введите госномер'
                    : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _vin,
                textCapitalization: TextCapitalization.characters,
                decoration: const InputDecoration(
                  labelText: 'VIN',
                  prefixIcon: Icon(Icons.tag),
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _year,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Год выпуска',
                  prefixIcon: Icon(Icons.calendar_today_outlined),
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return null;
                  final y = int.tryParse(v.trim());
                  if (y == null || y < 1900 || y > DateTime.now().year + 1) {
                    return 'Некорректный год';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _color,
                decoration: const InputDecoration(
                  labelText: 'Цвет',
                  prefixIcon: Icon(Icons.palette_outlined),
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _mileage,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Пробег, км',
                  prefixIcon: Icon(Icons.speed),
                ),
              ),
              const SizedBox(height: 28),
              ElevatedButton(
                onPressed: _loading ? null : _submit,
                child: _loading
                    ? const SizedBox(
                        height: 22,
                        width: 22,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : Text(_isEdit ? 'Сохранить' : 'Добавить'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _modelDropdown(List<CarModelRef> models) {
    // Если текущий modelId отсутствует в списке (например, справочник пуст) —
    // не передаём value, чтобы Dropdown не упал.
    final value =
        models.any((m) => m.modelId == _modelId) ? _modelId : null;
    return DropdownButtonFormField<int>(
      initialValue: value,
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Марка и модель *',
        prefixIcon: Icon(Icons.directions_car_outlined),
      ),
      items: models
          .map((m) => DropdownMenuItem<int>(
                value: m.modelId,
                child: Text(m.title, overflow: TextOverflow.ellipsis),
              ))
          .toList(),
      onChanged: (v) => setState(() => _modelId = v),
      validator: (v) => v == null ? 'Выберите марку и модель' : null,
    );
  }
}
