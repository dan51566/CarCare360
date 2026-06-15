/// Уведомление, полученное приложением (push или локальное).
class AppNotification {
  final String title;
  final String body;
  final DateTime receivedAt;

  AppNotification({
    required this.title,
    required this.body,
    DateTime? receivedAt,
  }) : receivedAt = receivedAt ?? DateTime.now();
}
