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
	public enum VectorTool
	{
		None = -1,
		View,
		Move,
		Rect,
		Point,
		Line,
		Circle,
		Poly,
		Shape
	}

	/// <remarks>
	/// For some reason EventCommandNames is internal, so redefine them here
	/// </remarks>
	protected const string Cmd_Delete = "Delete";
	protected const string Cmd_SelectAll = "SelectAll";
	protected const string Cmd_ModifierKeysChanged = "ModifierKeysChanged";

	protected const int IconViewPan = 0;
	protected const int IconViewZoom = 1;
	protected const int IconMove = 2;
	protected const int IconRect = 3;
	protected const int IconPoint = 4;
	protected const int IconSpline = 5;
	protected const int IconCircle = 6;
	protected const int IconPolygon = 7;
	protected const int IconPolyTri = 8;
	protected const int IconPolyRect = 9;
	protected const int IconPolyPent = 10;
	protected const int IconPolyHex = 11;
	protected const int IconShape = 12;

	protected const int IconCount = 13;

	protected const int ButtonView = 0;
	protected const int ButtonMove = 1;
	protected const int ButtonRect = 2;
	protected const int ButtonPoint = 3;
	protected const int ButtonSpline = 4;
	protected const int ButtonCircle = 5;
	protected const int ButtonPoly = 6;
	protected const int ButtonShape = 7;

	protected const int ButtonCount = 8;

	protected static Material renderMaterial;
	protected static GUIContent[] toolbarIcons;
	protected static GUIContent[] toolbarOnIcons;

	/// <summary>
	/// Initializes the GUI Content.
	/// </summary>
	private static void InitializeGUIContent()
	{
		if (renderMaterial == null)
		{
			renderMaterial = new Material(Shader.Find("Unlit/Vector"));
		}

		if (toolbarIcons == null)
		{
			toolbarIcons = new GUIContent[IconCount];
			toolbarIcons[IconViewPan] = EditorGUIUtility.IconContent("ViewToolMove", "|Hand Tool");
			toolbarIcons[IconViewZoom] = EditorGUIUtility.IconContent("ViewToolZoom", "|Zoom Tool");
			toolbarIcons[IconMove] = EditorGUIUtility.IconContent("MoveTool", "|Move Tool");
			toolbarIcons[IconRect] = EditorGUIUtility.IconContent("RectTool", "|Rect Tool");

			toolbarIcons[IconPoint] = EditorGUIUtility.IconContent("CreatePoint", "|Create Point");

			//VectorShape shape = new PointShape(Vector2.zero);
			//Sprite sprite = VectorUtils.BuildSprite(shape.ShapeGeometry, 1f, VectorUtils.Alignment.Center, Vector2.zero, 128);
			//Texture2D icon = VectorUtils.RenderSpriteToTexture2D(sprite, 18, 18, renderMaterial);
			//toolbarIcons[IconPoint] = new GUIContent(icon, "Create Point");

			toolbarIcons[IconSpline] = EditorGUIUtility.IconContent("CreateSpline", "|Create Spline");
			toolbarIcons[IconCircle] = EditorGUIUtility.IconContent("CreateCircle", "|Create Circle");
			toolbarIcons[IconPolygon] = EditorGUIUtility.IconContent("CreatePolygon", "|Create Polygon");
			toolbarIcons[IconPolyTri] = EditorGUIUtility.IconContent("CreateTriangle", "|Create Triangle");
			toolbarIcons[IconPolyRect] = EditorGUIUtility.IconContent("CreateRectangle", "|Create Rectangle");
			toolbarIcons[IconPolyPent] = EditorGUIUtility.IconContent("CreatePentagon", "|Create Pentagon");
			toolbarIcons[IconPolyHex] = EditorGUIUtility.IconContent("CreateHexagon", "|Create Hexagon");
			toolbarIcons[IconShape] = EditorGUIUtility.IconContent("CreateShape", "|Create Shape");
		}

		/*
		if (toolbarOnIcons == null)
		{
			toolbarOnIcons = new GUIContent[IconCount];
			toolbarOnIcons[IconViewPan] = EditorGUIUtility.IconContent("ViewToolMove On");
			toolbarOnIcons[IconViewZoom] = EditorGUIUtility.IconContent("ViewToolZoom On");
			toolbarOnIcons[IconMove] = EditorGUIUtility.IconContent("MoveTool On");
			toolbarOnIcons[IconRect] = EditorGUIUtility.IconContent("RectTool On");
			toolbarOnIcons[IconPoint] = EditorGUIUtility.IconContent("CreatePoint On");
			toolbarOnIcons[IconSpline] = EditorGUIUtility.IconContent("CreateSpline On");
			toolbarOnIcons[IconCircle] = EditorGUIUtility.IconContent("CreateCircle On");
			toolbarOnIcons[IconPolygon] = EditorGUIUtility.IconContent("CreateTri On");
			toolbarOnIcons[IconPolyTri] = EditorGUIUtility.IconContent("CreateTri On");
			toolbarOnIcons[IconPolyRect] = EditorGUIUtility.IconContent("CreateRect On");
			toolbarOnIcons[IconPolyPent] = EditorGUIUtility.IconContent("CreatePent On");
			toolbarOnIcons[IconPolyHex] = EditorGUIUtility.IconContent("CreateHex On");
			toolbarOnIcons[IconShape] = EditorGUIUtility.IconContent("CreateShape On");

			for (int i = 0; i < ButtonCount; i++)
			{
				toolbarOnIcons[i].text = toolbarIcons[i].text;
				toolbarOnIcons[i].tooltip = toolbarIcons[i].tooltip;
			}
		}
		*/
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

	protected int selectedTool = 0;
	protected int selectedPoly = 1;
	protected VectorTool activeTool;
	protected ViewTool activeViewTool;
	protected GUIContent[] toolbar = new GUIContent[ButtonCount];

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
	protected Matrix2D mouseToShapeMatrix;

	protected bool dragSelecting;
	protected Rect selectionRect;

	public Vector2 MouseToShapePoint(Vector2 mousePoint)
	{
		return mouseToShapeMatrix * mousePoint;
	}

	public void OnEnable()
	{
		InitializeGUIContent();

		renderUtil = new PreviewRenderUtility();
		renderUtil.camera.orthographic = true;
		renderUtil.camera.orthographicSize = 1f;
		renderUtil.camera.clearFlags = CameraClearFlags.SolidColor;
		renderUtil.camera.nearClipPlane = 0.1f;
		renderUtil.camera.farClipPlane = 100.0f;
		renderUtil.camera.transform.position = new Vector3(0, 0, -1);

		EditorApplication.modifierKeysChanged += OnModifierKeys;
		this.wantsMouseMove = true;
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
		toolbar[ButtonView] = toolbarIcons[IconViewPan];
		toolbar[ButtonMove] = toolbarIcons[IconMove];
		toolbar[ButtonRect] = toolbarIcons[IconRect];
		toolbar[ButtonPoint] = toolbarIcons[IconPoint];
		toolbar[ButtonSpline] = toolbarIcons[IconSpline];
		toolbar[ButtonCircle] = toolbarIcons[IconCircle];
		toolbar[ButtonPoly] = toolbarIcons[IconPolyHex];
		toolbar[ButtonShape] = toolbarIcons[IconShape];

		int selectedButton = selectedTool;
		if ((activeTool != VectorTool.View) && (activeViewTool != ViewTool.None))
		{
			selectedButton = ButtonView;
		}

		int newButton = GUI.Toolbar(guiRect, selectedButton, toolbar);
		if (newButton != selectedButton)
		{
			selectedTool = newButton;
			switch (newButton)
			{
				case ButtonView:
					activeTool = VectorTool.View;
					break;
				case ButtonMove:
					activeTool = VectorTool.Move;
					break;
				case ButtonRect:
					activeTool = VectorTool.Rect;
					break;
				case ButtonPoint:
					activeTool = VectorTool.Point;
					break;
				case ButtonSpline:
					activeTool = VectorTool.Line;
					break;
				case ButtonCircle:
					activeTool = VectorTool.Circle;
					break;
				case ButtonPoly:
					activeTool = VectorTool.Poly;
					Rect popupRect = new Rect(guiRect.xMin + toolbarWidth * ButtonPoly, guiRect.yMax, 1, 1);
					//PopupWindow.Show(popupRect, new SingleSelectionPopup(0, toolbar));
					break;
				case ButtonShape:
					activeTool = VectorTool.Shape;
					break;
			}
		}
	}

	protected void OnInfoArea(Event guiEvent, Rect guiRect)
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
		else if ((activeTool == VectorTool.View) || (guiEvent.alt))
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

		Rect toolRect = new Rect(0, 0, this.position.width, toolbarHeight + toolbarPadding * 2);

		Rect toolbarRect = new Rect(toolbarPadding * 2, toolbarPadding, toolbarWidth * toolbar.Length, toolbarHeight);
		OnToolbarArea(guiEvent, toolbarRect);

		Rect infoRect = new Rect(toolbarRect.xMax, toolRect.y, toolRect.width - toolbarRect.xMax, toolRect.height);
		OnInfoArea(guiEvent, infoRect);

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
				selected.DrawEditorHandles(true, (selection.Count == 1));
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
			float viewScale = renderUtil.camera.orthographicSize * 2f / viewRect.height;

			// Update matrix from mouse to shape space
			mouseToShapeMatrix =
				Matrix2D.Translate(renderUtil.camera.transform.position) *
				Matrix2D.Scale(new Vector2(viewScale, -viewScale)) *
				Matrix2D.Translate(-viewRect.center);

			handled = true;
		}
		else if (guiEvent.isMouse)
		{
			float viewScale = renderUtil.camera.orthographicSize * 2f / viewRect.height;

			// Update mouse position and matrix from mouse to shape space
			mousePosition = guiEvent.mousePosition;
			mouseToShapeMatrix =
				Matrix2D.Translate(renderUtil.camera.transform.position) *
            	Matrix2D.Scale(new Vector2(viewScale, -viewScale)) *
            	Matrix2D.Translate(-viewRect.center);

			if (activeViewTool != ViewTool.None)
			{
				if (guiEvent.type == EventType.MouseDrag)
				{
					if (activeViewTool == ViewTool.Pan)
					{
						renderUtil.camera.transform.position += (Vector3)(guiEvent.delta * new Vector2(-viewScale, viewScale));
					}
					else if (activeViewTool == ViewTool.Zoom)
					{
						float zoomFactor = HandleUtility.niceMouseDeltaZoom;
						renderUtil.camera.orthographicSize *= (1 + zoomFactor * .005f);
					}
				}

				handled = true;
			}

			selectionRect = Rect.zero;
			if (activeTool == VectorTool.Rect)
			{
				if (guiEvent.type == EventType.MouseDown)
				{
					dragSelecting = false;
					mouseDownPosition = guiEvent.mousePosition;
					previousSelection = selection;
					selection = new List<VectorShape>();
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

				handled = true;
			}

			if (!handled)
			{
				Vector2 delta = guiEvent.delta;
				guiEvent.mousePosition = MouseToShapePoint(guiEvent.mousePosition);
				guiEvent.delta = guiEvent.delta * new Vector2(viewScale, -viewScale);

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
		//testWindow.Selection = new List<VectorShape>() { testLine, testShape, testPoly5 };
		testWindow.Focus();
	}
}
