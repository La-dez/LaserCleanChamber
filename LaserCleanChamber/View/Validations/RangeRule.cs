using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LaserCleanChamber.View.Validations
{
    public class RangeRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (double.TryParse(value?.ToString(), out double val))
            {
                if (val < Min || val > Max)
                    return new ValidationResult(false, $"Диапазон: {Min}-{Max}");
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, "Не число");
        }
    }
}
