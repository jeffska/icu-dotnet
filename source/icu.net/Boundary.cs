﻿using System;

namespace Icu
{
	/// <summary>
	/// Specifies the start and end indexes of a word, line, sentence or character.
	/// </summary>
	public struct Boundary
	{
		/// <summary>
		/// Starting index of a boundary
		/// </summary>
		public int Start;

		/// <summary>
		/// End index of a boundary
		/// </summary>
		public int End;

		/// <summary>
		/// Creates a boundary with the specified start and end. The word would lie
		/// between indices x, Start <= x < End
		/// </summary>
		public Boundary(int start, int end)
		{
			if (start > end)
			{
				throw new ArgumentException("start index cannot be greater than the end index.");
			}

			Start = start;
			End = end;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Boundary))
			{
				return false;
			}

			var compared = (Boundary)obj;

			return Start == compared.Start && End == compared.End;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("Start: [{0}], End: [{1}]", Start, End);
		}
	}
}
