using System;
using System.Collections.Generic;
using System.Text;

namespace LinesMeasure
{
	public static class Quicksort
	{
		public static void Sort<T> ( IList<T> arr ) where T : IComparable<T> { Sort<T> ( arr, 0, arr.Count - 1 ); }
		private static void Sort<T> ( IList<T> arr, int left, int right ) where T : IComparable<T>
		{
			if ( right > left )
			{
				int pivot = Partition ( arr, left, right );
				Sort ( arr, left, pivot - 1 );
				Sort ( arr, pivot + 1, right );
			}
		}
		private static void Swap<T> ( IList<T> a, int i, int j ) { T t = a [ i ]; a [ i ] = a [ j ]; a [ j ] = t; }
		private static int Partition<T> ( IList<T> arr, int low, int high ) where T : IComparable<T>
		{
			int pivotPos = ( high + low ) / 2, left = low;
			T pivot = arr [ pivotPos ];
			Swap ( arr, low, pivotPos );
			for ( int i = low + 1; i <= high; i++ )
				if ( arr [ i ].CompareTo ( pivot ) < 0 )
					Swap ( arr, i, ++left );
			Swap ( arr, low, left );
			return left;
		}
	}
}
