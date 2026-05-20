using LaserCleanChamber.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LaserCleanChamber.View.Converters
{
    public class SideNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TracingAlgorithm algorithm)
            {
                switch(algorithm)
                {
                    case TracingAlgorithm.Snake: return "Змейка 1";
                    case TracingAlgorithm.SnakeModif: return "Змейка 2";
                    default:
                        return algorithm.ToString();
                }
            }
            else if (value is PlateSides placeSide)
            {
                switch (placeSide)
                {
                    case PlateSides.Top: return "Верх";
                    case PlateSides.Bottom: return "Низ";
                    default:
                        return placeSide.ToString();
                }
            }

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
