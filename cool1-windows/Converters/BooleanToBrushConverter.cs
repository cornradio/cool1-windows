using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Cool1Windows.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public System.Windows.Media.Brush? TrueBrush { get; set; }
        public System.Windows.Media.Brush? FalseBrush { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
