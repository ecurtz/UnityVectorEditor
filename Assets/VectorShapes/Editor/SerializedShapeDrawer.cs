using UnityEngine;
using UnityEditor;

// IngredientDrawer
[CustomPropertyDrawer(typeof(SerializedShape), true)]
public class SerializedShapeDrawer : PropertyDrawer
{
	const float padding = 4f;

	const float editButtonWidth = 30f;

	const float penLabelWidth = 24f;
	const float penSizeWidth = 30f;
	const float fillLabelWidth = 24f;

	const float colorChooserWidth = 24f;

	static GUIContent penLabelContent = new GUIContent("Pen", "Size and color of outline.");
	static GUIContent fillLabelContent = new GUIContent("Fill", "Color of interior.");
	static GUIContent editButtonContent = new GUIContent("Edit", "Modify the shape data.");

	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		// Bail if the ScriptableObject hasn't been initialized
		if (property.objectReferenceValue == null)
		{
			EditorGUI.PropertyField(position, property);
			return;
		}

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		SerializedObject propertyObject = new SerializedObject(property.objectReferenceValue);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		Rect shapeSettingsRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
		Rect colorSettingsRect = new Rect(position.x, shapeSettingsRect.yMax + padding, position.width, EditorGUIUtility.singleLineHeight);

		Rect editButtonRect = new Rect(shapeSettingsRect);
		editButtonRect.x = shapeSettingsRect.xMax - editButtonWidth;
		editButtonRect.width = editButtonWidth;

		//Rect shapeDescriptionRect = new Rect(shapeSettingsRect);
		//shapeDescriptionRect.x = shapeSettingsRect.x;
		//shapeDescriptionRect.xMax = editButtonRect.x - padding;

		//string shapeType = property.objectReferenceValue.GetType().Name;
		//int trimIndex = shapeType.LastIndexOf("Shape");
		//if (trimIndex > 0)
		//{
		//	shapeType = shapeType.Substring(0, trimIndex);
		//}
		//GUIContent shapeLabelContent = new GUIContent(shapeType, "Kind of shape.");

		//GUIStyle rightJustified = new GUIStyle(GUI.skin.label);
		//rightJustified.alignment = TextAnchor.MiddleRight;
		//EditorGUI.LabelField(shapeDescriptionRect, shapeLabelContent, rightJustified);

		if (GUI.Button(editButtonRect, editButtonContent, EditorStyles.miniButton))
		{
			SerializedShape serializedShape = property.objectReferenceValue as SerializedShape;

			VectorShapeEditor.OpenEditor(serializedShape);

			EditorUtility.SetDirty(serializedShape);
		}
		//EditorGUI.PropertyField(shapeSettingsRect, property, GUIContent.none);

		/*
		// Go right to left so we overflow back into the label space on small windows
		Rect fillColorRect = new Rect(colorSettingsRect);
		fillColorRect.width = colorChooserWidth;
		fillColorRect.x = colorSettingsRect.xMax - fillColorRect.width;
		Rect fillLabelRect = new Rect(colorSettingsRect);
		fillLabelRect.width = fillLabelWidth;
		fillLabelRect.x = fillColorRect.x - fillLabelRect.width - padding;

		Rect penColorRect = new Rect(colorSettingsRect);
		penColorRect.width = colorChooserWidth;
		penColorRect.x = fillLabelRect.x - penColorRect.width - padding;
		Rect penSizeRect = new Rect(colorSettingsRect);
		penSizeRect.width = penSizeWidth;
		penSizeRect.x = penColorRect.x - penSizeRect.width - padding;
		Rect penLabelRect = new Rect(colorSettingsRect);
		penLabelRect.width = penLabelWidth;
		penLabelRect.x = penSizeRect.x - penLabelRect.width;

		EditorGUI.BeginChangeCheck();
		EditorGUI.LabelField(penLabelRect, penLabelContent);
		SerializedProperty penSize = propertyObject.FindProperty("penSize");
		SerializedProperty colorOutline = propertyObject.FindProperty("colorOutline");
		EditorGUI.PropertyField(penSizeRect, penSize, GUIContent.none);
		colorOutline.colorValue = EditorGUI.ColorField(penColorRect, GUIContent.none, colorOutline.colorValue, false, true, false);

		EditorGUI.LabelField(fillLabelRect, fillLabelContent);
		SerializedProperty colorFill = propertyObject.FindProperty("colorFill");
		colorFill.colorValue = EditorGUI.ColorField(fillColorRect, GUIContent.none, colorFill.colorValue, false, true, false);

		if (EditorGUI.EndChangeCheck() || propertyObject.hasModifiedProperties)
		{
			SerializedProperty shapeDirty = propertyObject.FindProperty("shapeDirty");
			if (shapeDirty != null) shapeDirty.boolValue = true;
			SerializedProperty meshDirty = propertyObject.FindProperty("meshDirty");
			if (meshDirty != null) meshDirty.boolValue = true;

			propertyObject.ApplyModifiedProperties();
			propertyObject.Update();
		}
		*/

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
    }

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		if (property.objectReferenceValue == null)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		return EditorGUIUtility.singleLineHeight + padding;
	}
}