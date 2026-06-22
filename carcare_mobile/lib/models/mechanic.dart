import 'package:json_annotation/json_annotation.dart';

part 'mechanic.g.dart';

/// Механик в каталоге выбора клиента с признаком избранного.
/// Соответствует MechanicCatalogDto сервера (camelCase JSON).
/// Изменение №2, Доработка 3.
@JsonSerializable()
class MechanicCatalogItem {
  @JsonKey(name: 'mechanicID')
  final int mechanicId;

  @JsonKey(name: 'fullName')
  final String fullName;

  @JsonKey(name: 'specializationName')
  final String? specializationName;

  @JsonKey(name: 'qualificationLevel')
  final String? qualificationLevel;

  @JsonKey(name: 'isFavorite')
  final bool isFavorite;

  const MechanicCatalogItem({
    required this.mechanicId,
    required this.fullName,
    this.specializationName,
    this.qualificationLevel,
    required this.isFavorite,
  });

  /// Копия с изменённым флагом избранного (для оптимистичного обновления UI).
  MechanicCatalogItem copyWith({bool? isFavorite}) => MechanicCatalogItem(
        mechanicId: mechanicId,
        fullName: fullName,
        specializationName: specializationName,
        qualificationLevel: qualificationLevel,
        isFavorite: isFavorite ?? this.isFavorite,
      );

  factory MechanicCatalogItem.fromJson(Map<String, dynamic> json) =>
      _$MechanicCatalogItemFromJson(json);
  Map<String, dynamic> toJson() => _$MechanicCatalogItemToJson(this);
}
