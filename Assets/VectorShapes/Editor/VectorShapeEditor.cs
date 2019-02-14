using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VectorGraphics;

[ExecuteInEditMode]
public class VectorShapeEditor : EditorWindow
{
	/// <summary>
	/// Tool set for vector editor
	/// </summary>
	protected enum ToolSet
	{
		View,
		Select,
		Brush,
		Shape
	}
	const int ToolSetCount = 4;
	protected static GUIContent[] toolIcons;

	/// <summary>
	/// Shapes for vector editor
	/// </summary>
	protected enum ShapeSet
	{
		Point,
		Circle,
		Triangle,
		Rectangle,
		Pentagon,
		Hexagon,
		Polygon
	}
	const int ShapeSetCount = 7;
	protected static GUIContent[] shapeIcons;

	/// <summary>
	/// Selection levels for vector editor
	/// </summary>
	protected enum SelectionSet
	{
		Object,
		Component,
		Vertex
	}
	const int SelectionSetCount = 3;
	protected static GUIContent[] selectionIcons;

	/// <summary>
	/// Action taken with input events
	/// </summary>
	protected enum ActionType
	{
		None = -1,
		ViewPan,
		ViewZoom,
		SelectNormal,
		SelectAdditive,
		SelectSubtractive,
		LineEdit,
		CurveEdit,
		ShapeEdit,
		SetOrigin,
		ChangeScale
	}
	protected ActionType baseAction = ActionType.None;
	protected ActionType quickAction = ActionType.None;
	protected ActionType overrideAction = ActionType.None;

	protected ActionType action
	{
		get
		{
			if (overrideAction != ActionType.None) return overrideAction;
			if (quickAction != ActionType.None) return quickAction;
			return baseAction;
		}
	}

	/// <remarks>
	/// For some reason EventCommandNames is internal, so redefine them here
	/// </remarks>
	protected const string Cmd_Delete = "Delete";
	protected const string Cmd_SelectAll = "SelectAll";
	protected const string Cmd_ModifierKeysChanged = "ModifierKeysChanged";

	/// <summary>
	/// Material for rendering vector shapes.
	/// </summary>
	protected static Material renderMaterial;

	protected PreviewRenderUtility renderUtil = null;

	/// <summary>
	/// Texture for control seperator.
	/// </summary>
	protected static Texture2D separatorTexture;
	const int separatorWidth = 2;

	/// <summary>
	/// Miscellaneous GUI content.
	/// </summary>
	protected static GUIContent iconMoreTools;
	protected static GUIContent iconShapeStroke;
	protected static GUIContent iconShapeFill;

	public float shapePenSize = 2f;
	public Color shapeOutlineColor = Color.black;
	public Color shapeFillColor = Color.clear;

	/// <summary>
	/// Window state.
	/// </summary>
	protected EditorWindow popup;

	protected ToolSet activeTool;
	protected SelectionSet activeSelect;
	protected ShapeSet activeShape;

	protected VectorShape focusedShape;

	protected List<VectorShape> shapes = new List<VectorShape>();

	public Color backgroundColor = Color.Lerp(Color.black, Color.white, 0.9f);

	const int toolbarPadding = 5;
	const int toolbarHeight = 22;
	const int toolbarWidth = 32;
	const int colorFieldWidth = 40;

	const int toolAreaHeight = toolbarHeight + toolbarPadding * 2;

	/// <summary>
	/// User interaction state.
	/// </summary>
	protected Vector2 mousePosition;
	protected Vector2 mouseDownPosition;
	protected float mouseDownTime;
	protected bool mouseIsDown;
	protected bool mouseInContent;

	protected float mouseToShapeScale;
	protected Matrix2D mouseToShapeMatrix;

	const float dragMargin = 8f;
	protected bool dragActive;
	protected Rect selectionRect;

	protected bool showSnap;
	protected Vector2 snapPosition;

	/// <summary>
	/// Edit target.
	/// </summary>
	protected SerializedShape targetShape;
	protected bool shapeDirty;

	/// <summary>
	/// Initializes the GUI Content.
	/// </summary>
	private void InitializeGUIContent()
	{
		if (renderMaterial == null)
		{
			renderMaterial = new Material(Shader.Find("Unlit/Vector"));
		}

		if (separatorTexture == null)
		{
			separatorTexture = new Texture2D(separatorWidth, 1);
			separatorTexture.SetPixel(0, 0, Color.gray);
			separatorTexture.SetPixel(1, 0, Color.white);
			separatorTexture.Apply();
		}

		Texture2D icon;
		if (toolIcons == null)
		{
			toolIcons = new GUIContent[ToolSetCount];

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconView);
			toolIcons[(int)ToolSet.View] = new GUIContent(icon, "View Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectionRect);
			toolIcons[(int)ToolSet.Select] = new GUIContent(icon, "Selection Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconBrush);
			toolIcons[(int)ToolSet.Brush] = new GUIContent(icon, "Draw Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconShapes);
			toolIcons[(int)ToolSet.Shape] = new GUIContent(icon, "Shape Tool");
		}

		if (shapeIcons == null)
		{
			shapeIcons = new GUIContent[ShapeSetCount];

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPoint);
			shapeIcons[(int)ShapeSet.Point] = new GUIContent(icon, "Create Point");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconCircle);
			shapeIcons[(int)ShapeSet.Circle] = new GUIContent(icon, "Create Circle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconTriangle);
			shapeIcons[(int)ShapeSet.Triangle] = new GUIContent(icon, "Create Triangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSquare);
			shapeIcons[(int)ShapeSet.Rectangle] = new GUIContent(icon, "Create Rectangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPentagon);
			shapeIcons[(int)ShapeSet.Pentagon] = new GUIContent(icon, "Create Pentagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconHexagon);
			shapeIcons[(int)ShapeSet.Hexagon] = new GUIContent(icon, "Create Hexagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPolygon);
			shapeIcons[(int)ShapeSet.Polygon] = new GUIContent(icon, "Create Polygon");
		}

		if (selectionIcons == null)
		{
			selectionIcons = new GUIContent[SelectionSetCount];

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectObject);
			selectionIcons[(int)SelectionSet.Object] = new GUIContent(icon, "Select Shapes");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectSegment);
			selectionIcons[(int)SelectionSet.Component] = new GUIContent(icon, "Select Segments");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectVertex);
			selectionIcons[(int)SelectionSet.Vertex] = new GUIContent(icon, "Select Vertices");
		}

		if (iconMoreTools == null)
		{
			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconVerticalDots);
			iconMoreTools = new GUIContent(icon, "More Tools");
		}

		if (iconShapeStroke == null)
		{
			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconCircle);
			iconShapeStroke = new GUIContent(icon, "Outline Color");
		}

		if (iconShapeFill == null)
		{
			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconFilledCircle);
			iconShapeFill = new GUIContent(icon, "Fill Color");
		}
	}

	public static void OpenEditor(SerializedShape shape)
	{
		VectorShapeEditor editorWindow = EditorWindow.GetWindow(typeof(VectorShapeEditor)) as VectorShapeEditor;

		if (editorWindow.OnCheckSave())
		{
			editorWindow.targetShape = shape;
			editorWindow.titleContent.text = shape.name;
			editorWindow.Shapes = shape.components;
			editorWindow.Focus();
		}
	}

	public List<VectorShape> Shapes
	{
		set
		{
			shapes = value;

			Rect bounds;
			if (shapes.Count > 0)
			{
				bounds = shapes[0].ShapeBounds;
			}
			else
			{
				bounds = new Rect(-50f, -50f, 100f, 100f);
			}
			foreach (VectorShape shape in shapes)
			{
				bounds = VectorShapeUtils.RectUnion(bounds, shape.ShapeBounds);
			}

			SetCameraView(bounds.height * 0.6f, bounds.center);

			shapeDirty = false;
		}
		get
		{
			return shapes;
		}
	}

	protected List<VectorShape> previousSelection = new List<VectorShape>();
	protected List<VectorShape> selection = new List<VectorShape>();
	public List<VectorShape> Selection
	{
		set
		{
			previousSelection = selection;
			selection = value;

			Repaint();
		}
		get
		{
			return selection;
		}
	}

	protected void SetCameraView(float size, Vector2 position)
	{
		renderUtil.camera.orthographicSize = size;
		renderUtil.camera.transform.position = new Vector3(position.x, position.y, -1f);
		UpdateMouseToShape();

		foreach (VectorShape shape in shapes)
		{
			shape.penToMeshScale = mouseToShapeScale;
			shape.Dirty = true;
		}

		Repaint();
	}

	protected void SetCameraSize(float size)
	{
		renderUtil.camera.orthographicSize = size;
		UpdateMouseToShape();

		foreach (VectorShape shape in shapes)
		{
			shape.penToMeshScale = mouseToShapeScale;
			shape.Dirty = true;
		}

		Repaint();
	}

	protected void SetCameraPosition(Vector2 position)
	{
		renderUtil.camera.transform.position = new Vector3(position.x, position.y, -1f);
		UpdateMouseToShape();

		Repaint();
	}

	public Vector2 MouseToShapePoint(Vector2 mousePoint)
	{
		return mouseToShapeMatrix * mousePoint;
	}

	protected void UpdateMouseToShape()
	{
		Rect viewRect = new Rect(0, toolAreaHeight, this.position.width, this.position.height - toolAreaHeight);
		mouseToShapeScale = renderUtil.camera.orthographicSize * 2f / viewRect.height;
		mouseToShapeMatrix =
			Matrix2D.Translate(renderUtil.camera.transform.position) *
			Matrix2D.Scale(new Vector2(mouseToShapeScale, -mouseToShapeScale)) *
			Matrix2D.Translate(-viewRect.center);
	}

	protected bool OnCheckSave()
	{
		bool accepted = true;

		if (targetShape != null)
		{
			targetShape.SetShapes(shapes);
			EditorUtility.SetDirty(targetShape);
			AssetDatabase.SaveAssets();
		}
		else if (shapeDirty)
		{
			int option = EditorUtility.DisplayDialogComplex(
				"Unsaved Changes",
				"Do you want to save the changes you've made?",
				"Save", "Cancel", "Don't Save");

			switch (option)
			{
				case 0: // Save
					SaveShape();
					break;

				case 1: // Cancel
					accepted = false;
					break;

				case 2: // Don't Save.
					break;

				default:
					break;
			}
		}

		return accepted;
	}

	protected void SaveShape()
	{
		string path = EditorUtility.SaveFilePanelInProject("Save shape", "New Shape", "asset", "");
	}

	public void OnEnable()
	{
		renderUtil = new PreviewRenderUtility();
		renderUtil.camera.orthographic = true;
		renderUtil.camera.orthographicSize = 1f;
		renderUtil.camera.clearFlags = CameraClearFlags.SolidColor;
		renderUtil.camera.nearClipPlane = 0.1f;
		renderUtil.camera.farClipPlane = 100.0f;
		renderUtil.camera.transform.position = new Vector3(0f, 0f, -1f);

		InitializeGUIContent();

		EditorApplication.modifierKeysChanged += OnModifierKeys;
		this.wantsMouseMove = true;
	}

	public void OnDisable()
	{
		if (popup != null)
		{
			popup.Close();
			popup = null;
		}

		if (renderUtil != null)
		{
			renderUtil.Cleanup();
			renderUtil = null;
		}

		EditorApplication.modifierKeysChanged -= OnModifierKeys;
	}

	public void OnDestroy()
	{
		if (renderUtil != null)
		{
			renderUtil.Cleanup();
			renderUtil = null;
		}

		if (popup != null)
		{
			Object.DestroyImmediate(popup);
			popup = null;
		}

		OnCheckSave();
	}

	public void OnModifierKeys()
	{
		SendEvent(EditorGUIUtility.CommandEvent(Cmd_ModifierKeysChanged));
	}

	protected void OnToolbarArea(Event guiEvent, Rect guiRect)
	{
		int activeButton = (int)activeTool;
		if (overrideAction != ActionType.None)
		{
			switch (overrideAction)
			{
				case ActionType.ViewPan:
				case ActionType.ViewZoom:
					activeButton = (int)ToolSet.View;
					break;
			}
		}

		Rect toolbarRect = new Rect(guiRect);
		toolbarRect.width = toolbarWidth * toolIcons.Length;
		int newButton = GUI.Toolbar(toolbarRect, activeButton, toolIcons);
		if (newButton != activeButton)
		{
			activeTool = (ToolSet)newButton;
		}

		Rect buttonRect = new Rect(guiRect);
		buttonRect.width = toolbarWidth;
		buttonRect.x = toolbarRect.xMax + toolbarPadding;
		if (GUI.Button(buttonRect, iconMoreTools))
		{
			GenericMenu toolMenu = new GenericMenu();

			toolMenu.AddItem(new GUIContent("Move Origin"), false, OnMoveOrigin);
			toolMenu.AddItem(new GUIContent("Change Scale"), false, OnChangeScale);

			toolMenu.AddSeparator("");

			toolMenu.AddItem(new GUIContent("Load SVG"), false, OnLoadSVG);
#if DXF_SUPPORT
			toolMenu.AddItem(new GUIContent("Load DXF"), false, OnLoadDXF);
#endif

			toolMenu.DropDown(buttonRect);
		}
	}

	protected void OnMoveOrigin()
	{
		quickAction = ActionType.SetOrigin;
		showSnap = true;
	}

	protected void OnChangeScale()
	{
		quickAction = ActionType.ChangeScale;
	}

	protected void OnChangeScale(float scale)
	{
		popup.Close();
		popup = null;
	}

	protected void OnLoadSVG()
	{
		if (OnCheckSave())
		{
			string[] svgExtensions = { "svg", "SVG" };
			string svgPath = EditorUtility.OpenFilePanelWithFilters("Load SVG", "", svgExtensions);
			if (System.IO.File.Exists(svgPath))
			{
				System.IO.StreamReader reader = System.IO.File.OpenText(svgPath);
				Shapes = VectorShapeFilesSVG.ReadSVG(reader);
			}
		}
	}

#if DXF_SUPPORT
	protected void OnLoadDXF()
	{
		if (OnCheckSave())
		{
			string[] dxfExtensions = { "dxf", "DXF" };
			string dxfPath = EditorUtility.OpenFilePanelWithFilters("Load DXF", "", dxfExtensions);
			if (System.IO.File.Exists(dxfPath))
			{
				System.IO.FileStream stream = System.IO.File.OpenRead(dxfPath);
				Shapes = VectorShapeFilesDXF.ReadDXF(stream);
			}
		}
	}
#endif

	const float penLabelWidth = 26f;
	const float penSizeWidth = 50f;
	const float fillLabelWidth = 20f;

	const float colorChooserWidth = 24f;

	static GUIContent penLabelContent = new GUIContent("Pen", "Size and color of outline.");
	static GUIContent fillLabelContent = new GUIContent("Fill", "Color of interior.");

	VectorShape.ShapeProxy testProxy;
	protected void OnShapeColorArea(Event guiEvent, Rect guiRect)
	{
		if (testProxy == null)
		{
			testProxy = ScriptableObject.CreateInstance<VectorShape.ShapeProxy>();
			testProxy.name = "Shape";
			//testProxy.hideFlags = HideFlags.HideAndDontSave;
			testProxy.penSize = shapePenSize;
			testProxy.colorOutline = shapeOutlineColor;
			testProxy.colorFill = shapeFillColor;
		}

		// Go right to left
		Rect displayRect = new Rect(guiRect);
		displayRect.height = EditorGUIUtility.singleLineHeight;
		displayRect.y += (guiRect.height - displayRect.height) / 2f;
		displayRect.width -= toolbarPadding;
		Rect fillColorRect = new Rect(displayRect);
		fillColorRect.width = colorChooserWidth;
		fillColorRect.x = displayRect.xMax - fillColorRect.width;
		Rect fillLabelRect = new Rect(displayRect);
		fillLabelRect.width = fillLabelWidth;
		fillLabelRect.x = fillColorRect.x - fillLabelRect.width;

		Rect penColorRect = new Rect(displayRect);
		penColorRect.width = colorChooserWidth;
		penColorRect.x = fillLabelRect.x - penColorRect.width - toolbarPadding;
		Rect penSizeRect = new Rect(displayRect);
		penSizeRect.width = penSizeWidth;
		penSizeRect.x = penColorRect.x - penSizeRect.width - toolbarPadding;
		Rect penLabelRect = new Rect(displayRect);
		penLabelRect.width = penLabelWidth;
		penLabelRect.x = penSizeRect.x - penLabelRect.width;

		GUIStyle rightJustified = new GUIStyle(GUI.skin.label);
		rightJustified.alignment = TextAnchor.MiddleRight;

		SerializedObject propertyObject = new SerializedObject(testProxy);
		EditorGUI.BeginChangeCheck();

		EditorGUI.LabelField(penLabelRect, penLabelContent, rightJustified);
		SerializedProperty penSize = propertyObject.FindProperty("penSize");
		SerializedProperty colorOutline = propertyObject.FindProperty("colorOutline");
		penSize.floatValue = GUI.HorizontalSlider(penSizeRect, penSize.floatValue, 0f, 25f);
		colorOutline.colorValue = EditorGUI.ColorField(penColorRect, GUIContent.none, colorOutline.colorValue, false, true, false);

		EditorGUI.LabelField(fillLabelRect, fillLabelContent, rightJustified);
		SerializedProperty colorFill = propertyObject.FindProperty("colorFill");
		colorFill.colorValue = EditorGUI.ColorField(fillColorRect, GUIContent.none, colorFill.colorValue, false, true, false);

		if (EditorGUI.EndChangeCheck() || propertyObject.hasModifiedProperties)
		{
			propertyObject.ApplyModifiedProperties();
			propertyObject.Update();

			shapePenSize = testProxy.penSize;
			shapeOutlineColor = testProxy.colorOutline;
			shapeFillColor = testProxy.colorFill;
		}
		//EditorGUI.BeginChangeCheck();
		//EditorGUI.LabelField(penLabelRect, penLabelContent);
		//shapePenSize = EditorGUI.DelayedFloatField(penSizeRect, shapePenSize);

		//shapeOutlineColor = EditorGUI.ColorField(penColorRect, GUIContent.none, shapeOutlineColor, false, true, false);

		//EditorGUI.LabelField(fillLabelRect, fillLabelContent);
		//shapeFillColor = EditorGUI.ColorField(fillColorRect, GUIContent.none, shapeFillColor, false, true, false);

		//if (EditorGUI.EndChangeCheck())
		//{
		//}

	}

	protected void OnInfoAreaView(Event guiEvent, Rect guiRect)
	{
		GUILayout.BeginArea(guiRect);

		if (selection.Count == 0)
		{
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			Vector2 shapePosition = MouseToShapePoint(mousePosition);

			GUILayout.Label("Mouse");
			GUILayout.Label("X", GUILayout.Width(10f));
			GUILayout.Label(shapePosition.x.ToString("F2"), EditorStyles.textField, GUILayout.Width(50f));
			GUILayout.Label("Y", GUILayout.Width(10f));
			GUILayout.Label(shapePosition.y.ToString("F2"), EditorStyles.textField, GUILayout.Width(50f));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Background", GUILayout.MaxWidth(70f));
			backgroundColor = EditorGUILayout.ColorField(backgroundColor, GUILayout.MaxWidth(colorFieldWidth));

			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
		}

		GUILayout.EndArea();
	}

	protected void OnInfoAreaSelect(Event guiEvent, Rect guiRect)
	{
		Rect toolbarRect = new Rect(toolbarPadding, toolbarPadding, toolbarWidth * selectionIcons.Length, toolbarHeight);
		toolbarRect.position += guiRect.position;

		int activeButton = (int)activeSelect;

		int newButton = GUI.Toolbar(toolbarRect, activeButton, selectionIcons);
		if (newButton != activeButton)
		{
			activeSelect = (SelectionSet)newButton;
		}

		if (focusedShape != null)
		{
			shapeOutlineColor = focusedShape.colorOutline;
			shapeFillColor = focusedShape.colorFill;
		}
		Color currentOutline = shapeOutlineColor;
		Color currentFill = shapeFillColor;
		float currentPen = shapePenSize;

		Rect colorsRect = new Rect(toolbarRect.xMax, guiRect.position.y, guiRect.xMax - toolbarRect.xMax, guiRect.height);
		OnShapeColorArea(guiEvent, colorsRect);

		if (currentOutline != shapeOutlineColor)
		{
			foreach (VectorShape shape in selection)
			{
				shape.colorOutline = shapeOutlineColor;
				shape.Dirty = true;
			}
		}
		if (currentFill != shapeFillColor)
		{
			foreach (VectorShape shape in selection)
			{
				shape.colorFill = shapeFillColor;
				shape.Dirty = true;
			}
		}
		if (!Mathf.Approximately(currentPen, shapePenSize))
		{
			foreach (VectorShape shape in selection)
			{
				shape.penSize = shapePenSize;
				shape.Dirty = true;
			}
		}
	}

	protected void OnInfoAreaBrush(Event guiEvent, Rect guiRect)
	{
	}

	protected void OnInfoAreaShape(Event guiEvent, Rect guiRect)
	{
		Rect toolbarRect = new Rect(toolbarPadding, toolbarPadding, toolbarWidth * shapeIcons.Length, toolbarHeight);
		toolbarRect.position += guiRect.position;

		int activeButton = (int)activeShape;

		int newButton = GUI.Toolbar(toolbarRect, activeButton, shapeIcons);
		if (newButton != activeButton)
		{
			activeShape = (ShapeSet)newButton;

			SetFocusedShape(null);
		}

		Rect colorsRect = new Rect(toolbarRect.xMax, guiRect.position.y, guiRect.xMax - toolbarRect.xMax, guiRect.height);
		OnShapeColorArea(guiEvent, colorsRect);
	}

	public void OnGUI()
	{
		Event guiEvent = Event.current;

		if (guiEvent.isScrollWheel)
		{
			overrideAction = ActionType.ViewZoom;
		}
		else if (guiEvent.type == EventType.MouseDown)
		{
			mouseDownPosition = guiEvent.mousePosition;
			mouseDownTime = Time.time;
			mouseIsDown = true;
		}
		else if (guiEvent.type == EventType.MouseUp)
		{
			mouseIsDown = false;
		}
		else if (guiEvent.type != EventType.Repaint)
		{
			overrideAction = ActionType.None;
			if (guiEvent.alt)
			{
				if (guiEvent.control)
				{
					overrideAction = ActionType.ViewZoom;
				}
				else
				{
					overrideAction = ActionType.ViewPan;
				}
			}
		}

		UpdateMouseToShape();
		Rect toolRect = new Rect(0, 0, this.position.width, toolAreaHeight);

		Rect toolbarRect = new Rect(toolbarPadding * 2, toolbarPadding, toolbarWidth * (toolIcons.Length + 1) + toolbarPadding, toolbarHeight);
		OnToolbarArea(guiEvent, toolbarRect);

		Rect separatorRect = new Rect(toolbarRect.xMax + toolbarPadding, 0, separatorWidth, toolRect.height);
		GUI.DrawTexture(separatorRect, separatorTexture);

		Rect infoRect = new Rect(separatorRect.xMax, 0, toolRect.width - separatorRect.xMax, toolRect.height);
		switch (activeTool)
		{
			case ToolSet.View:
				OnInfoAreaView(guiEvent, infoRect);
				baseAction = ActionType.ViewPan;
				break;
			case ToolSet.Select:
				OnInfoAreaSelect(guiEvent, infoRect);
				baseAction = ActionType.SelectNormal;
				break;
			case ToolSet.Brush:
				OnInfoAreaBrush(guiEvent, infoRect);
				baseAction = ActionType.LineEdit;
				break;
			case ToolSet.Shape:
				OnInfoAreaShape(guiEvent, infoRect);
				baseAction = ActionType.ShapeEdit;
				break;
		}

		Rect viewRect = new Rect(0, toolRect.yMax, this.position.width, this.position.height - toolRect.yMax);

		MouseCursor activeCursor = MouseCursor.Arrow;
		switch (action)
		{
			case ActionType.ViewPan:
				activeCursor = MouseCursor.Pan;
				break;
			case ActionType.ViewZoom:
				activeCursor = MouseCursor.Zoom;
				break;
		}
		EditorGUIUtility.AddCursorRect(viewRect, activeCursor);

		bool handled = false;

		if (guiEvent.type == EventType.Repaint)
		{
			renderUtil.BeginPreview(viewRect, GUIStyle.none);
			renderUtil.camera.backgroundColor = backgroundColor;

			foreach (VectorShape shape in shapes)
			{
				renderUtil.DrawMesh(shape.ShapeMesh, Matrix4x4.identity, renderMaterial, 0);
			}
			renderUtil.Render();

			Handles.SetCamera(renderUtil.camera);
			VectorShape.handleDrawSize = HandleUtility.GetHandleSize(Vector3.zero) * viewRect.height / viewRect.width;

			foreach (VectorShape selected in selection)
			{
				selected.DrawEditorHandles(true, (selected == focusedShape));
			}

			if (selectionRect != Rect.zero)
			{
				Color outlineColor = Handles.color;
				Color fillColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.2f);
				Handles.DrawSolidRectangleWithOutline(selectionRect, fillColor, outlineColor);
			}

			renderUtil.EndAndDrawPreview(viewRect);

			Vector2 originLoc = mouseToShapeMatrix.Inverse().MultiplyPoint(Vector2.zero);
			if (viewRect.Contains(originLoc))
			{
				GUIStyle originPivot = "U2D.pivotDot";
				if (originPivot != null)
				{
					Rect pivotRect = new Rect(0f, 0f, originPivot.fixedWidth, originPivot.fixedHeight);
					pivotRect.center = originLoc;
					originPivot.Draw(pivotRect, false, false, false, false);
				}
			}

			if (showSnap)
			{
				Vector2 snapLoc = mouseToShapeMatrix.Inverse().MultiplyPoint(snapPosition);
				if (viewRect.Contains(snapLoc))
				{
					GUIStyle snapPivot = "U2D.dragDot";
					if (snapPivot != null)
					{
						Rect pivotRect = new Rect(0f, 0f, snapPivot.fixedWidth, snapPivot.fixedHeight);
						pivotRect.center = snapLoc;
						snapPivot.Draw(pivotRect, false, false, false, false);
					}
				}
			}
		}
		else if (guiEvent.isScrollWheel && (EditorWindow.mouseOverWindow == this))
		{
			float zoomFactor = HandleUtility.niceMouseDeltaZoom;
			float orthographicSize = renderUtil.camera.orthographicSize * (1 - zoomFactor * .02f);
			SetCameraSize(orthographicSize);

			handled = true;
		}
		else if (guiEvent.isMouse)
		{
			// Update mouse position
			mousePosition = guiEvent.mousePosition;
			mouseInContent = (viewRect.Contains(mousePosition));

			switch (action)
			{
				case ActionType.ViewPan:
				case ActionType.ViewZoom:
					handled = OnViewToolMouse(guiEvent);
					break;
				case ActionType.SelectNormal:
				case ActionType.SelectAdditive:
				case ActionType.SelectSubtractive:
					handled = OnSelectToolMouse(guiEvent);
					break;
				case ActionType.LineEdit:
				case ActionType.CurveEdit:
					handled = OnBrushToolMouse(guiEvent);
					break;
				case ActionType.ShapeEdit:
					handled = OnShapeToolMouse(guiEvent);
					break;
				case ActionType.SetOrigin:
					handled = OnSetOriginMouse(guiEvent);
					break;
				//case ActionType.ChangeScale:
					//handled = OnChangeScaleMouse(guiEvent);
					//break;
			}

			if (!handled)
			{
				Vector2 delta = guiEvent.delta;
				guiEvent.mousePosition = MouseToShapePoint(mousePosition);
				guiEvent.delta = guiEvent.delta * new Vector2(mouseToShapeScale, -mouseToShapeScale);

				foreach (VectorShape selected in selection)
				{
					handled |= selected.HandleEditorEvent(guiEvent, true);
				}

				guiEvent.mousePosition = mousePosition;
				guiEvent.delta = delta;
			}
		}
		else if (guiEvent.type == EventType.ValidateCommand)
		{
			switch (guiEvent.commandName)
			{
				case Cmd_Delete:
					if (selection.Count > 0)
					{
						handled = true;
					}
					break;
				case Cmd_SelectAll:
					if (shapes.Count > 0)
					{
						handled = true;
					}
					break;
			}
		}
		else if (guiEvent.type == EventType.ExecuteCommand)
		{
			switch (guiEvent.commandName)
			{
				case Cmd_Delete:
					foreach (VectorShape selected in selection)
					{
						shapes.Remove(selected);
					}
					selection.Clear();
					handled = true;
					break;
				case Cmd_SelectAll:
					foreach (VectorShape selected in shapes)
					{
						selection.Add(selected);
					}
					handled = true;
					break;
				case Cmd_ModifierKeysChanged:
					Repaint();
					handled = true;
					break;
			}
		}

		if (action == ActionType.ChangeScale)
		{
			handled = OnChangeScaleGUI(guiEvent);
		}

		if (handled)
		{
			guiEvent.Use();
		}
	}

	protected bool OnViewToolMouse(Event guiEvent)
	{
		if (mouseInContent && (guiEvent.type == EventType.MouseDrag))
		{
			if (action == ActionType.ViewPan)
			{
				Vector2 cameraPos = (Vector2)renderUtil.camera.transform.position + guiEvent.delta * new Vector2(-mouseToShapeScale, mouseToShapeScale);
				SetCameraPosition(cameraPos);
			}
			else if (action == ActionType.ViewZoom)
			{
				float zoomFactor = HandleUtility.niceMouseDeltaZoom;
				float cameraSize = renderUtil.camera.orthographicSize * (1 + zoomFactor * .005f);
				SetCameraSize(cameraSize);
			}
		}

		return true;
	}

	protected bool OnSelectToolMouse(Event guiEvent)
	{
		selectionRect = Rect.zero;
		if (guiEvent.type == EventType.MouseDown)
		{
			dragActive = false;
			mouseDownPosition = guiEvent.mousePosition;
			previousSelection = selection;
			selection = new List<VectorShape>();
			focusedShape = null;
		}
		else if (guiEvent.type == EventType.MouseDrag)
		{
			Vector2 shapeDownPosition = MouseToShapePoint(mouseDownPosition);
			Vector2 shapePosition = MouseToShapePoint(mousePosition);

			float minX = Mathf.Min(shapeDownPosition.x, shapePosition.x);
			float maxX = Mathf.Max(shapeDownPosition.x, shapePosition.x);
			float minY = Mathf.Min(shapeDownPosition.y, shapePosition.y);
			float maxY = Mathf.Max(shapeDownPosition.y, shapePosition.y);

			selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);

			selection.Clear();
			foreach (VectorShape shape in shapes)
			{
				if (shape.IsInside(selectionRect))
				{
					selection.Add(shape);
				}
			}

			Repaint();
		}
		else if (guiEvent.type == EventType.MouseUp)
		{
			if (selection.Count == 1)
			{
				focusedShape = selection[0];
			}
		}

		return true;
	}

	protected bool OnBrushToolMouse(Event guiEvent)
	{
		Vector2 shapePosition = MouseToShapePoint(mousePosition);

		if (guiEvent.type == EventType.MouseDown)
		{
			dragActive = false;

			PolyShape activeLine = focusedShape as PolyShape;
			if (activeLine == null)
			{
				PolyShape newLine = PolyShape.Create(new Vector2[] { shapePosition });
				newLine.penToMeshScale = mouseToShapeScale;
				newLine.colorOutline = shapeOutlineColor;
				newLine.colorFill = shapeFillColor;
				SetFocusedShape(newLine);
			}
			else
			{
				activeLine.LineTo(shapePosition);
				if (guiEvent.clickCount > 1)
				{
					focusedShape = null;
				}
			}
			Repaint();
		}
		else if (guiEvent.type == EventType.MouseDrag)
		{
			PolyShape activeLine = focusedShape as PolyShape;
			if ((activeLine != null) && (activeLine.vertices.Length > 1))
			{
				int vertexIndex = activeLine.vertices.Length - 1;
				Vector2 offset;

				if ((!dragActive) && (Vector2.Distance(mouseDownPosition, guiEvent.mousePosition) > dragMargin))
				{
					dragActive = true;
					int previousIndex = vertexIndex - 1;

					if (!activeLine.vertices[previousIndex].segmentCurves)
					{
						activeLine.vertices[previousIndex].segmentCurves = true;
						offset = (activeLine.vertices[vertexIndex].position - activeLine.vertices[previousIndex].position) / 2f;
						activeLine.vertices[previousIndex].exitCP = activeLine.vertices[previousIndex].position + offset;
						if ((previousIndex < 1) || (!activeLine.vertices[previousIndex - 1].segmentCurves))
						{
							activeLine.vertices[previousIndex].enterCP = activeLine.vertices[previousIndex].position - offset;
						}
					}
				}

				if (dragActive)
				{
					activeLine.vertices[vertexIndex].exitCP = shapePosition;
					offset = activeLine.vertices[vertexIndex].exitCP - activeLine.vertices[vertexIndex].position;
					activeLine.vertices[vertexIndex].enterCP = activeLine.vertices[vertexIndex].position - offset;

					activeLine.Dirty = true;
					Repaint();
				}
			}
		}

		return true;
	}

	protected bool OnShapeToolMouse(Event guiEvent)
	{
		switch (activeShape)
		{
			case ShapeSet.Point:
				OnShapePointMouse(guiEvent);
				break;
			case ShapeSet.Circle:
				OnShapeCircleMouse(guiEvent);
				break;
			case ShapeSet.Triangle:
				OnShapeRegularPolygonMouse(guiEvent, 3);
				break;
			case ShapeSet.Rectangle:
				OnShapeRectangleMouse(guiEvent);
				break;
			case ShapeSet.Pentagon:
				OnShapeRegularPolygonMouse(guiEvent, 5);
				break;
			case ShapeSet.Hexagon:
				OnShapeRegularPolygonMouse(guiEvent, 6);
				break;
			case ShapeSet.Polygon:
				OnShapePolygonMouse(guiEvent);
				break;
		}

		return true;
	}

	protected void OnShapePointMouse(Event guiEvent)
	{
		if (guiEvent.type == EventType.MouseDown)
		{
			Vector2 shapePosition = MouseToShapePoint(mousePosition);

			PointShape newPoint = PointShape.Create(shapePosition);
			newPoint.penToMeshScale = mouseToShapeScale;
			newPoint.colorOutline = shapeOutlineColor;
			newPoint.colorFill = shapeFillColor;
			SetFocusedShape(newPoint);

			Repaint();
		}
	}

	protected void OnShapeCircleMouse(Event guiEvent)
	{
		Vector2 shapePosition = MouseToShapePoint(mousePosition);

		if (guiEvent.type == EventType.MouseDown)
		{
			CircleShape newCircle = CircleShape.Create(shapePosition, 0f);
			newCircle.penToMeshScale = mouseToShapeScale;
			newCircle.colorOutline = shapeOutlineColor;
			newCircle.colorFill = shapeFillColor;
			SetFocusedShape(newCircle);

			Repaint();
		}
		else if (guiEvent.type == EventType.MouseDrag)
		{
			CircleShape activeCircle = focusedShape as CircleShape;
			if (activeCircle != null)
			{
				Vector2 shapeDownPosition = MouseToShapePoint(mouseDownPosition);
				activeCircle.Radius = Vector2.Distance(shapeDownPosition, shapePosition);

				Repaint();
			}
		}
	}

	const float minPolyRadius = 0.005f;
	protected void OnShapeRegularPolygonMouse(Event guiEvent, int sides)
	{
		Vector2 shapePosition = MouseToShapePoint(mousePosition);

		if (guiEvent.type == EventType.MouseDown)
		{
			PolyShape newPoly = PolyShape.Create(shapePosition, minPolyRadius, sides);
			newPoly.penToMeshScale = mouseToShapeScale;
			newPoly.colorOutline = shapeOutlineColor;
			newPoly.colorFill = shapeFillColor;
			SetFocusedShape(newPoly);

			Repaint();
		}
		else if (guiEvent.type == EventType.MouseDrag)
		{
			PolyShape activePoly = focusedShape as PolyShape;
			if ((activePoly != null) && (activePoly.vertices.Length == sides))
			{
				Vector2 centerPosition = MouseToShapePoint(mouseDownPosition);
				float radius = Vector2.Distance(centerPosition, shapePosition);
				if (radius < minPolyRadius) return;

				for (int i = 0; i < sides; i++)
				{
					Vector2 offset = activePoly.vertices[i].position - centerPosition;
					activePoly.vertices[i].position = centerPosition + (offset.normalized * radius);
				}
				activePoly.Dirty = true;
				Repaint();
			}
		}
	}

	protected void OnShapeRectangleMouse(Event guiEvent)
	{
		Vector2 shapePosition = MouseToShapePoint(mousePosition);

		if (guiEvent.type == EventType.MouseDown)
		{
			PolyShape newRect = PolyShape.Create(shapePosition, 0f, 4);
			newRect.penToMeshScale = mouseToShapeScale;
			newRect.colorOutline = shapeOutlineColor;
			newRect.colorFill = shapeFillColor;
			SetFocusedShape(newRect);

			Repaint();
		}
		else if (guiEvent.type == EventType.MouseDrag)
		{
			PolyShape activeRect = focusedShape as PolyShape;
			if ((activeRect != null) && (activeRect.vertices.Length == 4))
			{
				activeRect.vertices[1].position.x = shapePosition.x;
				activeRect.vertices[2].position = shapePosition;
				activeRect.vertices[3].position.y = shapePosition.y;
				activeRect.Dirty = true;
				Repaint();
			}
		}
	}

	protected void OnShapePolygonMouse(Event guiEvent)
	{
		if (guiEvent.type == EventType.MouseDown)
		{
			Vector2 shapePosition = MouseToShapePoint(mousePosition);

			PolyShape activePoly = focusedShape as PolyShape;
			if (activePoly == null)
			{
				Vector2[] points = new Vector2[1];
				points[0] = shapePosition;

				PolyShape newPoly = PolyShape.Create(points);
				newPoly.penToMeshScale = mouseToShapeScale;
				SetFocusedShape(newPoly);
			}
			else
			{
				activePoly.LineTo(shapePosition);

				if (guiEvent.clickCount > 1)
				{
					SetFocusedShape(null);
				}
			}

			Repaint();
		}
	}

	protected bool OnSetOriginMouse(Event guiEvent)
	{
		Vector2 shapePt = mouseToShapeMatrix.MultiplyPoint(guiEvent.mousePosition);
		snapPosition = shapePt;
		VectorShape.SnapPoint.Mode snapMode = VectorShape.SnapPoint.Mode.Center | VectorShape.SnapPoint.Mode.Endpoint;
		float snapDistance = 10f * mouseToShapeScale;

		foreach (VectorShape shape in shapes)
		{
			VectorShape.SnapPoint snap = shape.GetSnap(shapePt, snapMode);
			float distance = Vector2.Distance(shapePt, snap.point);
			if (distance < snapDistance)
			{
				snapDistance = distance;
				snapPosition = snap.point;
			}
		}

		if (guiEvent.type == EventType.MouseDown)
		{
			quickAction = ActionType.None;
			showSnap = false;

			foreach (VectorShape shape in shapes)
			{
				shape.TranslateBy(-snapPosition);
			}

			//renderUtil.camera.transform.position += (Vector3)(-snapPosition);
			Vector2 cameraPos = (Vector2)renderUtil.camera.transform.position - snapPosition;
			SetCameraPosition(cameraPos);
		}

		return true;
	}

	const float popupWidth = 300f;
	const float popupHeight = 60f;
	protected bool OnChangeScaleGUI(Event guiEvent)
	{
		BeginWindows();

		Rect popupRect = new Rect((position.width - popupWidth) / 2f, 0f, popupWidth, popupHeight);
		popupRect = GUI.Window(0, popupRect, OnScalePopup, GUIContent.none, GUI.skin.box);

		EndWindows();

		return ((guiEvent.type != EventType.Layout) && (guiEvent.type != EventType.Repaint));
	}

	protected float scaleValue = 1f;
	protected void OnScalePopup(int id)
	{
		string[] scaleLables = { "Custom", "Inch to MM", "MM to Inch", "90DPI to Inch", "90DPI to MM" };
		float[] scaleValues = { scaleValue, 25.4f, (1f / 25.4f), (1f / 90f), (25.4f / 90f)};
		EditorGUILayout.BeginVertical();
		EditorGUILayout.BeginHorizontal();
		scaleValue = EditorGUILayout.FloatField("Scale shapes by", scaleValue);
		int conversion = 0;
		for (int i = 1; i < scaleValues.Length; i++)
		{
			if (Mathf.Approximately(scaleValue, scaleValues[i]))
			{
				conversion = i;
				break;
			}
		}
		conversion = EditorGUILayout.Popup(conversion, scaleLables);
		if (conversion != 0)
		{
			scaleValue = scaleValues[conversion];
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(toolbarPadding);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Cancel"))
		{
			quickAction = ActionType.None;
		}
		if (GUILayout.Button("Scale"))
		{
			foreach (VectorShape shape in shapes)
			{
				shape.ScaleBy(scaleValue);
			}

			Vector2 cameraPos = (Vector2)renderUtil.camera.transform.position * scaleValue;
			float cameraSize = renderUtil.camera.orthographicSize * scaleValue;
			SetCameraView(cameraSize, cameraPos);

			quickAction = ActionType.None;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}

	protected void SetFocusedShape(VectorShape shape)
	{
		focusedShape = shape;
		selection.Clear();

		if (shape != null)
		{
			if (!shapes.Contains(shape))
			{
				shapes.Add(shape);
			}
			selection.Add(shape);
		}
	}

	[MenuItem("Testing/Vector Shape Editor")]
	public static void OpenTestWindow()
	{
		VectorShapeEditor testWindow = EditorWindow.GetWindow(typeof(VectorShapeEditor)) as VectorShapeEditor;
		testWindow.titleContent.text = "New Shape";

		EllipseShape testEllipse = new EllipseShape(Vector2.one, 1f, 2f, 22.5f);
		testEllipse.colorOutline = Color.green;
		PolyShape testRect = new PolyShape(testEllipse.ShapeBounds);
		testWindow.Shapes = new List<VectorShape>() { testEllipse, testRect };

		testWindow.Focus();
	}
}
