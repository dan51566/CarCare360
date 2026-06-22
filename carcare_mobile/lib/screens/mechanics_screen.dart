import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../helpers/app_theme.dart';
import '../helpers/ui_feedback.dart';
import '../models/mechanic.dart';
import '../providers/providers.dart';
import '../services/api_exception.dart';

/// Экран «Механики»: список механиков с возможностью отметить избранных
/// (звёздочка). Избранные отображаются первыми. Изменение №2, Доработка 3.
class MechanicsScreen extends ConsumerWidget {
  const MechanicsScreen({super.key});

  Future<void> _toggle(
      BuildContext context, WidgetRef ref, MechanicCatalogItem m) async {
    try {
      await ref
          .read(mechanicsCatalogProvider.notifier)
          .toggleFavorite(m.mechanicId);
    } on ApiException catch (e) {
      if (context.mounted) AppSnack.error(context, e.message);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final mechanicsAsync = ref.watch(mechanicsCatalogProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Механики')),
      body: RefreshIndicator(
        onRefresh: () => ref.read(mechanicsCatalogProvider.notifier).reload(),
        child: mechanicsAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => _error('$e'),
          data: (mechanics) {
            if (mechanics.isEmpty) return _empty();
            return ListView.separated(
              padding: const EdgeInsets.symmetric(vertical: 8),
              itemCount: mechanics.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (_, i) {
                final m = mechanics[i];
                final subtitle = [m.specializationName, m.qualificationLevel]
                    .where((s) => s != null && s.trim().isNotEmpty)
                    .join(' • ');
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: AppColors.primary,
                    child: Text(
                      m.fullName.isNotEmpty ? m.fullName[0] : '?',
                      style: const TextStyle(color: Colors.white),
                    ),
                  ),
                  title: Text(m.fullName),
                  subtitle: subtitle.isEmpty ? null : Text(subtitle),
                  trailing: IconButton(
                    icon: Icon(
                      m.isFavorite ? Icons.star : Icons.star_border,
                      color:
                          m.isFavorite ? Colors.amber : AppColors.textSecondary,
                    ),
                    tooltip:
                        m.isFavorite ? 'Убрать из избранного' : 'В избранное',
                    onPressed: () => _toggle(context, ref, m),
                  ),
                );
              },
            );
          },
        ),
      ),
    );
  }

  Widget _empty() => ListView(
        children: const [
          SizedBox(height: 120),
          Icon(Icons.engineering_outlined,
              size: 72, color: AppColors.textSecondary),
          SizedBox(height: 16),
          Center(
            child: Text('Список механиков пуст',
                style:
                    TextStyle(color: AppColors.textSecondary, fontSize: 16)),
          ),
        ],
      );

  Widget _error(String msg) => ListView(
        children: [
          const SizedBox(height: 120),
          const Icon(Icons.error_outline, size: 56, color: Colors.red),
          const SizedBox(height: 12),
          Center(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(msg, textAlign: TextAlign.center),
            ),
          ),
        ],
      );
}
