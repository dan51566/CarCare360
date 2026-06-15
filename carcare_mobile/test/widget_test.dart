// Базовые тесты доменной логики CarCare360.
import 'package:carcare_mobile/helpers/order_status.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('Отменяемые статусы: только Новый и Назначен', () {
    expect(OrderStatuses.isCancellable(OrderStatuses.newOrder), isTrue);
    expect(OrderStatuses.isCancellable(OrderStatuses.assigned), isTrue);
    expect(OrderStatuses.isCancellable(OrderStatuses.inProgress), isFalse);
    expect(OrderStatuses.isCancellable(OrderStatuses.issued), isFalse);
  });

  test('Активные статусы не включают Выдан и Отменён', () {
    expect(OrderStatuses.isActive(OrderStatuses.inProgress), isTrue);
    expect(OrderStatuses.isActive(OrderStatuses.issued), isFalse);
    expect(OrderStatuses.isActive(OrderStatuses.cancelled), isFalse);
  });
}
