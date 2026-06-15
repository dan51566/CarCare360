import 'package:json_annotation/json_annotation.dart';

part 'order_part_item.g.dart';

/// Запчасть в составе заказа. Соответствует объекту parts[] в OrderDto (camelCase).
@JsonSerializable()
class OrderPartItem {
  @JsonKey(name: 'orderPartID')
  final int orderPartId;

  @JsonKey(name: 'partID')
  final int partId;

  @JsonKey(name: 'partName')
  final String? partName;

  @JsonKey(name: 'quantity')
  final int quantity;

  @JsonKey(name: 'pricePerUnit')
  final double? pricePerUnit;

  const OrderPartItem({
    required this.orderPartId,
    required this.partId,
    this.partName,
    required this.quantity,
    this.pricePerUnit,
  });

  /// Стоимость позиции.
  double get lineTotal => (pricePerUnit ?? 0) * quantity;

  factory OrderPartItem.fromJson(Map<String, dynamic> json) =>
      _$OrderPartItemFromJson(json);
  Map<String, dynamic> toJson() => _$OrderPartItemToJson(this);
}
