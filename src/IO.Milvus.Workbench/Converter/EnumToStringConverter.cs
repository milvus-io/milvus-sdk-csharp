using System;
using System.Globalization;
using System.Windows.Data;

namespace IO.Milvus.Workbench.Converter
{
    /// <inheritdoc />
    /// <summary>
    /// 枚举转字符串
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(string))]
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.GetType().GetEnumValues();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
