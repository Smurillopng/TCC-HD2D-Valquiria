using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// todo: Runtime support for Power Inspector classes is no longer supported so
	/// this should me merged with EditorGUIDrawer and all code moved to DrawGUI.
	/// </summary>
	public sealed class RuntimeGUIDrawer : DrawGUI
	{
		private static int mouseDownButtonByIdPressedDown;
		private Object[] dragAndDropObjectReferences = new Object[0];
		private DragAndDropVisualMode dragAndDropVisual = DragAndDropVisualMode.None;

		public override Object[] DragAndDropObjectReferences
		{
			get
			{
				return dragAndDropObjectReferences;
			}

			set
			{
				dragAndDropObjectReferences = value;
			}
		}
		
		public override DragAndDropVisualMode DragAndDropVisualMode
		{
			get
			{
				return dragAndDropVisual;
			}

			set
			{
				dragAndDropVisual = value;
			}
		}

		public override float InspectorTitlebarHeight
		{
			get
			{
				return SingleLineHeight;
			}
		}

		public override float AssetTitlebarHeight(bool toolbarHasTwoRowsOfButtons)
		{
			if(toolbarHasTwoRowsOfButtons)
			{
				return SingleLineHeight + SingleLineHeight;
			}
			return SingleLineHeight;
		}

		public override void AssetHeader(Rect position, Object target, GUIContent label)
		{
			GUI.Label(position, GUIContent.none, InspectorPreferences.Styles.GameObjectHeaderBackground);

			var thumbnailRect = position;
			thumbnailRect.x += 6f;
			thumbnailRect.y += 6f;
			thumbnailRect.width = 32f;
			thumbnailRect.height = 32f;

			var labelRect = position;
			labelRect.x += 44f;
			labelRect.width -= 44f;
			GUI.Label(labelRect, label, InspectorPreferences.Styles.LargeLabel);
		}
		
		public override bool ComponentHeader(Rect position, bool unfolded, Object[] targets, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart)
		{
			bool clicked = false;
			Rect pos = FirstLine(position);
			AddIndentation(ref pos, 1);

			var settings = InspectorUtility.Preferences;
			var titleStyle = settings.GetStyle("title");

			if(GUI.Button(pos, targets[0].GetType().Name, titleStyle))
			{
				clicked = expandable && Event.current.button == 0;
			}

			int count = targets.Length;

			pos.width = IndentWidth;
			pos.x = position.x;

			var behaviour = targets[0] as MonoBehaviour;
			if(behaviour != null)
			{
				bool setEnabled = GUI.Toggle(pos, behaviour.enabled, "");
				if(setEnabled != behaviour.enabled && Event.current.button == 0)
				{
					for(int n = 0; n < count; n++)
					{
						behaviour = targets[n] as MonoBehaviour;
						if(behaviour != null)
						{
							behaviour.enabled = setEnabled;
						}
					}
				}
			}
			else
			{
				GUI.Label(pos, "", titleStyle);
			}

			if(expandable)
			{
				pos.x = position.x + position.width - IndentWidth;

				clicked = GUI.Button(pos, unfolded ? settings.graphics.IconUnfolded : settings.graphics.IconFolded, foldoutStyle) && Event.current.button == 0;
			}
			
			return clicked ? !unfolded : unfolded;
		}

		public override bool InspectorTitlebar(Rect position, bool unfolded, GUIContent label, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart)
		{
			bool clicked = false;
			Rect pos = FirstLine(position);
			AddIndentation(ref pos, 1);

			var settings = InspectorUtility.Preferences;
			var titleStyle = settings.GetStyle("title");

			if(GUI.Button(pos, label, titleStyle))
			{
				clicked = expandable && Event.current.button == 0;
			}

			return clicked ? !unfolded : unfolded;
		}

		public override bool Foldout(Rect position, GUIContent label, bool unfolded, GUIStyle guiStyle, Rect? highlightRect = null)
		{
			float indent = IndentLevel * IndentWidth;

			var pos = position;
			pos.width = SingleLineHeight;
			pos.height = SingleLineHeight;
			pos.x = position.x + indent - SingleLineHeight;

			var settings = InspectorUtility.Preferences;

			bool clicked = false;
			if(GUI.Button(pos, unfolded ? settings.graphics.IconUnfolded : settings.graphics.IconFolded, guiStyle))
			{
				clicked = Event.current.button == 0;
			}

			pos = position;
			pos.height = SingleLineHeight;
			pos.x = position.x + indent;
			pos.width -= indent;

			if(GUI.Button(pos, label, foldoutStyle))
			{
				clicked = Event.current.button == 0;
			}
			return clicked ? !unfolded : unfolded;
		}

		public override bool Foldout(Rect position, GUIContent label, bool unfolded, bool selected, bool mouseovered, bool unappliedChanges, Rect? highlightRect = null)
		{
			GUIStyle styleFoldout;
			if(selected)
			{
				styleFoldout = mouseovered ? foldoutStyleSelectedMouseovered : foldoutStyleSelected;
			}
			else
			{
				styleFoldout = mouseovered ? foldoutStyleMouseovered : foldoutStyle;
			}

			if(unappliedChanges)
			{
				styleFoldout.fontStyle = FontStyle.Bold;
			}
			
			unfolded = Foldout(position, label, unfolded, styleFoldout);

			styleFoldout.fontStyle = FontStyle.Normal;

			return unfolded;
		}

		public override bool Toggle(Rect position, bool value)
		{
			var remainingPos = position;
			CachedLabel.text = "";
			bool setValue = GUI.Toggle(remainingPos, value, CachedLabel);
			if(setValue != value && Event.current.button == 0)
			{
				UseEvent();
				return setValue;
			}
			return value;
		}

		public override void GameObjectHeader(Rect position, GameObject target)
		{
			var label = GUIContentPool.Temp(target.name.Length == 0 ? " " : target.name, target.transform.GetHierarchyPath());
			var pos = position;
			pos.height = EditorGUIDrawer.GameObjectTitlebarHeight(false, false);

			pos.x -= IndentWidth;
			pos.width += IndentWidth;

			GUI.Button(pos, label, InspectorPreferences.Styles.GameObjectHeaderBackground);
		}

		public override void AssetHeader(Rect position, Object target)
		{
			var label = GUIContentPool.Temp(target.name.Length == 0 ? " " : target.name);
			var pos = position;
			pos.height = AssetTitlebarHeight(false);

			pos.x -= IndentWidth;
			pos.width += IndentWidth;

			GUI.Label(pos, label, InspectorPreferences.Styles.GameObjectHeaderBackground);
		}

		/// <summary>
		/// Returns true if values were changed
		/// </summary>
		public bool FloatGridWithLetterPrefixes(Rect position, string label1, ref float value1, string label2, ref float value2, string label3, ref float value3, string label4, ref float value4)
		{
			position.height = SingleLineHeight;
			position.width = position.width / 3f - 2f;
			bool changed = false;

			CachedLabel.text = label1;
			var setValue = FloatFieldWithLetterPrefix(position, CachedLabel, value1);
			if(value1 != setValue)
			{
				value1 = setValue;
				changed = true;
			}

			position.x += position.width;
			CachedLabel.text = label2;
			setValue = FloatFieldWithLetterPrefix(position, CachedLabel, value2);
			if(value2 != setValue)
			{
				value2 = setValue;
				changed = true;
			}

			position.x += position.width;
			CachedLabel.text = label3;
			setValue = FloatFieldWithLetterPrefix(position, CachedLabel, value3);
			if(value3 != setValue)
			{
				value3 = setValue;
				changed = true;
			}

			position.x += position.width;
			CachedLabel.text = label3;
			setValue = FloatFieldWithLetterPrefix(position, CachedLabel, value4);
			if(value4 != setValue)
			{
				value4 = setValue;
				changed = true;
			}

			return changed;
		}

		public float FloatFieldWithLetterPrefix(Rect pos, GUIContent prefixLabel, float value)
		{
			var prefixPos = pos;
			prefixPos.x += 3f;
			GUI.Label(prefixPos, prefixLabel);

			int indentWas = IndentLevel;
			IndentLevel = 1;

			var result = FloatField(pos, value);

			IndentLevel = indentWas;
			return result;
		}

		public override AnimationCurve CurveField(Rect position, AnimationCurve value)
		{
			GUI.Label(position, value.ToString());
			return value;
		}

		public override Gradient GradientField(Rect position, Gradient value)
		{
			GUI.Label(position, value.ToString());
			return value;
		}

		public override Color ColorField(Rect position, Color value)
		{
			var remainingPos = position;
			remainingPos.height = SingleLineHeight;
			remainingPos.width = remainingPos.width - SingleLineHeight;
			float r = value.r;
			float g = value.g;
			float b = value.b;
			float a = value.a;
			if(FloatGridWithLetterPrefixes(remainingPos, "R", ref r, "G", ref g, "B", ref b, "A", ref a))
			{
				value.r = r;
				value.g = g;
				value.b = b;
				value.a = a;
			}

			var pixels = new Color32[]{value};
			var colorPreview = new Texture2D(1,1);
			colorPreview.SetPixels32(pixels);
			colorPreview.Apply();

			remainingPos.x = remainingPos.x + remainingPos.width;
			remainingPos.width = SingleLineHeight;
			GUI.DrawTexture(remainingPos, InspectorUtility.Preferences.graphics.IconFrame, ScaleMode.StretchToFill);
			GUI.DrawTexture(remainingPos, colorPreview, ScaleMode.StretchToFill);
			return value;
		}

		public override bool MouseDownButton(Rect position, GUIContent label)
		{
			if(GUI.RepeatButton(position, label, InspectorPreferences.Styles.Button) && Event.current.button == 0)
			{
				int id = GUIUtility.hotControl;
				if(mouseDownButtonByIdPressedDown != id)
				{
					mouseDownButtonByIdPressedDown = id;
					#if DEV_MODE
					Debug.Log("MouseDownButton pressed down: "+id);
					#endif
					return true;
				}
			}
			else if(GUIUtility.hotControl == 0)
			{
				mouseDownButtonByIdPressedDown = 0;
			}
			return false;
		}

		public override bool MouseDownButton(Rect position, GUIContent label, GUIStyle guiStyle)
		{
			bool clicked = GUI.RepeatButton(position, label, guiStyle) && Event.current.button == 0;
			return clicked;
		}

		public override void EnumPopup(Rect position, EnumDrawer popupField)
		{
			//temporary solution
			GUI.Label(position, popupField.Value.ToString());
		}

		public override void EnumFlagsPopup(Rect position, EnumDrawer popupField)
		{
			EnumPopup(position, popupField);
		}

		public override LayerMask MaskPopup(Rect position, LayerMask value)
		{
			GUI.Label(position, LayerMask.LayerToName(value));
			return value;
		}

		public override void TypePopup(Rect position, TypeDrawer popupField)
		{
			popupField.DrawControlVisuals(position, popupField.Value);
		}
		
		public override Object ObjectField(Rect position, Object target, Type objectType, bool allowSceneObjects)
		{
			string text = target == null ? "null" : StringUtils.Concat(target.name, " (", StringUtils.ToStringSansNamespace(target.GetType()), ")");
			GUI.Label(position, text, InspectorPreferences.Styles.ObjectField);
			return target;
		}

		public override Rect PrefixLabel(Rect position, GUIContent label)
		{
			var pos = position;
			float indent = LeftPadding + IndentLevel * IndentWidth; 
			pos.x += indent;
			pos.height = SingleLineHeight;
			float prefixWidth = PrefixLabelWidth;
			pos.width = prefixWidth - indent;
			GUI.Label(pos, label);
			pos = position;
			pos.width = position.width - prefixWidth;
			pos.x = position.x + prefixWidth;
			return pos;
		}

		public override void PrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges, out Rect labelRect, out Rect controlRect)
		{
			labelRect = position;
			float indent = LeftPadding + IndentLevel * IndentWidth;
			labelRect.x += indent;
			labelRect.height = SingleLineHeight;
			float prefixWidth = PrefixLabelWidth;
			labelRect.width = prefixWidth - indent;

			var theme = InspectorUtility.Preferences.theme;

			GUI.skin.label.normal.textColor = selected ? theme.PrefixSelectedText : theme.PrefixIdleText;

			GUI.Label(labelRect, label);

			GUI.skin.label.normal.textColor = theme.PrefixIdleText;

			controlRect = position;
			controlRect.width = position.width - prefixWidth;
			controlRect.x = position.x + prefixWidth;
		}

		public override Rect PrefixLabel(Rect position, GUIContent label, bool selected)
		{
			var theme = InspectorUtility.Preferences.theme;

			GUI.skin.label.normal.textColor = selected ? theme.PrefixSelectedText : theme.PrefixIdleText;

			var remainingSpace = PrefixLabel(position, label);
			
			GUI.skin.label.normal.textColor = theme.PrefixIdleText;

			return remainingSpace;
		}

		public override Rect PrefixLabel(Rect position, GUIContent label, GUIStyle guiStyle)
		{
			var pos = position;
			float indent = IndentLevel * IndentWidth; 
			pos.x += indent;
			pos.height = SingleLineHeight;
			float prefixWidth = PrefixLabelWidth;
			pos.width = prefixWidth - indent;
			GUI.Label(pos, label, guiStyle);
			pos = position;
			pos.width = position.width - prefixWidth;
			pos.x = position.x + prefixWidth;
			return pos;
		}

		/// <summary>
		/// Draws prefix label over the entire given area without any indentations
		/// </summary>
		public override void InlinedPrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges)
		{
			if(selected)
			{
				if(unappliedChanges)
				{
					InlinedSelectedModifiedPrefixLabel(position, label);
				}
				else
				{
					InlinedSelectedPrefixLabel(position, label);
				}
			}
			else if(unappliedChanges)
			{
				InlinedModifiedPrefixLabel(position, label);
			}
			else
			{
				InlinedPrefixLabel(position, label);
			}
		}

		//Draws prefix label without any indentations
		public override void InlinedPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabel);
		}

		public override void InlinedPrefixLabel(Rect position, string label)
		{
			GUI.Label(position, label, prefixLabel);
		}

		public override void InlinedMouseoveredPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabelMouseovered);
		}

		public override void InlinedMouseoveredModifiedPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabelMouseoveredModified);
		}

		public override void InlinedSelectedPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabelSelected);
		}

		public override void InlinedModifiedPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabelModified);
		}

		public override void InlinedSelectedModifiedPrefixLabel(Rect position, GUIContent label)
		{
			GUI.Label(position, label, prefixLabelSelectedModified);
		}

		/// <summary>
		/// if the label has a tooltip, draw a hint icon which shows the tooltip on mouseover
		/// </summary>
		public override void HandleHintIcon(Rect position, GUIContent label)
		{
			if(InspectorUtility.Preferences.enableTooltipIcons && label.tooltip.Length > 0)
			{
				var hintPos = position;
				hintPos.x -= SingleLineHeight;
				hintPos.width = SingleLineHeight;
				HintIcon(hintPos, label.tooltip);
			}
		}

		public override void HintIcon(Rect position, string text)
		{
			CachedLabel.text = "";
			CachedLabel.tooltip = text;
			CachedLabel.image = InspectorUtility.Preferences.graphics.tooltipIcon;
			GUI.Label(position, CachedLabel);
			CachedLabel.tooltip = "";
			CachedLabel.image = null;
		}

		public override int IntField(Rect position, int value)
		{
			var remainingPos = position;
			string valueString = value.ToString();

			string setString = GUI.TextField(remainingPos, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0;
				}
				int setValue;
				if(int.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}

		public override int IntField(Rect position, int value, bool delayed)
		{
			// to do: actually support delayed fields at runtime
			return IntField(position, value);
		}

		public override short ShortField(Rect position, short value)
		{
			var remainingPos = position;
			string valueString = value.ToString();

			string setString = GUI.TextField(remainingPos, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0;
				}
				short setValue;
				if(short.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}

		public override ushort UShortField(Rect position, ushort value)
		{
			var remainingPos = position;
			string valueString = value.ToString();

			string setString = GUI.TextField(remainingPos, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0;
				}
				ushort setValue;
				if(ushort.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}
		
		public override float Slider<TValue>(Rect position, TValue value, float min, float max)
		{
			float valueWas = (float)Convert.ChangeType(value, Types.Float);
			return Slider(position, valueWas, min, max);
		}

		public override int Slider(Rect position, int value, int min, int max)
		{
			return Mathf.Clamp(Mathf.RoundToInt(GUI.HorizontalSlider(position, value, min, max)), min, max);
		}

		public override float Slider(Rect position, float value, float min, float max)
		{
			return GUI.HorizontalSlider(position, value, min, max);
		}

		public override float FloatField(Rect position, float value)
		{
			var remainingPos = position;
			string valueString = StringUtils.ToString(value);

			string setString = GUI.TextField(remainingPos, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0f;
				}
				float setValue;
				if(float.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}

		public override float FloatField(Rect position, float value, bool delayed)
		{
			// to do: actually support delayed fields at runtime
			return FloatField(position, value);
		}

		public override double DoubleField(Rect position, double value)
		{
			string valueString = value.ToString(StringUtils.DoubleFormat);
			string setString = GUI.TextField(position, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0d;
				}
				double setValue;
				if(double.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}

		public override decimal DecimalField(Rect position, decimal value)
		{
			var remainingPos = position;
			string valueString = StringUtils.ToString(value);
			string setString = GUI.TextField(remainingPos, valueString);
			if(!string.Equals(valueString, setString))
			{
				if(string.IsNullOrEmpty(setString))
				{
					return 0m;
				}
				decimal setValue;
				if(decimal.TryParse(setString, out setValue))
				{
					return setValue;
				}
			}
			return value;
		}

		public override string TextField(Rect position, string value)
		{
			position.height = SingleLineHeight - 2f;
			position.y += 1f;
			value = GUI.TextField(position, value);
			return value;
		}

		public override string TextField(Rect position, string value, bool delayed)
		{
			// to do: actually support delayed fields at runtime
			return TextField(position, value);
		}


		public override string TextArea(Rect position, string value, bool wordWrapping)
		{
			position.height = SingleLineHeight - 2f;
			position.y += 1f;
			value = GUI.TextArea(position, value);
			return value;
		}

		public override string TextArea(Rect position, string value, GUIStyle guiStyle)
		{
			position.height = SingleLineHeight - 2f;
			position.y += 1f;
			value = GUI.TextArea(position, value, guiStyle);
			return value;
		}

		public override string TextField(Rect position, string value, GUIStyle guiStyle)
		{
			position.height = SingleLineHeight - 2f;
			position.y += 1f;
			value = GUI.TextField(position, value, guiStyle);
			return value;
		}
		
		public override string TextField(Rect position, GUIContent label, string value, GUIStyle guiStyle)
		{
			var remainingPos = string.IsNullOrEmpty(label.text) ? position : PrefixLabel(position, label);
			value = GUI.TextField(remainingPos, value, guiStyle);
			return value;
		}

		public override void Label(Rect position, GUIContent label, string styleName)
		{
			GUI.Label(position, label, InspectorUtility.Preferences.GetStyle(styleName));
		}

		public override void Label(Rect position, GUIContent label, GUIStyle style)
		{
			GUI.Label(position, label, style);
		}

		public override void HelpBox(Rect position, string message, MessageType messageType)
		{
			float leftIndex = LeftPadding + IndentLevel * IndentWidth;
			position.x += leftIndex;
			position.width -= leftIndex + RightPadding;

			switch(messageType)
			{
				case MessageType.Info:
					GUI.Label(position, message, "CN EntryInfo");
					break;
				case MessageType.Warning:
					GUI.Label(position, message, "CN EntryWarn");
					break;
				case MessageType.Error:
					GUI.Label(position, message, "CN EntryError");
					break;
			}
		}

		public Object ObjectField(Rect position, GUIContent label, Object value, Type objType, GUIStyle guiStyle = null)
		{
			var transform = value.Transform();
			if(transform == null)
			{
				return value;
			}
			var hierarchyPath = transform.HierarchyPath();

			var pos = FirstLine(position);
			
			var setPath = Active.TextField(pos, label, hierarchyPath, guiStyle ?? InspectorPreferences.Styles.TextField);
			if(!string.Equals(setPath, hierarchyPath))
			{
				hierarchyPath = setPath;
				if(string.IsNullOrEmpty(hierarchyPath))
				{
					value = null;
				}
				else
				{
					var tryFind = GameObject.Find(hierarchyPath);
					if(tryFind != null)
					{
						if(objType.IsComponent())
						{
							value = tryFind.GetComponent(objType);
						}
						else if(objType.IsGameObject())
						{
							value = tryFind;
						}
						else
						{
							value = null;
						}
					}
				}
			}
			return value;
		}
		
		public override void AcceptDrag()
		{
			//dragging not yet implemented for runtime
		}

		public override void AddCursorRect(Rect position, MouseCursor cursor) { }

		public override void PingObject(Object target)
		{
			var transform = target.Transform();
			if(transform != null)
			{
				if(transform.gameObject.scene.IsValid())
				{
					//TO DO: Use this.Debugger (or DrawGUI.Debugger) instead of Debug,
					//to allow displaying these messages at runtime?
					Message(transform.GetHierarchyPath());
				}
				else
				{
					Message(string.Concat("(Resource:", target.GetType().Name, ")\"", target.name, "\""));
				}
			}
			else
			{
				Debug.LogError("Failed to ping target "+(target == null ? "null" : target.GetType().Name));
			}
		}

		public override int DisplayDialog(string title, string message, string button1, string button2, string button3)
		{
			throw new NotImplementedException("DisplayDialog runtime support not implemented");
		}

		public override bool DisplayDialog(string title, string message, string ok, string cancel)
		{
			throw new NotImplementedException("DisplayDialog runtime support not implemented");
		}

		public override void DisplayDialog(string title, string message, string ok)
		{
			throw new NotImplementedException("DisplayDialog runtime support not implemented");
		}
		
		public override string ToString()
		{
			return "RuntimeGUIDrawer";
		}
	}
}