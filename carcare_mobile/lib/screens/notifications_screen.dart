import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../helpers/app_theme.dart';
import '../helpers/formatters.dart';
import '../providers/providers.dart';
import '../services/notification_service.dart';

/// Список полученных уведомлений (push / локальные).
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final items = ref.watch(notificationsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Уведомления'),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_active_outlined),
            tooltip: 'Тестовое уведомление',
            onPressed: () => NotificationService.instance.showLocalTest(),
          ),
        ],
      ),
      body: items.isEmpty
          ? const Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.notifications_none,
                      size: 72, color: AppColors.textSecondary),
                  SizedBox(height: 12),
                  Text('Уведомлений пока нет',
                      style: TextStyle(color: AppColors.textSecondary)),
                ],
              ),
            )
          : ListView.separated(
              itemCount: items.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (_, i) {
                final n = items[i];
                return ListTile(
                  leading: const CircleAvatar(
                    backgroundColor: AppColors.primary,
                    child: Icon(Icons.notifications, color: Colors.white),
                  ),
                  title: Text(n.title,
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text(n.body),
                  trailing: Text(Fmt.dateTime(n.receivedAt),
                      style: const TextStyle(
                          fontSize: 11, color: AppColors.textSecondary)),
                );
              },
            ),
    );
  }
}
