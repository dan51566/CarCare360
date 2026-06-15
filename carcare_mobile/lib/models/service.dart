import 'package:json_annotation/json_annotation.dart';

part 'service.g.dart';

/// Услуга автосервиса. Соответствует ServiceDto сервера (camelCase JSON).
@JsonSerializable()
class Service {
  @JsonKey(name: 'serviceID')
  final int serviceId;

  @JsonKey(name: 'name')
  final String name;

  @JsonKey(name: 'description')
  final String? description;

  /// Нормо-часы.
  @JsonKey(name: 'normHour')
  final double normHour;

  /// Базовая цена за нормо-час (может отсутствовать).
  @JsonKey(name: 'basePrice')
  final double? basePrice;

  const Service({
    required this.serviceId,
    required this.name,
    this.description,
    required this.normHour,
    this.basePrice,
  });

  /// Ориентировочная стоимость услуги (нормо-часы × цена), если цена задана.
  double? get estimatedCost =>
      basePrice == null ? null : normHour * basePrice!;

  factory Service.fromJson(Map<String, dynamic> json) =>
      _$ServiceFromJson(json);
  Map<String, dynamic> toJson() => _$ServiceToJson(this);
}
