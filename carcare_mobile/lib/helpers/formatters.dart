import 'package:intl/intl.dart';

/// Утилиты форматирования дат, времени и денег.
class Fmt {
  Fmt._();

  static final DateFormat _date = DateFormat('dd.MM.yyyy', 'ru_RU');
  static final DateFormat _dateTime = DateFormat('dd.MM.yyyy HH:mm', 'ru_RU');

  static String date(DateTime? d) => d == null ? '—' : _date.format(d.toLocal());

  static String dateTime(DateTime? d) =>
      d == null ? '—' : _dateTime.format(d.toLocal());

  /// Дата + (необязательно) время "HH:mm".
  static String dateAndTime(DateTime? d, String? time) {
    final datePart = date(d);
    if (time == null || time.isEmpty) return datePart;
    // Сервер отдаёт время как "HH:mm[:ss]" — берём первые 5 символов.
    final t = time.length >= 5 ? time.substring(0, 5) : time;
    return '$datePart, $t';
  }

  static String money(num? value) {
    if (value == null) return '—';
    final f =
        NumberFormat.currency(locale: 'ru_RU', symbol: '₽', decimalDigits: 0);
    return f.format(value);
  }
}
