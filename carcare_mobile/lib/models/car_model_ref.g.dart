// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'car_model_ref.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

CarModelRef _$CarModelRefFromJson(Map<String, dynamic> json) => CarModelRef(
  modelId: (json['modelID'] as num).toInt(),
  brandId: (json['brandID'] as num).toInt(),
  brandName: json['brandName'] as String,
  name: json['name'] as String,
  yearFrom: (json['yearFrom'] as num?)?.toInt(),
  yearTo: (json['yearTo'] as num?)?.toInt(),
);

Map<String, dynamic> _$CarModelRefToJson(CarModelRef instance) =>
    <String, dynamic>{
      'modelID': instance.modelId,
      'brandID': instance.brandId,
      'brandName': instance.brandName,
      'name': instance.name,
      'yearFrom': instance.yearFrom,
      'yearTo': instance.yearTo,
    };
