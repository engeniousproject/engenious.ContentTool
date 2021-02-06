using System;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using engenious.ContentTool.Models;

namespace engenious.ContentTool.Avalonia
{
    public class LogTypeConverter : IValueConverter
    {
        private Bitmap OpenBitamp(string rawUri)
        {
            Uri uri;
            // Allow for assembly overrides
            if (rawUri.StartsWith("avares://"))
            {
                uri = new Uri(rawUri);
            }
            else
            {
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                uri = new Uri($"avares://{assemblyName}{rawUri}");
            }
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var asset = assets.Open(uri);
            return new Bitmap(asset);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LogType logType))
                return null;
            return logType switch
            {
                LogType.None => null,
                LogType.Success => OpenBitamp("/Resources/Success.png"),
                LogType.Information => OpenBitamp("/Resources/Info.png"),
                LogType.Warning => OpenBitamp("/Resources/Warning.png"),
                LogType.Error => OpenBitamp("/Resources/Error.png"),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}