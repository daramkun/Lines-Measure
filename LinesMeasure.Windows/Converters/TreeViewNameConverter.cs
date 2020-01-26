using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace LinesMeasure.Converters
{
	class TreeViewNameConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if ( value is FileNode )
			{
				FileNode node = value as FileNode;
				if ( node.Lines != 0 )
					return $"{node.Name} ({node.Lines})";
				else
					return node.Name;
			}
			else if ( value is Node )
				return ( value as Node ).Name;
			return null;
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
