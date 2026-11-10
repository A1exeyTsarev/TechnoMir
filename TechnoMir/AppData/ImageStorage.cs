using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Фото товаров (ТЗ, F3). Файл, выбранный через OpenFileDialog, копируется
    /// в папку ProductImages рядом с программой, а в БД (Products.ImagePath)
    /// пишется относительный путь вида «ProductImages/имя.png».
    /// </summary>
    public static class ImageStorage
    {
        public const string FolderName = "ProductImages";
        public const string FileFilter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
        private const string PlaceholderUri = "pack://application:,,,/Images/picture.png";

        private static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>Копирует картинку в ProductImages и возвращает относительный путь для БД.</summary>
        public static string SaveImage(string sourceFile)
        {
            string folder = Path.Combine(BaseDirectory, FolderName);
            Directory.CreateDirectory(folder);

            // Уникальное имя, чтобы не затереть фото другого товара
            string name = Path.GetFileNameWithoutExtension(sourceFile);
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            string fileName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

            File.Copy(sourceFile, Path.Combine(folder, fileName), true);
            return FolderName + "/" + fileName;
        }

        /// <summary>Полный путь к файлу по относительному пути из БД или null, если файла нет.</summary>
        public static string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            string full = Path.Combine(BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? full : null;
        }

        /// <summary>
        /// Картинка для Image.Source. Файл читается в память целиком (OnLoad),
        /// чтобы он не блокировался. Нет файла — заглушка picture.png.
        /// </summary>
        public static ImageSource Load(string relativePath)
        {
            string full = GetFullPath(relativePath);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = full != null ? new Uri(full, UriKind.Absolute) : new Uri(PlaceholderUri);
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
