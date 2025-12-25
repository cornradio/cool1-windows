using System;
using System.Globalization;
using System.Windows.Data;
using Cool1Windows.Services;

namespace Cool1Windows.Converters
{
    public class PathToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                return IconHelper.GetIcon(path);
            }
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
