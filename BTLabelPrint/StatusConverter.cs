using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace BTLabelPrint
{
    class StatusConverter : IValueConverter
    {
        private static (string Text, Brush Brush)[] statuses = 
        { 
            ("не подтвержден", Brushes.Gray),
            ("ожидает оплаты", new SolidColorBrush(Color.FromRgb(0xc1, 0xa7, 0))),
            ("оплачен", new SolidColorBrush(Color.FromRgb(0x28, 0xbb, 0x1d))),
            ("в доставке", new SolidColorBrush(Color.FromRgb(0x66, 0x8b, 0xd4))),
            ("отменен", new SolidColorBrush(Color.FromRgb(0xf4, 0x43, 0x36))),
            ("выполнен", new SolidColorBrush(Color.FromRgb(0x66, 0xb9, 0x17))),
            ("в обработке", new SolidColorBrush(Color.FromRgb(0xca, 0x5f, 0x09)))
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int val = -1;
            if(value is string sval)
            {
                if(!int.TryParse(sval, out val))
                {
                    return null;
                }
            }
            else if(value is int)
            {
                val = (int)value;
            }
            else
            {
                return null;
            }

            if(targetType == typeof(System.Windows.Media.Brush))
            {
                return statuses[val].Brush;
            }
            return statuses[val].Text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
