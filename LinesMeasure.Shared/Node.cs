using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinesMeasure
{
	public class Node : INotifyPropertyChanged, IComparable<Node>
	{

		public event PropertyChangedEventHandler PropertyChanged;
		protected void PC ( string name ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( name ) ); }

		public string Name { get; private set; }
		public Node Self => this;
		public Node ParentNode { get; private set; }
		public IList<Node> SubNodes { get; private set; } = new ObservableCollection<Node> ();
		public virtual bool? IsChecked
		{
			get
			{
				if ( SubNodes.Count == 0 ) return false;

				int trueNodes = 0, falseNodes = 0;
				foreach ( var node in SubNodes )
				{
					bool? sub = node.IsChecked;
					if ( sub == true )
						++trueNodes;
					else if ( sub == false )
						++falseNodes;
				}

				if ( trueNodes == SubNodes.Count )
					return true;
				else if ( falseNodes == SubNodes.Count )
					return false;
				else
					return null;
			}
			set
			{
				SetIsCheckedChildren ( value );
				//SetIsCheckedParent ();
			}
		}

		private void SetIsCheckedChildren ( bool? value )
		{
			foreach ( var node in SubNodes )
				node.IsChecked = value;
			PC ( nameof ( IsChecked ) );
		}

		protected void SetIsCheckedParent ()
		{
			Node node = this;
			while ( node != null )
			{
				node?.PC ( nameof ( IsChecked ) );
				node = node.ParentNode;
			}
		}

		public virtual int Lines
		{
			get
			{
				int total = 0;
				foreach ( Node n in SubNodes )
					total += ( n.IsChecked != false ) ? n.Lines : 0;
				return total;
			}
		}

		protected void SetLinesParent ()
		{
			Node node = this;
			while ( node != null )
			{
				node?.PC ( nameof ( Lines ) );
				node = node.ParentNode;
			}
		}
		public Node ( string name )
		{
			Name = name;
		}

		public override string ToString () => Name;

		public int CompareTo ( Node other )
		{
			if ( other == null || ( !( this is FileNode ) && other is FileNode ) )
				return -1;
			else if ( ( this is FileNode ) && !( other is FileNode ) )
				return 1;
			return Name.CompareTo ( other.Name );
		}

		private static void Sort ( Node node )
		{
			List<Node> emptyNode = new List<Node> ();
			foreach ( Node n in node.SubNodes )
			{
				Sort ( n );
				if ( !( n is FileNode ) && n.SubNodes.Count == 0 )
					emptyNode.Add ( n );
			}

			foreach ( Node n in emptyNode )
				node.SubNodes.Remove ( n );

			if ( node.SubNodes.Count != 0 )
				Quicksort.Sort ( node.SubNodes );
		}

		private static readonly string [] IgnoreDirectories = {
			".vs", ".vscode",																//< Visual Studio, Visual Studio Code
			".idea", ".idea_modules",														//< IntelliJ
			".git", ".svn",																	//< Source Version Control
			"bin", "obj", "res", "build",													//< Common Binary, Object, Resources directories
		};

		private static readonly string [] DefinitlyIgnoreFileExtensions = {
			".o", ".a", ".so", ".exe", ".dll", ".lib", ".jar", ".aar", ".srcaar", ".appx",
				".class", ".msi",															//< Executable Files
			".zip", ".rar", ".zipx", ".gz", ".z", ".lzh", ".tar", ".br", ".alz", ".egg",
				".pak",																		//< Archive Files or Compression Files
			".jpg", ".jpeg", ".png", ".tif", ".tiff", ".jp2", ".hdp", ".jxr", ".gif",
				".webp", ".raw", ".heif", ".avif", ".ai", ".psd", ".pdn", ".wdp", "bmp",
				".dib", ".exr", ".svg", ".wmf", ".vml", ".ico", ".cur", ".pcx", ".tga",
				".dds", ".astc", ".ktx", ".pkm",											//< Image Files
			".ogg", ".opus", ".mp3", ".m4a", ".mka", ".aac", ".flac", ".alac", ".wav",
				".oga",																		//< Audio Files
			".mp4", ".mkv", ".m4v", ".ts", ".avi", ".mov", ".swf", ".flv", ".qt", ".wmv",
				".rm", ".rmvb", ".3gp", ".amv", ".svi", ".nsv", ".asf", ".m4p", ".mpg",
				".mpeg", ".m2v", ".webm", ".ogv",											//< Video Files
			".doc", ".ppt", ".xls", ".docx", ".pptx", ".xlsx", ".odt", ".pdf", ".sxw",
				".sxc", ".sxd", ".sxi", ".sxm", ".sxg", ".stw", ".epub", ".hwp",			//< Document Files
			".sha1", ".md5", ".sha2",														//< Hash Files
			".ttf", ".otf",																	//< Font Files
			"", ".meta", ".info", ".bin", ".mdb", ".pdb", ".bson", ".pom", ".assets",
				".anim", ".controller", ".unity", ".resource", ".prefab",					//< etc
		};

		public static Node FilesToNode ( IEnumerable<string> pathes )
		{
			SpinLock spinLock = new SpinLock ();

			Node node = new Node ( null );
			Parallel.ForEach ( pathes, ( path ) =>
			{
				var split = path.Split ( '\\' );
				Node root = node;
				foreach ( string s in split )
				{
					if ( IgnoreDirectories.Contains ( s.ToLower () ) )
						return;

					bool proceed = false;

					bool lockTaken = false;
					do
					{
						spinLock.Enter ( ref lockTaken );
					} while ( !lockTaken );
					foreach ( Node n in root.SubNodes )
					{
						if ( n.Name == s )
						{
							root = n;
							proceed = true;
						}
					}

					if ( !proceed )
					{
						Node n = ( s == split [ split.Length - 1 ] && !File.GetAttributes ( path ).HasFlag ( FileAttributes.Directory ) )
							? new FileNode ( s, path )
							: new Node ( s );
						n.ParentNode = root;

						if ( n is FileNode && DefinitlyIgnoreFileExtensions.Contains ( Path.GetExtension ( path ).ToLower () ) )
						{
							spinLock.Exit ();
							continue;
						}

						root.SubNodes.Add ( n );
						root = n;
					}
					spinLock.Exit ();
				}
			} );

			while ( node.SubNodes.Count == 1 )
			{
				node = node.SubNodes [ 0 ];
				node.ParentNode = null;
			}
			Sort ( node );

			return node;
		}
	}

	public class FileNode : Node
	{
		private static readonly string [] SourceCodeExtensions = {
			".cs",																			//< C#
			".fs", ".cls", ".bas", ".vb", ".vbs", ".qs",									//< F#, Visual Basic, Visual Basic.NET, Q#
			".asp", ".aspx", ".asx", ".asmx",												//< ASP/ASP.NET
			".java", ".kt",																	//< Java, Kotlin
			".jsp", ".jspx", ".do",															//< JSP
			".html", ".htm", ".xhtml", ".shtml",											//< HTML
			".css",																			//< CSS
			".js", ".ts",																	//< Javascript, Typescript
			".php", ".php3", ".phps",														//< PHP
			".cgi",																			//< C/Perl CGI
			".cpp", ".h", ".hxx", ".hpp", ".c", ".hh", ".cc", ".cp", ".cxx", ".ixx", ".inl",//< C/C++
			".asm", ".s",																	//< Assembly
			".m", ".mm",																	//< Objective-C or MATLAB
			".swift",																		//< Swift
			".py", "pyx",																	//< Python
			".rs",																			//< Rust
			".lua",																			//< Lua
			".pl",																			//< Perl
			".go",																			//< Go
			".rb", ".rhtml", ".erb", ".rjs",												//< Ruby
			//".xml", ".json", ".yml", ".yaml", ".cson",										//< etc Markup Languages
			".coffee", "._coffee", ".cake", ".cjsx", ".iced",								//< CoffeeScript
			".erl", ".yaws",																//< Erlang
			".bash", ".sh", ".zsh", ".ps", ".ps1", ".bat", ".csh",							//< Shell Scripts
			".pas", ".dpr", ".lpr",															//< Pascal
			".lisp", ".lsp", ".mnl",														//< LISP
			".hlsl", ".glsl", ".fx", ".shaderlab", ".metal", ".vert", ".frag", ".tesc",
				".tese", ".geom", ".comp",													//< Shader Languages
			".hs",																			//< Haskell
			".gs",																			//< Google Apps Script
			".groovy",																		//< Groovy
			".ex", ".exs",																	//< Elixir
			".scala", ".sc",																//< Scala
			".sml",																			//< Standard ML
			".ml", ".mli",																	//< Ocaml
			".scm",																			//< Scheme
			".dart",																		//< Dart
			".hx",																			//< Haxe
			".r",																			//< R
			".sql",																			//< SQL
			".as",																			//< ActionScript
			".clj", ".edn",																	//< Clojure
			".nut",																			//< Squirrel
		};

		public string FullPath { get; private set; }

		bool? isChecked = false;
		public override bool? IsChecked
		{
			get { return isChecked; }
			set
			{
				isChecked = value;
				if ( value == true && lines == 0 )
				{
					if ( FileLineCountHelper.IsText ( FullPath, 4096 ) )
					{
						lines = FileLineCountHelper.GetFileLineCount ( FullPath );
						PC ( nameof ( Self ) );
					}
				}
				PC ( nameof ( IsChecked ) );
				SetIsCheckedParent ();
				SetLinesParent ();
			}
		}

		int lines = 0;
		public override int Lines => lines;
		public FileNode ( string name, string path )
			: base ( name )
		{
			FullPath = path;
			string ext = Path.GetExtension ( FullPath ).ToLower ();
			var filename = Path.GetFileName ( FullPath );
			if ( SourceCodeExtensions.Contains ( ext ) && ( filename != "AssemblyInfo.cs" && filename != "Resources.Designer.cs" ) )
			{
				try
				{
					IsChecked = true;
					lines = FileLineCountHelper.GetFileLineCount ( FullPath );
				}
				catch
				{
					IsChecked = false;
					lines = 0;
				}
			}
		}

		public override string ToString () => $"{Name} ({FullPath})";
	}
}
