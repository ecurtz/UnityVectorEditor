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
		Select,
		Brush,
		Shape
	}
	protected const int ToolView = (int)ToolSet.View;
	protected const int ToolSelect = (int)ToolSet.Select;
	protected const int ToolBrush = (int)ToolSet.Brush;
	protected const int ToolShape = (int)ToolSet.Shape;
	const int ToolSetCount = 4;
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
	protected const int SelectionObject = (int)SelectionSet.Object;
	protected const int SelectionComponent = (int)SelectionSet.Component;
	protected const int SelectionVertex = (int)SelectionSet.Vertex;
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

	protected PreviewRenderUtility renderUtil = null;

	/// <summary>
	/// Texture for control seperator.
	/// </summary>
	protected static Texture2D separatorTexture;
	const int separatorWidth = 2;

	/// <summary>
	/// Miscellaneous GUI content.
	/// </summary>
	protected static GUIContent iconShapeStroke;
	protected static GUIContent iconShapeFill;

	public Color shapeOutlineColor = Color.black;
	public Color shapeFillColor = Color.clear;

	/// <summary>
	/// Window state.
	/// </summary>
	protected ViewTool activeViewTool;

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
			shapeIcons[ShapePoint] = new GUIContent(icon, "Create Point");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconCircle, renderUtil);
			shapeIcons[ShapeCircle] = new GUIContent(icon, "Create Circle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconTriangle, renderUtil);
			shapeIcons[ShapeTriangle] = new GUIContent(icon, "Create Triangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSquare, renderUtil);
			shapeIcons[ShapeRectangle] = new GUIContent(icon, "Create Rectangle");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPentagon, renderUtil);
			shapeIcons[ShapePentagon] = new GUIContent(icon, "Create Pentagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconHexagon, renderUtil);
			shapeIcons[ShapeHexagon] = new GUIContent(icon, "Create Hexagon");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconPolygon, renderUtil);
			shapeIcons[ShapePolygon] = new GUIContent(icon, "Create Polygon");
		}

		if (selectionIcons == null)
		{
			selectionIcons = new GUIContent[SelectionSetCount];

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectObject, renderUtil);
			selectionIcons[SelectionObject] = new GUIContent(icon, "Select Shapes");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectSegment, renderUtil);
			selectionIcons[SelectionComponent] = new GUIContent(icon, "Select Segments");

			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconSelectVertex, renderUtil);
			selectionIcons[SelectionVertex] = new GUIContent(icon, "Select Vertices");
		}

		if (iconShapeStroke == null)
		{
			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconCircle, renderUtil);
			iconShapeStroke = new GUIContent(icon, "Outline Color");
		}

		if (iconShapeFill == null)
		{
			icon = VectorShapeIcons.GetIcon(VectorShapeIcons.iconFilledCircle, renderUtil);
			iconShapeFill = new GUIContent(icon, "Fill Color");
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
				bounds = Rect.zero;
			}
			foreach (VectorShape shape in shapes)
			{
				bounds = RectUnion(bounds, shape.ShapeBounds);
			}

			renderUtil.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -1);
			renderUtil.camera.orthographicSize = bounds.height * 0.6f;

			float penSize = bounds.height / 5f;
			foreach (VectorShape shape in shapes)
			{
				shape.penSize = penSize;
				shape.Dirty = true;
			}

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

	protected void OnToolbarArea(Event guiEvent, Rect guiRect)
	{
		int activeButton = (int)activeTool;
		if ((activeTool != ToolSet.View) && (activeViewTool != ViewTool.None))
		{
			activeButton = ToolView;
		}

		int newButton = GUI.Toolbar(guiRect, activeButton, toolIcons);
		if (newButton != activeButton)
		{
			switch (newButton)
			{
				case ToolView:
					activeTool = ToolSet.View;
					break;
				case ToolSelect:
					activeTool = ToolSet.Select;
					break;
				case ToolBrush:
					SetFocusedShape(null);
					activeTool = ToolSet.Brush;
					break;
				case ToolShape:
					SetFocusedShape(null);
					activeTool = ToolSet.Shape;
					break;
			}
		}
	}

	protected void OnShapeColorArea(Event guiEvent, Rect guiRect)
	{
		// HACK - this is ridiculous, may as well hard code everything
		GUILayout.BeginArea(guiRect);
		EditorGUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		GUILayout.Box(iconShapeStroke, GUIStyle.none);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		shapeOutlineColor = EditorGUILayout.ColorField(shapeOutlineColor, GUILayout.MaxWidth(colorFieldWidth));
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		GUILayout.FlexibleSpace();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		GUILayout.Box(iconShapeFill, GUIStyle.none);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		shapeFillColor = EditorGUILayout.ColorField(shapeFillColor, GUILayout.MaxWidth(colorFieldWidth));
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		GUILayout.FlexibleSpace();

		EditorGUILayout.EndHorizontal();
		GUILayout.EndArea();
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
			switch (newButton)
			{
				case SelectionObject:
					activeSelect = SelectionSet.Object;
					break;
				case SelectionComponent:
					activeSelect = SelectionSet.Component;
					break;
				case SelectionVertex:
					activeSelect = SelectionSet.Vertex;
					break;
			}
		}

		if (focusedShape != null)
		{
			shapeOutlineColor = focusedShape.colorOutline;
			shapeFillColor = focusedShape.colorFill;
		}
		Color currentOutline = shapeOutlineColor;
		Color currentFill = shapeFillColor;

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
			switch (newButton)
			{
				case ShapePoint:
					activeShape = ShapeSet.Point;
					break;
				case ShapeCircle:
					activeShape = ShapeSet.Circle;
					break;
				case ShapeTriangle:
					activeShape = ShapeSet.Triangle;
					break;
				case ShapeRectangle:
					activeShape = ShapeSet.Rectangle;
					break;
				case ShapePentagon:
					activeShape = ShapeSet.Pentagon;
					break;
				case ShapeHexagon:
					activeShape = ShapeSet.Hexagon;
					break;
				case ShapePolygon:
					activeShape = ShapeSet.Polygon;
					break;
			}

			SetFocusedShape(null);
		}

		Rect colorsRect = new Rect(toolbarRect.xMax, guiRect.position.y, guiRect.xMax - toolbarRect.xMax, guiRect.height);
		OnShapeColorArea(guiEvent, colorsRect);
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

		Rect toolbarRect = new Rect(toolbarPadding * 2, toolbarPadding, toolbarWidth * toolIcons.Length, toolbarHeight);
		OnToolbarArea(guiEvent, toolbarRect);

		Rect separatorRect = new Rect(toolbarRect.xMax + toolbarPadding, 0, separatorWidth, toolRect.height);
		GUI.DrawTexture(separatorRect, separatorTexture);

		Rect infoRect = new Rect(separatorRect.xMax, 0, toolRect.width - separatorRect.xMax, toolRect.height);
		switch (activeTool)
		{
			case ToolSet.View:
				OnInfoAreaView(guiEvent, infoRect);
				break;
			case ToolSet.Select:
				OnInfoAreaSelect(guiEvent, infoRect);
				break;
			case ToolSet.Brush:
				OnInfoAreaBrush(guiEvent, infoRect);
				break;
			case ToolSet.Shape:
				OnInfoAreaShape(guiEvent,  infoRect);
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
				//Debug.Log(shape.ShapeMesh.vertexCount);
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
				PolyShape newLine = new PolyShape(new Vector2[] { shapePosition });
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

			PointShape newPoint = new PointShape(shapePosition);
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
			CircleShape newCircle = new CircleShape(shapePosition, 0f);
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
			PolyShape newPoly = new PolyShape(shapePosition, minPolyRadius, sides);
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
			PolyShape newRect = new PolyShape(shapePosition, 0f, 4);
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

				PolyShape newPoly = new PolyShape(points);
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
		/*

		PointShape testPoint = new PointShape(-2, 0);
		testPoint.colorOutline = Color.black;

		PolyShape testLine = new PolyShape(new Vector2(-1, 0), 0.4f, 4);
		testLine.colorOutline = Color.black;
		for (int i = 0; i < testLine.vertices.Length; i++)
		{
			testLine.vertices[i].segmentCurves = true;
		}
		testLine.closed = false;

		CircleShape testCircle = new CircleShape(new Vector2(0, 0), 0.4f);
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
		*/

		//EllipseShape testEllipse = new EllipseShape(Vector2.zero, 1f, 2f, 22.5f);
		//testEllipse.colorOutline = Color.green;
		//PolyShape testRect = new PolyShape(testEllipse.ShapeBounds);
		//testWindow.Shapes = new List<VectorShape>() { testEllipse, testRect };

		//TextAsset asset = Resources.Load("Flipper_Left_SVG") as TextAsset;
		//testWindow.Shapes = VectorShapeFilesSVG.ReadSVG(new System.IO.StringReader(asset.text));

		//TextAsset asset = Resources.Load("InlineDrop_DXF") as TextAsset;
		//TextAsset asset = Resources.Load("Saucer_DXF") as TextAsset;
		//TextAsset asset = Resources.Load("LaneGuide_1500_DXF") as TextAsset;
		TextAsset asset = Resources.Load("Playfield_DXF") as TextAsset;
		//TextAsset asset = Resources.Load("ThreeTarget_DXF") as TextAsset;
		testWindow.Shapes = VectorShapeFilesDXF.ReadDXF(new System.IO.MemoryStream(asset.bytes));

		testWindow.Focus();
	}
}
