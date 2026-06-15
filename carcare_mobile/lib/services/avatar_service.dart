import 'dart:io';

import 'package:image_picker/image_picker.dart';
import 'package:path_provider/path_provider.dart';

/// Сервис управления локальной аватаркой профиля клиента.
///
/// Аватарка хранится как файл [_fileName] в директории документов приложения
/// ([getApplicationDocumentsDirectory]). Сервер не используется — хранение
/// полностью локальное.
class AvatarService {
  static const String _fileName = 'profile_avatar.jpg';

  final ImagePicker _picker = ImagePicker();

  /// Возвращает текущий файл аватарки или null, если аватарка не установлена.
  Future<File?> getAvatarFile() async {
    final dir = await getApplicationDocumentsDirectory();
    final file = File('${dir.path}/$_fileName');
    return file.existsSync() ? file : null;
  }

  /// Открывает системный пикер изображений ([source] — галерея или камера),
  /// сохраняет выбранное фото и возвращает сохранённый файл.
  /// Возвращает null, если пользователь отменил выбор.
  Future<File?> pickAndSave(ImageSource source) async {
    final XFile? picked = await _picker.pickImage(
      source: source,
      imageQuality: 85,  // умеренное сжатие для экономии места
      maxWidth: 512,
      maxHeight: 512,
    );
    if (picked == null) return null;
    return saveAvatar(picked);
  }

  /// Сохраняет [imageFile] как аватарку и возвращает новый File.
  Future<File> saveAvatar(XFile imageFile) async {
    final dir = await getApplicationDocumentsDirectory();
    final dest = File('${dir.path}/$_fileName');
    // Перезаписываем текущую аватарку.
    await File(imageFile.path).copy(dest.path);
    return dest;
  }

  /// Удаляет сохранённую аватарку (сброс до иконки-заглушки).
  Future<void> deleteAvatar() async {
    final file = await getAvatarFile();
    if (file != null) await file.delete();
  }
}
