using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Rove.View
{
    public class ShowOnDisabledConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = values[0] as bool?;
            var isCommandAvailable = values[1] as bool?;
            return isVisible == false && isCommandAvailable == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolean = value as bool?;
            return boolean == true ? "Hide" : "Show";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ProcessInfo : UserControl
    {
        public ProcessInfo()
        {
            InitializeComponent();
        }
    }
}
