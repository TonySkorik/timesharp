using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TimesharpUI {
	class BoolToColorConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			bool? val = (bool?) value;
			if (val.HasValue) {
				if (val.Value) {
					return Brushes.LawnGreen;
				} else {
					return Brushes.OrangeRed;
				}
			}
			return Brushes.White;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			if(Equals((Brush)value, Brushes.White)) {
				return null;
			}else if (Equals((Brush)value, Brushes.LawnGreen)) {
				return true;
			}
			return false;
		}
	}

	class MinDateTimeToNotFetchedConverter:IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if ((DateTime) value == DateTime.MinValue) {
				return "Not Fetched";
			}

			if ((DateTime) value == DateTime.MaxValue) {
				return "ERROR";
			}

			return ((DateTime)value).ToString("F");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	public class InvertableBooleanToVisibilityConverter : IValueConverter {
		enum Parameters {
			Normal, Inverted
		}
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var boolValue = (bool)value;
			var direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);

			if(direction == Parameters.Inverted)
				return !boolValue ? Visibility.Visible : Visibility.Collapsed;

			return boolValue ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return null;
		}
	}

	public class InvertableBooleanConverter : IValueConverter {
		enum Parameters {
			Normal, Inverted
		}
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var boolValue = (bool)value;
			var direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);

			if (direction == Parameters.Inverted)
				return !boolValue;

			return boolValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return null;
		}
	}
}
