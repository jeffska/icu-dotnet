// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Icu
{
	/// <summary>
	/// Bidi algorithm for ICU
	/// </summary>
	/// <remarks>
	/// This is an implementation of the Unicode Bidirectional Algorithm.The algorithm is defined in the
	/// <a href="http://www.unicode.org/unicode/reports/tr9/">Unicode Standard Annex #9</a>.
	/// </remarks>
	public sealed class BiDi : IDisposable
	{
		/// <summary>
		/// Paragraph level setting.
		/// 
		/// Constant indicating that the base direction depends on the first strong directional character in the text
		/// according to the Unicode Bidirectional Algorithm.If no strong directional character is present, then
		/// set the paragraph level to 0 (left-to-right).
		/// 
		/// If this value is used in conjunction with reordering modes <see cref="BiDiReorderingMode.INVERSE_LIKE_DIRECT"/> or
		/// <see cref="BiDiReorderingMode.INVERSE_FOR_NUMBERS_SPECIAL"/>, the text to reorder is assumed to be visual LTR, and the text
		/// after reordering is required to be the corresponding logical string with appropriate contextual direction.
		/// The direction of the result string will be <see cref="BiDiDirection.RTL"/> if either the rightmost or leftmost strong character of the
		/// source text is RTL or Arabic Letter, the direction will be <see cref="BiDiDirection.LTR"/> otherwise.
		/// 
		/// If reordering option <see cref="BiDiReorderingOption.INSERT_MARKS"/> is set, an RLM may be added at the beginning of the result
		/// string to ensure round trip (that the result string, when reordered back to visual, will produce the original
		/// source text).
		/// </summary>
		public const byte DEFAULT_LTR = 0xfe;

		/// <summary>
		/// Paragraph level setting.
		/// 
		/// Constant indicating that the base direction depends on the first strong directional character in the text according
		/// to the Unicode Bidirectional Algorithm.If no strong directional character is present, then set the paragraph
		/// level to 1 (right-to-left).
		/// 
		/// If this value is used in conjunction with reordering modes <see cref="BiDiReorderingMode.INVERSE_LIKE_DIRECT"/> or
		/// <see cref="BiDiReorderingMode.INVERSE_FOR_NUMBERS_SPECIAL"/>, the text to reorder is assumed to be visual LTR, and the text
		/// after reordering is required to be the corresponding logical string with appropriate contextual direction.
		/// The direction of the result string will be <see cref="BiDiDirection.RTL"/>  if either the rightmost or leftmost strong character of the
		/// source text is RTL or Arabic Letter, or if the text contains no strong character; the direction will be <see cref="BiDiDirection.LTR"/> otherwise.
		/// 
		/// If reordering option <see cref="BiDiReorderingOption.INSERT_MARKS"/> is set, an RLM may be added at the beginning of the result string to
		/// ensure round trip (that the result string, when reordered back to visual, will produce the original source text).
		/// </summary>
		public const byte DEFAULT_RTL = 0xff;

		/// <summary>
		/// Maximum explicit embedding level.
		/// 
		/// Same as the max_depth value in the Unicode Bidirectional Algorithm. (The maximum resolved level can be up to MAX_EXPLICIT_LEVEL+1).
		/// </summary>
		public const int MAX_EXPLICIT_LEVEL = 125;

		/// <summary>
		/// Bit flag for level input.
		/// 
		/// Overrides directional properties.
		/// </summary>
		public const byte LEVEL_OVERRIDE = 0x80;

		/// <summary>
		/// Special value which can be returned by the mapping functions when a logical index has no corresponding visual index or vice-versa.
		/// 
		/// This may happen for the logical-to-visual mapping of a Bidi control when option <see cref="BiDiReorderingOption.REMOVE_CONTROLS"/> is specified.
		/// This can also happen for the visual-to-logical mapping of a Bidi mark(LRM or RLM) inserted by option <see cref="BiDiReorderingOption.INSERT_MARKS"/>.
		/// </summary>
		public const int MAP_NOWHERE = -1;


		/// <summary>
		/// Option bit for <see cref="GetReordered(ushort)"/>: keep combining characters after their base characters in RTL runs.
		/// </summary>
		public const ushort KEEP_BASE_COMBINING = 1;

		/// <summary>
		/// Option bit for <see cref="GetReordered(ushort)"/>: replace characters with the "mirrored" property in RTL runs by their mirror-image mappings.
		/// </summary>
		public const ushort DO_MIRRORING = 2;

		/// <summary>
		/// Option bit for <see cref="GetReordered(ushort)"/>: surround the run with LRMs if necessary; this is part of the approximate "inverse Bidi" algorithm.
		/// </summary>
		public const ushort INSERT_LRM_FOR_NUMERIC = 4;

		/// <summary>
		/// Option bit for <see cref="GetReordered(ushort)"/>: remove Bidi control characters (this does not affect <see cref="INSERT_LRM_FOR_NUMERIC"/>).
		/// </summary>
		public const ushort REMOVE_BIDI_CONTROLS = 8;

		/// <summary>
		/// Option bit for <see cref="GetReordered(ushort)"/>: write the output in reverse order.
		/// </summary>
		public const ushort OUTPUT_REVERSE = 16;


		/// <summary>
		/// BiDiDirection values indicate the text direction.
		/// </summary>
		public enum BiDiDirection
		{
			LTR,
			RTL,
			MIXED,
			NEUTRAL
		};

		/// <summary>
		/// BiDiReorderingMode values indicate which variant of the Bidi algorithm to use.
		/// </summary>
		public enum BiDiReorderingMode
		{
			DEFAULT = 0,
			NUMBERS_SPECIAL,
			GROUP_NUMBERS_WITH_R,
			RUNS_ONLY,
			INVERSE_NUMBERS_AS_L,
			INVERSE_LIKE_DIRECT,
			INVERSE_FOR_NUMBERS_SPECIAL
		};

		/// <summary>
		/// BiDiReorderingOption values indicate which options are specified to affect the Bidi algorithm.
		/// </summary>
		[Flags]
		public enum BiDiReorderingOption
		{
			DEFAULT = 0,
			INSERT_MARKS = 1,
			REMOVE_CONTROLS = 2,
			STREAMING = 4
		}


		private IntPtr _biDi = IntPtr.Zero;
		private IntPtr _para = IntPtr.Zero;
		private bool _disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Creates a new empty BiDi object.
		/// </summary>
		public BiDi()
		{
			_biDi = NativeMethods.ubidi_open();
			if (_biDi == IntPtr.Zero)
				throw new Exception("Creating BiDi object failed!");
		}

		/// <summary>
		/// Creates a new BiDi object with preallocated memory for internal structures.
		/// </summary>
		/// <param name="maxLength">The maximum text or line length that internal memory will be preallocated for</param>
		/// <param name="maxRunCount">The maximum anticipated number of same-level runs that internal memory will be preallocated for</param>
		public BiDi(int maxLength, int maxRunCount)
		{
			ErrorCode errorCode;
			_biDi = NativeMethods.ubidi_openSized(maxLength, maxRunCount, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Creating BiDi object failed! " + errorCode);
		}

		/// <summary>
		/// Private constructor.
		/// </summary>
		/// <param name="bidiPtr"></param>
		private BiDi(IntPtr bidiPtr)
		{
			_biDi = bidiPtr;
		}

		/// <summary>
		/// Implementing IDisposable pattern to properly release unmanaged resources.
		/// See https://msdn.microsoft.com/en-us/library/b1yfkh5e(v=vs.110).aspx
		/// and https://msdn.microsoft.com/en-us/library/b1yfkh5e(v=vs.100).aspx
		/// for more information.
		/// </summary>
		/// <param name="disposing"></param>
		void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				if (_biDi != IntPtr.Zero)
				{
					NativeMethods.ubidi_close(_biDi);
					_biDi = IntPtr.Zero;
				}

				if (_para != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(_para);
					_para = IntPtr.Zero;
				}

				_disposedValue = true;
			}
		}

		/// <summary>
		/// Disposes of all unmanaged resources used by BiDi
		/// </summary>
		~BiDi()
		{
			Dispose(false);
		}

		/// <summary>
		/// Dispose of managed/unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets or sets the reordering mode for the BiDi object.
		/// </summary>
		/// <remarks>
		/// This property must be set before <see cref="SetPara(string, byte, byte[])"/>, and stays in effect until called again with a different argument.
		/// </remarks>
		public BiDiReorderingMode ReorderingMode
		{
			get
			{
				return NativeMethods.ubidi_getReorderingMode(_biDi);
			}

			set
			{
				NativeMethods.ubidi_setReorderingMode(_biDi, value);
			}
		}

		/// <summary>
		/// Gets or sets the reordering options for the BiDi object.
		/// </summary>
		public BiDiReorderingOption ReorderingOptions
		{
			get
			{
				return NativeMethods.ubidi_getReorderingOptions(_biDi);
			}

			set
			{
				NativeMethods.ubidi_setReorderingOptions(_biDi, value);
			}
		}

		/// <summary>
		/// Modify the operation of the Bidi algorithm such that it approximates an "inverse Bidi" algorithm.
		/// </summary>
		public bool IsInverse
		{
			get
			{
				return NativeMethods.ubidi_isInverse(_biDi);
			}

			set
			{
				NativeMethods.ubidi_setInverse(_biDi, value);
			}
		}

		/// <summary>
		/// Specify whether block separators must be allocated level zero, so that successive paragraphs will progress from left to right.
		/// </summary>
		public bool OrderParagraphsLTR
		{
			get
			{
				return NativeMethods.ubidi_isOrderParagraphsLTR(_biDi);
			}

			set
			{
				NativeMethods.ubidi_orderParagraphsLTR(_biDi, value);
			}
		}

		/// <summary>
		/// Get the directionality of the text.
		/// </summary>
		public BiDiDirection Direction
		{
			get
			{
				return NativeMethods.ubidi_getDirection(_biDi);
			}
		}

		/// <summary>
		/// Perform the Unicode Bidi algorithm.
		/// </summary>
		/// <param name="text">The text that the Bidi algorithm will be performed on</param>
		/// <param name="paraLevel">Specifies the default level for the text; it is typically 0 (LTR) or 1 (RTL)</param>
		/// <param name="embeddingLevels">May be used to preset the embedding and override levels, ignoring characters like LRE and PDF in the text;
		/// a level overrides the directional property of its corresponding (same index) character if the level has the UBIDI_LEVEL_OVERRIDE bit set</param>
		/// <remarks>
		/// This function takes a piece of plain text containing one or more paragraphs, with or without externally specified embedding levels from styled
		/// text and computes the left-right-directionality of each character.
		/// 
		/// If the entire text is all of the same directionality, then the function may not perform all the steps described by the algorithm, i.e.,
		/// some levels may not be the same as if all steps were performed.This is not relevant for unidirectional text.
		/// 
		/// For example, in pure LTR text with numbers the numbers would get a resolved level of 2 higher than the surrounding text according to the
		/// algorithm.This implementation may set all resolved levels to the same value in such a case.
		/// 
		/// The text can be composed of multiple paragraphs. Occurrence of a block separator in the text terminates a paragraph, and whatever comes
		/// next starts a new paragraph.The exception to this rule is when a Carriage Return (CR) is followed by a Line Feed (LF). Both CR and LF are
		/// block separators, but in that case, the pair of characters is considered as terminating the preceding paragraph, and a new paragraph will
		/// be started by a character coming after the LF.
		/// </remarks>
		public void SetPara(string text, byte paraLevel, byte[] embeddingLevels)
		{
			ErrorCode errorCode;

			if (_para != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_para);
				_para = IntPtr.Zero;
			}

			// icu BiDi expects the para pointer to live for the life of the structure, so we have to stash it
			_para = Marshal.StringToHGlobalUni(text);
			NativeMethods.ubidi_setPara(_biDi, _para, text.Length, paraLevel, embeddingLevels, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi analysis failed! " + errorCode);
		}

		/// <summary>
		/// Sets a BiDi object to contain the reordering information, especially the resolved levels, for all the characters in a line of text.
		/// </summary>
		/// <param name="start">The line's first index into the text</param>
		/// <param name="limit">Position just behind the line's last index into the text (its last index +1)</param>
		/// <returns></returns>
		public BiDi SetLine(int start, int limit)
		{
			ErrorCode errorCode;
			IntPtr lineBidi;
			NativeMethods.ubidi_setLine(_biDi, start, limit, out lineBidi, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi line creation failed! " + errorCode);
			return new BiDi(lineBidi);
		}

		/// <summary>
		/// Set the context before a call to <see cref="SetPara(string, byte, byte[])"/>.
		/// </summary>
		/// <param name="prologue">The text which precedes the text that will be specified in a coming call to <see cref="SetPara(string, byte, byte[])"/></param>
		/// <param name="epilogue">The text which follows the text that will be specified in a coming call to <see cref="SetPara(string, byte, byte[])"/></param>
		public void SetContext(string prologue, string epilogue)
		{
			ErrorCode errorCode;
			NativeMethods.ubidi_setContext(_biDi, prologue, prologue?.Length ?? 0, epilogue, epilogue?.Length ?? 0, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi setting context failed! " + errorCode);
		}

		/// <summary>
		/// Get the text.
		/// </summary>
		public string Text
		{
			get
			{
				return NativeMethods.ubidi_getText(_biDi);
			}
		}

		/// <summary>
		/// Get the length of the text.
		/// </summary>
		public int Length
		{
			get
			{
				return NativeMethods.ubidi_getLength(_biDi);
			}
		}

		/// <summary>
		/// Get the length of the source text processed by the last call to <see cref="SetPara(string, byte, byte[])"/>.
		/// </summary>
		public int ProcessedLength
		{
			get
			{
				return NativeMethods.ubidi_getProcessedLength(_biDi);
			}
		}

		/// <summary>
		/// Get the length of the reordered text resulting from the last call to <see cref="SetPara(string, byte, byte[])"/>.
		/// </summary>
		public int ResultLength
		{
			get
			{
				return NativeMethods.ubidi_getResultLength(_biDi);
			}
		}

		/// <summary>
		/// Get an enumeration of levels for each character.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<byte> GetLevels()
		{
			ErrorCode errorCode;
			var levels = NativeMethods.ubidi_getLevels(_biDi, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi level retrieval failed! " + errorCode);

			var ret = new byte[ProcessedLength];
			Marshal.Copy(levels, ret, 0, ret.Length);
			return ret;
		}

		/// <summary>
		/// Get the level for one character.
		/// </summary>
		/// <param name="index">The index of a character. It must be in the range [0..<see cref="ProcessedLength"/>]</param>
		/// <returns></returns>
		public byte GetLevelAt(int index)
		{
			return NativeMethods.ubidi_getLevelAt(_biDi, index);
		}

		/// <summary>
		/// Get the paragraph level of the text.
		/// </summary>
		/// <returns>The paragraph level</returns>
		public byte GetParaLevel()
		{
			return NativeMethods.ubidi_getParaLevel(_biDi);
		}

		/// <summary>
		/// Get the number of paragraphs.
		/// </summary>
		/// <returns>The number of paragraphs</returns>
		public int CountParagraphs()
		{
			return NativeMethods.ubidi_countParagraphs(_biDi);
		}

		/// <summary>
		/// Get a paragraph, given a position within the text.
		/// </summary>
		/// <param name="charIndex">The index of a character within the text, in the range [0..<see cref="ProcessedLength"/>-1]</param>
		/// <param name="paraStart">Will receive the index of the first character of the paragraph in the text</param>
		/// <param name="paraLimit">Will receive the limit of the paragraph</param>
		/// <param name="paraLevel">Will receive the level of the paragraph</param>
		/// <returns>The index of the paragraph containing the specified position</returns>
		public int GetParagraph(int charIndex, out int paraStart, out int paraLimit, out byte paraLevel)
		{
			ErrorCode errorCode;
			var ret = NativeMethods.ubidi_getParagraph(_biDi, charIndex, out paraStart, out paraLimit, out paraLevel, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Paragraph retrieval failed! " + errorCode);
			return ret;
		}

		/// <summary>
		/// Get a paragraph, given the index of this paragraph.
		/// </summary>
		/// <param name="paraIndex">The number of the paragraph, in the range [0..<see cref="CountParagraphs"/>-1]</param>
		/// <param name="paraStart">Will receive the index of the first character of the paragraph in the text</param>
		/// <param name="paraLimit">Will receive the limit of the paragraph</param>
		/// <param name="paraLevel">Will receive the level of the paragraph</param>
		public void GetParagraphByIndex(int paraIndex, out int paraStart, out int paraLimit, out byte paraLevel)
		{
			ErrorCode errorCode;
			NativeMethods.ubidi_getParagraphByIndex(_biDi, paraIndex, out paraStart, out paraLimit, out paraLevel, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Paragraph retrieval failed! " + errorCode);
		}

		/// <summary>
		/// Get the number of runs.
		/// </summary>
		/// <returns>The number of runs</returns>
		public int CountRuns()
		{
			ErrorCode errorCode;
			var ret = NativeMethods.ubidi_countRuns(_biDi, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Run count failed! " + errorCode);
			return ret;
		}

		/// <summary>
		/// Get the logical text position from a visual position.
		/// </summary>
		/// <param name="visualIndex">The visual position of a character</param>
		/// <returns>The index of this character in the text</returns>
		public int GetLogicalIndex(int visualIndex)
		{
			ErrorCode errorCode;
			var ret = NativeMethods.ubidi_getLogicalIndex(_biDi, visualIndex, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Get logical index failed! " + errorCode);
			return ret;
		}

		/// <summary>
		/// Get a logical-to-visual index map (array) for the characters in the BiDi (paragraph or line) object.
		/// </summary>
		/// <returns>An array of <see cref="ProcessedLength"/>) indexes which will reflect the reordering of the characters</returns>
		public int[] GetLogicalMap()
		{
			ErrorCode errorCode;
			var map = new int[ResultLength];
			NativeMethods.ubidi_getLogicalMap(_biDi, map, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Get logical map failed! " + errorCode);
			return map;
		}

		/// <summary>
		/// Get a logical run.
		/// </summary>
		/// <param name="logicalPosition">A logical position within the source text</param>
		/// <param name="runLevel">Will receive the level of the corresponding run</param>
		/// <returns>The limit of the corresponding run</returns>
		public int GetLogicalRun(int logicalPosition, out byte runLevel)
		{
			NativeMethods.ubidi_getLogicalRun(_biDi, logicalPosition, out int limit, out runLevel);
			return limit;
		}

		/// <summary>
		/// Get the visual position from a logical text position.
		/// </summary>
		/// <param name="logicalIndex">The index of a character in the tex</param>
		/// <returns>The visual position of this character.</returns>
		public int GetVisualIndex(int logicalIndex)
		{
			ErrorCode errorCode;
			var ret = NativeMethods.ubidi_getVisualIndex(_biDi, logicalIndex, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Get visual index failed! " + errorCode);
			return ret;
		}

		/// <summary>
		/// Get a visual-to-logical index map (array) for the characters in the BiDi (paragraph or line) object.
		/// </summary>
		/// <returns>An array of <see cref="ResultLength"/>indexes which will reflect the reordering of the character</returns>
		public int[] GetVisualMap()
		{
			ErrorCode errorCode;
			var map = new int[ResultLength];
			NativeMethods.ubidi_getVisualMap(_biDi, map, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("Get visual map failed! " + errorCode);
			return map;
		}

		/// <summary>
		/// Get one run's logical start, length, and directionality, which can be 0 for LTR or 1 for RTL.
		/// </summary>
		/// <param name="runIndex">The number of the run in visual order, in the range [0..<see cref="CountRuns"/>-1]</param>
		/// <param name="logicalStart">Will receive the first logical character index in the text</param>
		/// <param name="runLength">Will receive the number of characters (at least one) in the run</param>
		/// <returns>The directionality of the run, never UBIDI_MIXED, never UBIDI_NEUTRAL</returns>
		public BiDiDirection GetVisualRun(int runIndex, out int logicalStart, out int runLength)
		{
			return NativeMethods.ubidi_getVisualRun(_biDi, runIndex, out logicalStart, out runLength);
		}

		/// <summary>
		/// Returns a reordered string for a piece of text (one or more paragraphs) set by <see cref="SetPara(string, byte, byte[])"/> or
		/// for a line of text set by <see cref="SetLine(int, int)"/>.
		/// </summary>
		/// <param name="options">Options for the reordering that control how the reordered text is written.</param>
		/// <returns>The reordered string</returns>
		public string GetReordered(ushort options)
		{
			ErrorCode errorCode;
			var buff = new char[ProcessedLength * 2];
			var len = NativeMethods.ubidi_writeReordered(_biDi, buff, buff.Length * 2, options, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi reordering failed! " + errorCode);

			return new string(buff, 0, len);
		}

		/// <summary>
		/// Gets the base direction of the text provided according to the Unicode Bidirectional Algorithm.
		/// </summary>
		/// <param name="str">The text whose base direction is needed</param>
		/// <returns>The base direction of the text</returns>
		public static BiDiDirection GetBaseDirection(string str)
		{
			return NativeMethods.ubidi_getBaseDirection(str, str.Length);
		}

		/// <summary>
		/// Reverse a Right-To-Left run of Unicode text.
		/// </summary>
		/// <param name="str">The RTL text.</param>
		/// <param name="options">Options for the reordering that control how the reordered text is written.</param>
		/// <returns>The reversed string</returns>
		public static string ReverseString(string str, ushort options)
		{
			if (str == null)
				return "";

			ErrorCode errorCode;
			var buff = new char[str.Length];
			var len = NativeMethods.ubidi_writeReverse(str, str.Length, buff, buff.Length, options, out errorCode);
			if (errorCode.IsFailure())
				throw new Exception("BiDi reversing failed! " + errorCode);

			return new string(buff, 0, len);
		}
	}
}
