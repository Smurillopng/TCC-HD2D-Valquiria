#define DEBUG_GET_NEXT

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Class representing the different parts of a Unity Object header.
	/// 
	/// This includes things like the Base, the foldout arrow and icons on the header toolbar
	/// at the top-right of the header.
	/// 
	/// This does NOT include asset header buttons.
	/// </summary>
	public class HeaderParts
	{
		/// <summary> List of all header parts, starting from the right. </summary>
		private readonly List<HeaderPartDrawer> parts;

		/// <summary> Get header part at index. </summary>
		/// <param name="index"> Zero-based index of the header part starting from the right. </param>
		/// <returns> The header part. </returns>
		public HeaderPartDrawer this[int index]
		{
			get
			{
				return parts[index];
			}
		}

		/// <summary> Gets the item responsible for handling the given HeaderPart. </summary>
		/// <param name="part"> The HeaderPart. </param>
		/// <returns> The indexed item. </returns>
		public HeaderPartDrawer this[HeaderPart part]
		{
			get
			{
				for(int n = parts.Count - 1; n >= 0; n--)
				{
					var test = parts[n];
					if(test == part)
					{
						return test;
					}
				}
				return null;
			}
		}

		/// <summary> Gets the number of header parts. </summary>
		/// <value> The header aprt count. </value>
		public int Count
		{
			get
			{
				return parts.Count;
			}
		}

		/// <summary> Gets the first (starting from top-left) selectable header part. </summary>
		/// <value> The first selectable header part. </value>
		public HeaderPartDrawer FirstSelectable
		{
			get
			{
				int count = parts.Count;
				for(int n = count - 1; n >= 0; n--)
				{
					var part = parts[n];
					if(part.Selectable)
					{
						return part;
					}
				}
				return null;
			}
		}

		/// <summary> Gets the last (starting from top-left) selectable header part. </summary>
		/// <value> The last selectable header part. </value>
		public HeaderPartDrawer LastSelectable
		{
			get
			{
				int count = parts.Count;
				for(int n = 0; n < count; n++)
				{
					var part = parts[n];
					if(part.Selectable)
					{
						return part;
					}
				}
				return null;
			}
		}

		/// <summary> Gets the item responsible for handling HeaderPart.Base. </summary>
		/// <value> The base drawer. </value>
		public HeaderPartDrawer Base
		{
			get
			{
				int count = parts.Count;
				for(int n = 0; n < count; n++)
				{
					var test = parts[n];
					if(test == HeaderPart.Base)
					{
						return test;
					}
				}
				return null;
			}
		}

		[CanBeNull]
		public HeaderPartDrawer GetNextSelectableHeaderPartRight([CanBeNull]HeaderPartDrawer current)
		{
			if(current == null)
			{
				#if DEV_MODE && DEBUG_GET_NEXT
				UnityEngine.Debug.Log("HeaderParts.GetNextRight(current="+StringUtils.Null+"): "+FirstSelectable+" (FirstSelectable)");
				#endif
				return FirstSelectable;
			}
			int currentIndex = parts.IndexOf(current);
			for(int n = currentIndex - 1; n >= 0; n--)
			{
				var part = parts[n];
				if(part.Selectable)
				{
					#if DEV_MODE && DEBUG_GET_NEXT
					UnityEngine.Debug.Log("HeaderParts.GetNextRight(current="+ current + "): "+ part + " (currentIndex - "+(currentIndex - n) +")");
					#endif
					return part;
				}
			}

			#if DEV_MODE && DEBUG_GET_NEXT
			UnityEngine.Debug.Log("HeaderParts.GetNextRight(current="+ current + "): "+ StringUtils.Null);
			#endif

			return null;

		}

		/// <summary>
		/// Gets the next selectable header part to the left from current header part.
		/// </summary>
		/// <param name="current"> The currently selected header part, or null if none is currently selected. </param>
		/// <returns> The next header part left. This may be null. </returns>
		[CanBeNull]
		public HeaderPartDrawer GetNextSelectableHeaderPartLeft([CanBeNull]HeaderPartDrawer current)
		{
			int count = parts.Count;
			if(current == null)
			{
				return LastSelectable;
			}
			int currentIndex = parts.IndexOf(current);
			for(int n = currentIndex + 1; n < count; n++)
			{
				var part = parts[n];
				if(part.Selectable)
				{
					return part;
				}
			}
			return null;
		}
		
		public HeaderParts(int capacity)
		{
			parts = new List<HeaderPartDrawer>(capacity);
		}

		public void Add(HeaderPartDrawer button)
		{
			parts.Add(button);
		}

		public void Insert(int index, HeaderPartDrawer button)
		{
			parts.Insert(index, button);
		}

		/// <summary>
		/// Draws buttons at the base of the header.
		/// </summary>
		public void Draw()
		{	
			for(int n = 0, count = parts.Count; n < count; n++)
			{
				parts[n].Draw();
			}
		}
		
		public void Clear()
		{
			for(int n = parts.Count - 1; n >= 0; n--)
			{
				parts[n].Dispose();
			}
			parts.Clear();
		}

		public bool Contains([NotNull]HeaderPartDrawer part)
		{
			if(part == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("HeaderParts.Contains called with null part.");
				#endif
				return false;
			}

			for(int n = parts.Count - 1; n >= 0; n--)
			{
				if(parts[n] == part)
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			var sb = StringBuilderPool.Create();
			int lastIndex = parts.Count - 1;
			for(int n = 0; n < lastIndex; n++)
			{
				sb.Append(parts[n]);
				sb.Append(',');
			}
			sb.Append(parts[lastIndex]);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
	}
}