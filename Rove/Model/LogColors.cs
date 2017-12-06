using FastColoredTextBoxNS;
using System.Windows.Media;
using System.Drawing;

namespace Rove.Model
{
    public static class LogColors
    {
        public static Style WarnStyle { get; } = new TextStyle(System.Drawing.Brushes.Orange, null, FontStyle.Regular);

        public static Style ErrorStyle { get; } = new TextStyle(System.Drawing.Brushes.Red, null, FontStyle.Regular);

        public static Style StartupStyle { get; } = new TextStyle(System.Drawing.Brushes.Blue, null, FontStyle.Regular);
    }
}
