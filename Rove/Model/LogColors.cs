using System.Windows.Media;

namespace Rove.Model
{
    public static class LogColors
    {
        public static SolidColorBrush InfoForeground { get; } = new SolidColorBrush(Colors.Black);

        public static SolidColorBrush WarnForeground { get; } = new SolidColorBrush(Colors.Orange);

        public static SolidColorBrush ErrorForeground { get; } = new SolidColorBrush(Colors.Red);
    }
}
