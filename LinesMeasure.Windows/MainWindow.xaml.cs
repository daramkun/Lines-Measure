using Daramee.Winston.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LinesMeasure
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		Node currentNode = new Node ( "" );

		public MainWindow ()
		{
			InitializeComponent ();

			TotalLines.DataContext = currentNode;
		}

		private void ButtonBrowse_Click ( object sender, RoutedEventArgs e )
		{
			Daramee.Winston.Dialogs.OpenFolderDialog ofd = new Daramee.Winston.Dialogs.OpenFolderDialog ();
			ofd.FileName = MeasurePath.Text;
			if ( ofd.ShowDialog () == false )
				return;

			var fileEnums = FilesEnumerator.EnumerateFiles ( ofd.FileName, "*", false );
			currentNode = Node.FilesToNode ( fileEnums );
			FilesTree.ItemsSource = new Node [] { currentNode };
			TotalLines.DataContext = currentNode;

			MeasurePath.Text = ofd.FileName;
		}

		private void CheckBox_Checked ( object sender, RoutedEventArgs e )
		{
			//FilesTree.Items.Refresh ();
			//FilesTree.UpdateLayout ();
		}
	}
}
