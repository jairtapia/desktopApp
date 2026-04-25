using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DesktopAssistant.Converters;

public class FieldTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value as string) switch
        {
            "toggle" => new SolidColorBrush(Color.FromArgb(255, 59,  130, 246)),
            "slider" => new SolidColorBrush(Color.FromArgb(255, 245, 158,  11)),
            "select" => new SolidColorBrush(Color.FromArgb(255, 139,  92, 246)),
            "info"   => new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)),
            _        => new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
