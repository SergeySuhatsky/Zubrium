using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Zubrium.Maui.Converters
{
    public class ZeroToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Возвращает true, если элементов 0 (чтобы показать надпись "Нет результатов")
                return count == 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}