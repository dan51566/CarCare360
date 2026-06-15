import 'package:json_annotation/json_annotation.dart';

import 'user.dart';

part 'auth_response.g.dart';

/// Ответ на login / register / refresh.
@JsonSerializable()
class AuthResponse {
  @JsonKey(name: 'accessToken')
  final String accessToken;

  @JsonKey(name: 'refreshToken')
  final String refreshToken;

  @JsonKey(name: 'accessTokenExpiresAt')
  final DateTime accessTokenExpiresAt;

  @JsonKey(name: 'user')
  final User user;

  const AuthResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAt,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) =>
      _$AuthResponseFromJson(json);
  Map<String, dynamic> toJson() => _$AuthResponseToJson(this);
}
