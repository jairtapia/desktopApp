using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DesktopAssistant.Converters;

public class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    var r = System.Convert.ToByte(hex[..2], 16);
                    var g = System.Convert.ToByte(hex[2..4], 16);
                    var b = System.Convert.ToByte(hex[4..6], 16);
                    return new SolidColorBrush(Color.FromArgb(255, r, g, b));
                }
            }
            catch { }
        }
        return new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
