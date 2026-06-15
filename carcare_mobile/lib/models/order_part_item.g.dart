// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'order_part_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

OrderPartItem _$OrderPartItemFromJson(Map<String, dynamic> json) =>
    OrderPartItem(
      orderPartId: (json['orderPartID'] as num).toInt(),
      partId: (json['partID'] as num).toInt(),
      partName: json['partName'] as String?,
      quantity: (json['quantity'] as num).toInt(),
      pricePerUnit: (json['pricePerUnit'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$OrderPartItemToJson(OrderPartItem instance) =>
    <String, dynamic>{
      'orderPartID': instance.orderPartId,
      'partID': instance.partId,
      'partName': instance.partName,
      'quantity': instance.quantity,
      'pricePerUnit': instance.pricePerUnit,
    };
