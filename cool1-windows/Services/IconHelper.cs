using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cool1Windows.Services
{
    public static class IconHelper
    {
        private static ImageSource? _fishIcon;

        public static ImageSource? GetIcon(string path)
        {
            try
            {
                // 如果是本程序的名称，使用 fish.png
                if (!string.IsNullOrEmpty(path) && path.Contains("Cool1Windows", StringComparison.OrdinalIgnoreCase))
                {
                    return GetFishIcon();
                }

                if (File.Exists(path))
                {
                    // 使用 System.Drawing 提取关联图标
                    using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    {
                        if (icon != null)
                        {
                            return Imaging.CreateBitmapSourceFromHIcon(
                                icon.Handle,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                        }
                    }
                }
            }
            catch { }
            return GetFishIcon();
        }

        public static ImageSource? GetFishIcon()
        {
            if (_fishIcon == null)
            {
                try
                {
                    string fishPath = @"c:\Users\kasus\Documents\GitHub\cool1\fish.png";
                    if (File.Exists(fishPath))
                    {
                        _fishIcon = new BitmapImage(new Uri(fishPath));
                    }
                }
                catch { }
            }
            return _fishIcon;
        }
    }
}
