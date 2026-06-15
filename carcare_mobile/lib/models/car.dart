import 'package:json_annotation/json_annotation.dart';

part 'car.g.dart';

/// Автомобиль клиента. Соответствует CarDto сервера (camelCase JSON).
@JsonSerializable()
class Car {
  @JsonKey(name: 'carID')
  final int carId;

  @JsonKey(name: 'clientID')
  final int clientId;

  @JsonKey(name: 'clientName')
  final String? clientName;

  @JsonKey(name: 'modelID')
  final int modelId;

  @JsonKey(name: 'brandName')
  final String? brandName;

  @JsonKey(name: 'modelName')
  final String? modelName;

  @JsonKey(name: 'year')
  final int? year;

  @JsonKey(name: 'vin')
  final String? vin;

  @JsonKey(name: 'licensePlate')
  final String licensePlate;

  @JsonKey(name: 'color')
  final String? color;

  @JsonKey(name: 'mileage')
  final int? mileage;

  const Car({
    required this.carId,
    required this.clientId,
    this.clientName,
    required this.modelId,
    this.brandName,
    this.modelName,
    this.year,
    this.vin,
    required this.licensePlate,
    this.color,
    this.mileage,
  });

  /// Человекочитаемое название «Марка Модель».
  String get title {
    final parts = [brandName, modelName]
        .where((s) => s != null && s.trim().isNotEmpty)
        .toList();
    return parts.isEmpty ? licensePlate : parts.join(' ');
  }

  factory Car.fromJson(Map<String, dynamic> json) => _$CarFromJson(json);
  Map<String, dynamic> toJson() => _$CarToJson(this);
}
