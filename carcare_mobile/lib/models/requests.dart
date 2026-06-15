import 'package:json_annotation/json_annotation.dart';

part 'requests.g.dart';

/// Запрос на вход. POST /api/auth/login
@JsonSerializable(createFactory: false, includeIfNull: false)
class LoginRequest {
  @JsonKey(name: 'Login')
  final String login;
  @JsonKey(name: 'Password')
  final String password;

  const LoginRequest({required this.login, required this.password});
  Map<String, dynamic> toJson() => _$LoginRequestToJson(this);
}

/// Запрос на регистрацию. POST /api/auth/register
@JsonSerializable(createFactory: false, includeIfNull: false)
class RegisterRequest {
  @JsonKey(name: 'Login')
  final String login;
  @JsonKey(name: 'Password')
  final String password;
  @JsonKey(name: 'FullName')
  final String fullName;
  @JsonKey(name: 'Email')
  final String? email;
  @JsonKey(name: 'Phone')
  final String? phone;

  const RegisterRequest({
    required this.login,
    required this.password,
    required this.fullName,
    this.email,
    this.phone,
  });
  Map<String, dynamic> toJson() => _$RegisterRequestToJson(this);
}

/// Запрос на создание/обновление автомобиля. POST/PUT /api/cars
@JsonSerializable(createFactory: false, includeIfNull: false)
class CarRequest {
  @JsonKey(name: 'ModelID')
  final int modelId;
  @JsonKey(name: 'Year')
  final int? year;
  @JsonKey(name: 'VIN')
  final String? vin;
  @JsonKey(name: 'LicensePlate')
  final String licensePlate;
  @JsonKey(name: 'Color')
  final String? color;
  @JsonKey(name: 'Mileage')
  final int? mileage;

  const CarRequest({
    required this.modelId,
    this.year,
    this.vin,
    required this.licensePlate,
    this.color,
    this.mileage,
  });
  Map<String, dynamic> toJson() => _$CarRequestToJson(this);
}

/// Запрос на создание заказа. POST /api/orders
@JsonSerializable(createFactory: false, includeIfNull: false)
class OrderCreateRequest {
  @JsonKey(name: 'CarID')
  final int carId;
  @JsonKey(name: 'ScheduledDate')
  final DateTime? scheduledDate;

  /// Время в формате "HH:mm".
  @JsonKey(name: 'ScheduledTime')
  final String? scheduledTime;
  @JsonKey(name: 'Notes')
  final String? notes;
  @JsonKey(name: 'ServiceIds')
  final List<int> serviceIds;

  const OrderCreateRequest({
    required this.carId,
    this.scheduledDate,
    this.scheduledTime,
    this.notes,
    required this.serviceIds,
  });
  Map<String, dynamic> toJson() => _$OrderCreateRequestToJson(this);
}

/// Запрос на обновление профиля клиента. PUT /api/clients/{id}
@JsonSerializable(createFactory: false, includeIfNull: false)
class ClientUpdateRequest {
  @JsonKey(name: 'FullName')
  final String fullName;
  @JsonKey(name: 'Email')
  final String? email;
  @JsonKey(name: 'Phone')
  final String? phone;

  const ClientUpdateRequest({
    required this.fullName,
    this.email,
    this.phone,
  });
  Map<String, dynamic> toJson() => _$ClientUpdateRequestToJson(this);
}
