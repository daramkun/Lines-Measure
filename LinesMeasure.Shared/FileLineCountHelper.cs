using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinesMeasure
{
	public static class FileLineCountHelper
	{
		public static int GetFileLineCount ( string filename )
		{
			byte [] buffer = new byte [ 4096 ];
			int count = 1;
			using ( Stream stream = File.Open ( filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete ) )
			{
				while ( stream.Position != stream.Length )
				{
					int read = stream.Read ( buffer, 0, 4096 );
					for ( int i = 0; i < read; ++i )
					{
						byte data = buffer [ i ];
						if ( data == ( byte ) '\n' )
							++count;
					}
				}
			}
			return count;
		}

		// Original Source code from http://stackoverflow.com/a/6613967
		public static bool IsText ( string fileName, int windowSize )
		{
			try
			{
				using ( var fileStream = File.OpenRead ( fileName ) )
				{
					var rawData = new byte [ windowSize ];
					var text = new char [ windowSize ];
					var isText = true;

					var rawLength = fileStream.Read ( rawData, 0, rawData.Length );
					fileStream.Seek ( 0, SeekOrigin.Begin );

					Encoding encoding;

					if ( rawData [ 0 ] == 0xef && rawData [ 1 ] == 0xbb && rawData [ 2 ] == 0xbf )
						encoding = Encoding.UTF8;
					else if ( rawData [ 0 ] == 0xfe && rawData [ 1 ] == 0xff )
						encoding = Encoding.Unicode;
					else if ( rawData [ 0 ] == 0 && rawData [ 1 ] == 0 && rawData [ 2 ] == 0xfe && rawData [ 3 ] == 0xff )
						encoding = Encoding.UTF32;
					else if ( rawData [ 0 ] == 0x2b && rawData [ 1 ] == 0x2f && rawData [ 2 ] == 0x76 )
						encoding = Encoding.UTF7;
					else
						encoding = Encoding.Default;

					using ( var streamReader = new StreamReader ( fileStream ) )
						streamReader.Read ( text, 0, text.Length );

					using ( var memoryStream = new MemoryStream () )
					{
						using ( var streamWriter = new StreamWriter ( memoryStream, encoding ) )
						{
							streamWriter.Write ( text );
							streamWriter.Flush ();

							var memoryBuffer = memoryStream.GetBuffer ();
							for ( var i = 0; i < rawLength && isText; i++ )
								isText = rawData [ i ] == memoryBuffer [ i ];
						}
					}

					return isText;
				}
			}
			catch { return false; }
		}
	}
}
