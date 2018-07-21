using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SingleSelectionPopup : PopupWindowContent
{
	int index;
	GUIContent[] options;
	GUIStyle style;

	public SingleSelectionPopup(int selectedIndex, GUIContent[] displayedOptions)
	{
		index = selectedIndex;
		options = displayedOptions;
		style = GUIStyle.none;
	}

	public SingleSelectionPopup(int selectedIndex, GUIContent[] displayedOptions, GUIStyle displayedStyle)
	{
		index = selectedIndex;
		options = displayedOptions;
		style = displayedStyle;
	}

	public override Vector2 GetWindowSize()
	{
		Vector2 size = Vector2.one;

		for (int i = 0; i < options.Length; i++)
		{
			Vector2 contentSize = style.CalcSize(options[i]);
			size.x = Mathf.Max(size.x, contentSize.x);
			size.y += contentSize.y;
		}

		return size;
	}

	public override void OnGUI(Rect rect)
	{
		Rect labelRect = rect;

		for (int i = 0; i < options.Length; i++)
		{
			Vector2 contentSize = style.CalcSize(options[i]);
			labelRect.width = Mathf.Min(rect.width, contentSize.x);
			labelRect.height = contentSize.y;

			GUI.Label(labelRect, options[i], style);

			labelRect.y += labelRect.height;
		}
	}

	public override void OnOpen()
	{
		Debug.Log("Popup opened: " + this);
	}

	public override void OnClose()
	{
		Debug.Log("Popup closed: " + this);
	}
}