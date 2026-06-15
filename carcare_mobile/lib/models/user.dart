import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';

/// Пользователь системы (для мобильного приложения — клиент).
/// Соответствует UserDto сервера (camelCase JSON, акронимы вида userID).
@JsonSerializable()
class User {
  @JsonKey(name: 'userID')
  final int userId;

  @JsonKey(name: 'login')
  final String login;

  @JsonKey(name: 'fullName')
  final String fullName;

  @JsonKey(name: 'email')
  final String? email;

  @JsonKey(name: 'phone')
  final String? phone;

  @JsonKey(name: 'role')
  final String role;

  @JsonKey(name: 'isActive', defaultValue: true)
  final bool isActive;

  @JsonKey(name: 'createdAt')
  final DateTime? createdAt;

  const User({
    required this.userId,
    required this.login,
    required this.fullName,
    this.email,
    this.phone,
    required this.role,
    this.isActive = true,
    this.createdAt,
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);
  Map<String, dynamic> toJson() => _$UserToJson(this);
}
