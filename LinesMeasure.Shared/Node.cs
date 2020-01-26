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
			foreach ( Node n in node.SubNodes )
				Sort ( n );

			if ( node.SubNodes.Count != 0 )
				Quicksort.Sort ( node.SubNodes );
		}

		private static readonly string [] IgnoreDirectories = {
			".vs", ".vscode",																//< Visual Studio, Visual Studio Code
			".idea", ".idea_modules",														//< IntelliJ
			".git", ".svn",																	//< Source Version Control
			"bin", "obj", "res",															//< Common Binary, Object, Resources directories
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
			".xml",																			//< XML
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
			string ext = Path.GetExtension ( FullPath );
			if ( SourceCodeExtensions.Contains ( ext ) && Path.GetFileName ( FullPath ) != "AssemblyInfo.cs" )
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
