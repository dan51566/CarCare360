// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'mechanic.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

MechanicCatalogItem _$MechanicCatalogItemFromJson(Map<String, dynamic> json) =>
    MechanicCatalogItem(
      mechanicId: (json['mechanicID'] as num).toInt(),
      fullName: json['fullName'] as String,
      specializationName: json['specializationName'] as String?,
      qualificationLevel: json['qualificationLevel'] as String?,
      isFavorite: json['isFavorite'] as bool,
    );

Map<String, dynamic> _$MechanicCatalogItemToJson(
  MechanicCatalogItem instance,
) => <String, dynamic>{
  'mechanicID': instance.mechanicId,
  'fullName': instance.fullName,
  'specializationName': instance.specializationName,
  'qualificationLevel': instance.qualificationLevel,
  'isFavorite': instance.isFavorite,
};
