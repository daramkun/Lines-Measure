using Daramee.Winston.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
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
			if ( !Environment.Is64BitProcess )
				Title += " (32-bit)";

			TotalLines.DataContext = currentNode;
		}

		private async void ButtonBrowse_Click ( object sender, RoutedEventArgs e )
		{
			Daramee.Winston.Dialogs.OpenFolderDialog ofd = new Daramee.Winston.Dialogs.OpenFolderDialog ();
			ofd.FileName = MeasurePath.Text;
			if ( ofd.ShowDialog () == false )
				return;

			( sender as Button ).IsEnabled = FilesTree.IsEnabled = false;
			Indicator.Visibility = Visibility.Visible;

			GCLatencyMode gcOldMode = GCSettings.LatencyMode;
			RuntimeHelpers.PrepareConstrainedRegions ();

			try
			{
				GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

				await Task.Run ( () =>
				{
					var fileEnums = FilesEnumerator.EnumerateFiles ( ofd.FileName, "*", false );
					currentNode = Node.FilesToNode ( fileEnums );

					Dispatcher.BeginInvoke ( new Action ( () =>
					{
						GC.TryStartNoGCRegion ( Environment.Is64BitProcess ? ( 15 * 1024 * 1024 ) : ( 256 * 1024 * 1024 ) );
						
						FilesTree.ItemsSource = new Node [] { currentNode };
						TotalLines.DataContext = currentNode;

						if ( GCSettings.LatencyMode == GCLatencyMode.NoGCRegion )
							GC.EndNoGCRegion ();
					} ) );
				} );
			}
			finally
			{
				GCSettings.LatencyMode = gcOldMode;
			}

			Indicator.Visibility = Visibility.Hidden;
			( sender as Button ).IsEnabled = FilesTree.IsEnabled = true;

			MeasurePath.Text = ofd.FileName;
		}

		private void CheckBox_Checked ( object sender, RoutedEventArgs e )
		{
			//FilesTree.Items.Refresh ();
			//FilesTree.UpdateLayout ();
		}
	}
}
