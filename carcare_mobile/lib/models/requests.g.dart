// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'requests.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Map<String, dynamic> _$LoginRequestToJson(LoginRequest instance) =>
    <String, dynamic>{'Login': instance.login, 'Password': instance.password};

Map<String, dynamic> _$RegisterRequestToJson(RegisterRequest instance) =>
    <String, dynamic>{
      'Login': instance.login,
      'Password': instance.password,
      'FullName': instance.fullName,
      'Email': ?instance.email,
      'Phone': ?instance.phone,
    };

Map<String, dynamic> _$CarRequestToJson(CarRequest instance) =>
    <String, dynamic>{
      'ModelID': instance.modelId,
      'Year': ?instance.year,
      'VIN': ?instance.vin,
      'LicensePlate': instance.licensePlate,
      'Color': ?instance.color,
      'Mileage': ?instance.mileage,
    };

Map<String, dynamic> _$OrderCreateRequestToJson(OrderCreateRequest instance) =>
    <String, dynamic>{
      'CarID': instance.carId,
      'ScheduledDate': ?instance.scheduledDate?.toIso8601String(),
      'ScheduledTime': ?instance.scheduledTime,
      'Notes': ?instance.notes,
      'ServiceIds': instance.serviceIds,
    };

Map<String, dynamic> _$ClientUpdateRequestToJson(
  ClientUpdateRequest instance,
) => <String, dynamic>{
  'FullName': instance.fullName,
  'Email': ?instance.email,
  'Phone': ?instance.phone,
};
