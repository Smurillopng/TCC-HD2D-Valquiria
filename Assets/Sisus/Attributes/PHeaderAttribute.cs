using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like Unity's built-in HeaderAttribute but supports targeting of properties and methods in addition to fields.
	/// 
	/// Also has support for new rich text tag "{em}", which causes the tagged text to be highlighted in a color that is
	/// selected based on whether pro skin is being used or not.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class PHeaderAttribute : HeaderAttribute, ITargetableAttribute
	{
		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="header">The header text.</param>
		public PHeaderAttribute(string header) : base(header.IndexOf("<em>", StringComparison.OrdinalIgnoreCase) == -1 ? header : ReplaceEmWithColorTags(header)) { }

		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="headerLines">The lines of text for the header.</param>
		public PHeaderAttribute(params string[] headerLines) : base(FormatLines(headerLines)) { }

		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		/// <param name="fontSize">Specify the fontSize to use for the header.</param>
		public PHeaderAttribute(string header, int fontSize) : base(string.Concat("<size=", fontSize, ">", header, "</size>")) { }
		
		/// <summary> Add a header above a field, property or a method in the Inspector. </summary>
		///  <param name="fontSize"> Specify the fontSize to use for the header. If 0 the default font size is used. </param>
		/// <param name="color">Specify the font color to use for the header. For example "red" or "#ff0000ff".</param>
		public PHeaderAttribute(string header, int fontSize, string color) : base(fontSize > 0 ? string.Concat("<size=", fontSize, "><color=", color, ">", header, "</em></size>") : string.Concat("<color=", color, ">", header, "</em>")) { }

		public Target Target
		{
			get
			{
				return Target.This;
			}
		}

		private static string ReplaceEmWithColorTags(string header)
		{
			#if UNITY_EDITOR
			bool pro = UnityEditor.EditorGUIUtility.isProSkin;
			return header.Replace("</em>", "</color>").Replace("</EM>", "</COLOR>").Replace("<em>", pro ? "<color=#4ec9b0>" : "<color=#207c69>").Replace("<EM>", pro ? "<COLOR=#4ec9b0>" : "<COLOR=#207c69>");
			#else
			return header.Replace("</em>", "</color>").Replace("</EM>", "</COLOR>").Replace("<em>", "<color=#4ec9b0>").Replace("<EM>", "<COLOR=#4ec9b0>");
			#endif
		}

		private static string FormatLines(string[] headerLines)
		{
			string header = string.Join("\n\n", headerLines);
			return header.IndexOf("<em>", StringComparison.OrdinalIgnoreCase) == -1 ? header : ReplaceEmWithColorTags(header);
		}
	}
}