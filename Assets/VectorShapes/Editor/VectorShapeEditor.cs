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
		None = -1,
		View,
		Move,
		Select,
		Brush,
		Shape
	}
	protected const int ToolView = (int)ToolSet.View;
	protected const int ToolMove = (int)ToolSet.Move;
	protected const int ToolSelect = (int)ToolSet.Select;
	protected const int ToolBrush = (int)ToolSet.Brush;
	protected const int ToolShape = (int)ToolSet.Shape;
	const int ToolSetCount = 5;
	protected static GUIContent[] toolIcons;

	/// <summary>
	/// Shapes for vector editor
	/// </summary>
	protected enum ShapeSet
	{
		None = -1,
		Point,
		Circle,
		Triangle,
		Rectangle,
		Pentagon,
		Hexagon,
		Polygon
	}

	protected const int ShapePoint = (int)ShapeSet.Point;
	protected const int ShapeCircle = (int)ShapeSet.Circle;
	protected const int ShapeTriangle = (int)ShapeSet.Triangle;
	protected const int ShapeRectangle = (int)ShapeSet.Rectangle;
	protected const int ShapePentagon = (int)ShapeSet.Pentagon;
	protected const int ShapeHexagon = (int)ShapeSet.Hexagon;
	protected const int ShapePolygon = (int)ShapeSet.Polygon;
	const int ShapeSetCount = 7;
	protected static GUIContent[] shapeIcons;

	/// <summary>
	/// Selection levels for vector editor
	/// </summary>
	protected enum SelectionSet
	{
		None = -1,
		Object,
		Component,
		Vertex
	}
	const int SelectionSetCount = 3;
	protected static GUIContent[] selectionIcons;

	/// <summary>
	/// Behavior of selection operations
	/// </summary>
	protected enum SelectionType
	{
		Normal,
		Additive,
		Subtractive
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

	/// <summary>
	/// Texture for control seperator.
	/// </summary>
	protected static Texture2D separatorTexture;
	const int separatorWidth = 2;

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

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconView, renderUtil);
			toolIcons[ToolView] = new GUIContent(icon, "View Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconArrowAll, renderUtil);
			toolIcons[ToolMove] = new GUIContent(icon, "Move Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectionRect, renderUtil);
			toolIcons[ToolSelect] = new GUIContent(icon, "Selection Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconBrush, renderUtil);
			toolIcons[ToolBrush] = new GUIContent(icon, "Draw Tool");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconShapes, renderUtil);
			toolIcons[ToolShape] = new GUIContent(icon, "Shape Tool");
		}

		if (shapeIcons == null)
		{
			shapeIcons = new GUIContent[ShapeSetCount];

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPoint, renderUtil);
			shapeIcons[(int)ShapeSet.Point] = new GUIContent(icon, "Create Point");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconCircle, renderUtil);
			shapeIcons[(int)ShapeSet.Circle] = new GUIContent(icon, "Create Circle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconTriangle, renderUtil);
			shapeIcons[(int)ShapeSet.Triangle] = new GUIContent(icon, "Create Triangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSquare, renderUtil);
			shapeIcons[(int)ShapeSet.Rectangle] = new GUIContent(icon, "Create Rectangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPentagon, renderUtil);
			shapeIcons[(int)ShapeSet.Pentagon] = new GUIContent(icon, "Create Pentagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconHexagon, renderUtil);
			shapeIcons[(int)ShapeSet.Hexagon] = new GUIContent(icon, "Create Hexagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconOctagon, renderUtil);
			shapeIcons[(int)ShapeSet.Polygon] = new GUIContent(icon, "Create Polygon");
		}
	}

	/// <summary>
	/// Union of two Rects
	/// </summary>
	/// <returns>The union.</returns>
	/// <param name="rectA">Rect A</param>
	/// <param name="rectB">Rect B</param>
	private static Rect RectUnion(Rect rectA, Rect rectB)
	{
		return Rect.MinMaxRect(Mathf.Min(rectA.xMin, rectB.xMin),
							   Mathf.Min(rectA.yMin, rectB.yMin),
		                       Mathf.Max(rectA.xMax, rectB.xMax),
		                       Mathf.Max(rectA.yMax, rectB.yMax));
	}

	protected ViewTool activeViewTool;
	protected ToolSet activeTool;
	protected ShapeSet activeShape;

	protected VectorShape focusedShape;

	protected GUIContent[] toolbar = new GUIContent[ToolSetCount];

	protected PreviewRenderUtility renderUtil = null;

	protected List<VectorShape> shapes = new List<VectorShape>();
	public List<VectorShape> Shapes
	{
		set
		{
			shapes = value;

			Rect bounds = Rect.zero;
			foreach (VectorShape shape in shapes)
			{
				bounds = RectUnion(bounds, shape.ShapeBounds);
			}

			renderUtil.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -1);
			renderUtil.camera.orthographicSize = bounds.height * 0.6f;
			Repaint();
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

	public Color backgroundColor = Color.Lerp(Color.black, Color.white, 0.9f);

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

	public Vector2 MouseToShapePoint(Vector2 mousePoint)
	{
		return mouseToShapeMatrix * mousePoint;
	}

	public void OnEnable()
	{
		renderUtil = new PreviewRenderUtility();
		renderUtil.camera.orthographic = true;
		renderUtil.camera.orthographicSize = 1f;
		renderUtil.camera.clearFlags = CameraClearFlags.SolidColor;
		renderUtil.camera.nearClipPlane = 0.1f;
		renderUtil.camera.farClipPlane = 100.0f;
		renderUtil.camera.transform.position = new Vector3(0, 0, -1);

		InitializeGUIContent();

		EditorApplication.modifierKeysChanged += OnModifierKeys;
		this.wantsMouseMove = true;
	}

	public void OnDisable()
	{
		renderUtil.Cleanup();
		EditorApplication.modifierKeysChanged -= OnModifierKeys;
	}

	public void OnModifierKeys()
	{
		SendEvent(EditorGUIUtility.CommandEvent(Cmd_ModifierKeysChanged));
	}

	const int toolbarPadding = 5;
	const int toolbarHeight = 22;
	const int toolbarWidth = 32;
	protected void OnToolbarArea(Event guiEvent, Rect guiRect)
	{
		toolbar[ToolView] = toolIcons[(int)ToolSet.View];
		toolbar[ToolMove] = toolIcons[(int)ToolSet.Move];
		toolbar[ToolSelect] = toolIcons[(int)ToolSet.Select];
		toolbar[ToolBrush] = toolIcons[(int)ToolSet.Brush];
		toolbar[ToolShape] = toolIcons[(int)ToolSet.Shape];

		int activeButton = (int)activeTool;
		if ((activeTool != ToolSet.View) && (activeViewTool != ViewTool.None))
		{
			activeButton = ToolView;
		}

		int newButton = GUI.Toolbar(guiRect, activeButton, toolbar);
		if (newButton != activeButton)
		{
			switch (newButton)
			{
				case ToolView:
					activeTool = ToolSet.View;
					break;
				case ToolMove:
					activeTool = ToolSet.Move;
					break;
				case ToolSelect:
					activeTool = ToolSet.Select;
					break;
				case ToolBrush:
					activeTool = ToolSet.Brush;
					break;
				case ToolShape:
					activeTool = ToolSet.Shape;
					break;
			}
		}
	}

	//protected void OnPopupArea(Event guiEvent, Rect guiRect)
	//{
	//	popup[PopupPoint] = shapeIcons[(int)ShapeSet.Point];
	//	popup[PopupCircle] = shapeIcons[(int)ShapeSet.Circle];
	//	popup[PopupTriangle] = shapeIcons[(int)ShapeSet.Triangle];
	//	popup[PopupRectangle] = shapeIcons[(int)ShapeSet.Rectangle];
	//	popup[PopupPentagon] = shapeIcons[(int)ShapeSet.Pentagon];
	//	popup[PopupHexagon] = shapeIcons[(int)ShapeSet.Hexagon];
	//	popup[PopupPolygon] = shapeIcons[(int)ShapeSet.Polygon];

	//	// For some reason the button doesn't actually fill the specified rect
	//	popupStyle = new GUIStyle("LargeDropDown");
	//	popupStyle.overflow.bottom = 1;
	//	//popupStyle.normal = popupStyle.onNormal;
	//	if (EditorGUI.DropdownButton(guiRect, toolIcons[0], FocusType.Passive, popupStyle))
	//	{
	//		activeTool = ToolSet.None;

	//		PopupWindow.Show(guiRect, new SingleSelectionPopup(0, popup));
	//	}
	//}

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
			backgroundColor = EditorGUILayout.ColorField(backgroundColor, GUILayout.MaxWidth(40f));

			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
		}

		GUILayout.EndArea();
	}

	public void OnGUI()
	{
		Event guiEvent = Event.current;

		activeViewTool = ViewTool.None;
		if (guiEvent.isScrollWheel)
		{
			activeViewTool = ViewTool.Zoom;
		}
		else if ((activeTool == ToolSet.View) || (guiEvent.alt))
		{
			if (guiEvent.control)
			{
				activeViewTool = ViewTool.Zoom;
			}
			else
			{
				activeViewTool = ViewTool.Pan;
			}
		}

		if (guiEvent.type == EventType.MouseDown)
		{
			mouseDownPosition = guiEvent.mousePosition;
			mouseDownTime = Time.time;
			mouseIsDown = true;
		}
		else if (guiEvent.type == EventType.MouseUp)
		{
			mouseIsDown = false;
		}

		Rect toolRect = new Rect(0, 0, this.position.width, toolbarHeight + toolbarPadding * 2);

		Rect toolbarRect = new Rect(toolbarPadding * 2, toolbarPadding, toolbarWidth * toolbar.Length, toolbarHeight);
		OnToolbarArea(guiEvent, toolbarRect);

		Rect separatorRect = new Rect(toolbarRect.xMax + toolbarPadding, 0, separatorWidth, toolRect.height);
		GUI.DrawTexture(separatorRect, separatorTexture);

		Rect infoRect = new Rect(separatorRect.xMax, 0, toolRect.width - separatorRect.xMax, toolRect.height);
		switch (activeTool)
		{
			default:
				OnInfoAreaView(guiEvent, infoRect);
				break;
		}

		Rect viewRect = new Rect(0, toolRect.yMax, this.position.width, this.position.height - toolRect.yMax);

		MouseCursor activeCursor = MouseCursor.Arrow;
		switch (activeViewTool)
		{
			case ViewTool.Zoom:
				activeCursor = MouseCursor.Zoom;
				break;
			case ViewTool.Pan:
				activeCursor = MouseCursor.Pan;
				break;
		}
		EditorGUIUtility.AddCursorRect(viewRect, activeCursor);

		bool handled = false;
		if (guiEvent.type == EventType.Repaint)
		{
			renderUtil.BeginPreview(viewRect, GUIStyle.none);
			// renderUtil.camera.backgroundColor is reset in BeginPreview()
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
		}
		else if (guiEvent.isScrollWheel && (EditorWindow.mouseOverWindow == this))
		{
			float zoomFactor = HandleUtility.niceMouseDeltaZoom;
			renderUtil.camera.orthographicSize *= (1 - zoomFactor * .02f);

			// Update matrix from mouse to shape space
			mouseToShapeScale = renderUtil.camera.orthographicSize * 2f / viewRect.height;
			mouseToShapeMatrix =
				Matrix2D.Translate(renderUtil.camera.transform.position) *
		        Matrix2D.Scale(new Vector2(mouseToShapeScale, -mouseToShapeScale)) *
				Matrix2D.Translate(-viewRect.center);

			handled = true;
		}
		else if (guiEvent.isMouse)
		{
			// Update mouse position and matrix from mouse to shape space
			mouseToShapeScale = renderUtil.camera.orthographicSize * 2f / viewRect.height;
			mousePosition = guiEvent.mousePosition;
			mouseToShapeMatrix =
				Matrix2D.Translate(renderUtil.camera.transform.position) *
		        Matrix2D.Scale(new Vector2(mouseToShapeScale, -mouseToShapeScale)) *
            	Matrix2D.Translate(-viewRect.center);

			mouseInContent = (viewRect.Contains(mousePosition));

			if (activeViewTool != ViewTool.None)
			{
				handled = OnViewToolMouse(guiEvent);
			}
			else switch (activeTool)
			{
				case ToolSet.Move:
					handled = OnMoveToolMouse(guiEvent);
					break;
				case ToolSet.Select:
					handled = OnSelectToolMouse(guiEvent);
					break;
				case ToolSet.Brush:
					handled = OnBrushToolMouse(guiEvent);
					break;
				case ToolSet.Shape:
					handled = OnShapeToolMouse(guiEvent);
					break;
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

		if (handled)
		{
			guiEvent.Use();
		}
	}

	protected bool OnViewToolMouse(Event guiEvent)
	{
		if (guiEvent.type == EventType.MouseDrag)
		{
			if ((activeViewTool == ViewTool.Pan) && mouseInContent)
			{
				renderUtil.camera.transform.position += (Vector3)(guiEvent.delta * new Vector2(-mouseToShapeScale, mouseToShapeScale));
			}
			else if (activeViewTool == ViewTool.Zoom)
			{
				float zoomFactor = HandleUtility.niceMouseDeltaZoom;
				renderUtil.camera.orthographicSize *= (1 + zoomFactor * .005f);
			}
		}

		return true;
	}

	protected bool OnMoveToolMouse(Event guiEvent)
	{
		return false;
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

	protected bool OnPointToolMouse(Event guiEvent)
	{
		if (guiEvent.type == EventType.MouseDown)
		{
			PointShape newPoint = new PointShape(MouseToShapePoint(mousePosition));
			SetFocusedShape(newPoint);

			Repaint();
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
				PolyShape newLine = new PolyShape(new Vector2[] { shapePosition });
				newLine.closed = false;
				SetFocusedShape(newLine);
			}
			else
			{
				activeLine.AppendVertex(shapePosition);
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
		if (guiEvent.type == EventType.MouseUp)
		{
			focusedShape = null;
		}
		else switch (activeShape)
		{
			case ShapeSet.Point:
				OnShapePointMouse(guiEvent);
				break;
			case ShapeSet.Circle:
				OnShapeCircleMouse(guiEvent);
				break;
		}

		return true;
	}

	protected void OnShapePointMouse(Event guiEvent)
	{
		if (guiEvent.type == EventType.MouseDown)
		{
			Vector2 shapePosition = MouseToShapePoint(mousePosition);

			PointShape newPoint = new PointShape(shapePosition);
			SetFocusedShape(newPoint);

			Repaint();
		}
	}

	protected void OnShapeCircleMouse(Event guiEvent)
	{
		Vector2 shapePosition = MouseToShapePoint(mousePosition);

		if (guiEvent.type == EventType.MouseDown)
		{
			CircleShape newCircle = new CircleShape(shapePosition, 0f);
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
		testWindow.titleContent.text = "Testing...";

		PointShape testPoint = new PointShape(-2, 0);
		testPoint.colorOutline = Color.black;

		PolyShape testLine = new PolyShape(new Vector2(-1, 0), 0.4f, 4);
		testLine.colorOutline = Color.black;
		for (int i = 0; i < testLine.vertices.Length; i++)
		{
			testLine.vertices[i].segmentCurves = true;
		}
		testLine.closed = false;

		CircleShape testCircle = new CircleShape(new Vector2(0, 0), 0.65f);
		testCircle.colorOutline = Color.black;

		PolyShape testPoly3 = new PolyShape(new Vector2(1, 2), 0.45f, 3);
		testPoly3.colorOutline = Color.black;
		PolyShape testPoly4 = new PolyShape(new Vector2(1, 1), 0.50f, 4);
		testPoly4.colorOutline = Color.black;
		testPoly4.RotateAround(new Vector2(1, 1), 45);
		PolyShape testPoly5 = new PolyShape(new Vector2(1, 0), 0.45f, 5);
		testPoly5.colorOutline = Color.black;
		PolyShape testPoly6 = new PolyShape(new Vector2(1, -1), 0.45f, 6);
		testPoly6.colorOutline = Color.black;
		testPoly6.RotateAround(new Vector2(1, -1), 30);

		PolyShape testShape = new PolyShape(new Vector2(2, 0), 0.4f, 4);
		testShape.colorOutline = Color.black;
		for (int i = 0; i < testShape.vertices.Length; i++)
		{
			testShape.vertices[i].segmentCurves = true;
		}

		testWindow.Shapes = new List<VectorShape>() { testPoint, testLine, testCircle, testPoly3, testPoly4, testPoly5, testPoly6, testShape };
		testWindow.Focus();

		string tempPath = Application.dataPath + "/test.svg";
		System.IO.Stream stream = new System.IO.FileStream(tempPath, System.IO.FileMode.OpenOrCreate);
		VectorShapeSVGExporter exporter = new VectorShapeSVGExporter();
		Rect bounds = new Rect(-5, -5, 10, 10);
		exporter.Open(stream, bounds, VectorShapeSVGExporter.Unit.Centimeters);
		exporter.AddShapeGroup(testWindow.Shapes, "TestGroup");
		exporter.Close();
		stream.Close();
	}
}
