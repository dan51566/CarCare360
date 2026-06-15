import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../helpers/app_theme.dart';
import '../helpers/ui_feedback.dart';
import '../providers/providers.dart';

/// Экран профиля пользователя с поддержкой локальной аватарки.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  void _changePasswordStub(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Смена пароля'),
        content: const Text(
            'Функция смены пароля находится в разработке и будет добавлена позже.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: const Text('Понятно')),
        ],
      ),
    );
  }

  Future<void> _logout(BuildContext context, WidgetRef ref) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Выйти из аккаунта?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Отмена')),
          TextButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Выйти')),
        ],
      ),
    );
    if (ok == true) {
      await ref.read(authProvider.notifier).logout();
    }
  }

  /// Показывает Bottom Sheet для выбора источника изображения (галерея / камера).
  void _showAvatarPicker(BuildContext context, WidgetRef ref) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 8),
            Container(
              width: 40, height: 4,
              decoration: BoxDecoration(
                color: AppColors.textSecondary.withValues(alpha: 0.3),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 12),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined,
                  color: AppColors.primary),
              title: const Text('Из галереи'),
              onTap: () {
                Navigator.pop(ctx);
                ref
                    .read(avatarFileProvider.notifier)
                    .pick(ImageSource.gallery);
              },
            ),
            ListTile(
              leading: const Icon(Icons.camera_alt_outlined,
                  color: AppColors.primary),
              title: const Text('Сделать фото'),
              onTap: () {
                Navigator.pop(ctx);
                ref
                    .read(avatarFileProvider.notifier)
                    .pick(ImageSource.camera);
              },
            ),
            // Кнопка удаления — только если аватарка установлена.
            if (ref.read(avatarFileProvider) != null)
              ListTile(
                leading:
                    const Icon(Icons.delete_outline, color: Colors.red),
                title: const Text('Удалить фото',
                    style: TextStyle(color: Colors.red)),
                onTap: () {
                  Navigator.pop(ctx);
                  ref.read(avatarFileProvider.notifier).remove();
                  AppSnack.info(context, 'Аватарка удалена');
                },
              ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider);
    // Следим за файлом аватарки — при изменении виджет перестраивается.
    final avatarFile = ref.watch(avatarFileProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Профиль')),
      body: user == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              children: [
                const SizedBox(height: 24),
                // ── Аватарка: нажимаемый круглый виджет с оранжевой иконкой-камерой.
                Center(
                  child: GestureDetector(
                    onTap: () => _showAvatarPicker(context, ref),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        // Основной круг с аватаркой или иконкой-заглушкой.
                        CircleAvatar(
                          radius: 44,
                          backgroundColor: AppColors.primary,
                          backgroundImage: avatarFile != null
                              ? FileImage(avatarFile)
                              : null,
                          child: avatarFile == null
                              ? const Icon(Icons.person,
                                  size: 48, color: Colors.white)
                              : null,
                        ),
                        // Маленький оранжевый кружок с иконкой камеры.
                        Positioned(
                          bottom: 0,
                          right: 0,
                          child: Container(
                            width: 28,
                            height: 28,
                            decoration: BoxDecoration(
                              color: AppColors.accent,
                              shape: BoxShape.circle,
                              border: Border.all(
                                  color: Colors.white, width: 2),
                            ),
                            child: const Icon(Icons.camera_alt,
                                size: 14, color: Colors.white),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Center(
                  child: Text(user.fullName,
                      style: const TextStyle(
                          fontSize: 20, fontWeight: FontWeight.bold)),
                ),
                Center(
                  child: Text(user.role,
                      style: const TextStyle(
                          color: AppColors.textSecondary)),
                ),
                const SizedBox(height: 24),
                _tile(Icons.account_circle_outlined, 'Логин', user.login),
                _tile(Icons.email_outlined, 'Email', user.email ?? '—'),
                _tile(Icons.phone_outlined, 'Телефон', user.phone ?? '—'),
                const SizedBox(height: 12),
                ListTile(
                  leading:
                      const Icon(Icons.edit, color: AppColors.primary),
                  title: const Text('Редактировать профиль'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push('/edit-profile'),
                ),
                ListTile(
                  leading: const Icon(Icons.lock_outline,
                      color: AppColors.primary),
                  title: const Text('Сменить пароль'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _changePasswordStub(context),
                ),
                const Divider(),
                ListTile(
                  leading:
                      const Icon(Icons.logout, color: Colors.red),
                  title: const Text('Выйти',
                      style: TextStyle(color: Colors.red)),
                  onTap: () => _logout(context, ref),
                ),
              ],
            ),
    );
  }

  Widget _tile(IconData icon, String label, String value) {
    return ListTile(
      leading: Icon(icon, color: AppColors.textSecondary),
      title: Text(label,
          style: const TextStyle(
              color: AppColors.textSecondary, fontSize: 13)),
      subtitle: Text(value,
          style: const TextStyle(
              color: AppColors.textPrimary, fontSize: 16)),
    );
  }
}
