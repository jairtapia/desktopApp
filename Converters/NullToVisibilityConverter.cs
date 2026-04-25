using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DesktopAssistant.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool visible = value switch
        {
            null                              => false,
            string s                          => !string.IsNullOrEmpty(s),
            System.Collections.ICollection c => c.Count > 0,
            _                                => true,
        };
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
