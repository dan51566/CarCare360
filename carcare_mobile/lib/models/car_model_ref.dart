import 'package:json_annotation/json_annotation.dart';

part 'car_model_ref.g.dart';

/// Справочная модель автомобиля (марка + модель) из GET /api/car-models.
/// Используется при добавлении авто, чтобы клиент выбрал готовый modelID.
@JsonSerializable()
class CarModelRef {
  @JsonKey(name: 'modelID')
  final int modelId;

  @JsonKey(name: 'brandID')
  final int brandId;

  @JsonKey(name: 'brandName')
  final String brandName;

  @JsonKey(name: 'name')
  final String name;

  @JsonKey(name: 'yearFrom')
  final int? yearFrom;

  @JsonKey(name: 'yearTo')
  final int? yearTo;

  const CarModelRef({
    required this.modelId,
    required this.brandId,
    required this.brandName,
    required this.name,
    this.yearFrom,
    this.yearTo,
  });

  /// Отображаемое название «Марка Модель».
  String get title => '$brandName $name';

  factory CarModelRef.fromJson(Map<String, dynamic> json) =>
      _$CarModelRefFromJson(json);
  Map<String, dynamic> toJson() => _$CarModelRefToJson(this);
}
