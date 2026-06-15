// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

User _$UserFromJson(Map<String, dynamic> json) => User(
  userId: (json['userID'] as num).toInt(),
  login: json['login'] as String,
  fullName: json['fullName'] as String,
  email: json['email'] as String?,
  phone: json['phone'] as String?,
  role: json['role'] as String,
  isActive: json['isActive'] as bool? ?? true,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$UserToJson(User instance) => <String, dynamic>{
  'userID': instance.userId,
  'login': instance.login,
  'fullName': instance.fullName,
  'email': instance.email,
  'phone': instance.phone,
  'role': instance.role,
  'isActive': instance.isActive,
  'createdAt': instance.createdAt?.toIso8601String(),
};
