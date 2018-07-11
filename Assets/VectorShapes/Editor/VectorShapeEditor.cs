using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class VectorShapeEditor : EditorWindow {

	private PreviewRenderUtility renderUtil = null;
	private Material renderMaterial = null;

	private static Rect RectUnion(Rect rectA, Rect rectB)
	{
		return Rect.MinMaxRect(Mathf.Min(rectA.xMin, rectB.xMin),
							   Mathf.Min(rectA.yMin, rectB.yMin),
		                       Mathf.Max(rectA.xMax, rectB.xMax),
		                       Mathf.Max(rectA.yMax, rectB.yMax));
	}

	// For some reason EventCommandNames is internal
	protected const string Cmd_Delete = "Delete";
	protected const string Cmd_SelectAll = "SelectAll";

	//protected static Texture2D panImage = null;
	//protected static Texture2D zoomImage = null;
	//protected static Texture2D moveImage = null;

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

	protected List<VectorShape> selection = new List<VectorShape>();
	public List<VectorShape> Selection
	{
		set
		{
			selection = value;

			Repaint();
		}
		get
		{
			return selection;
		}
	}

	public Color backgroundColor = Color.gray;

	public void OnEnable()
	{
		//if (panImage == null) panImage = EditorGUIUtility.FindTexture("ViewToolMove");
		//if (zoomImage == null) zoomImage = EditorGUIUtility.FindTexture("ViewToolZoom");
		//if (moveImage == null) moveImage = EditorGUIUtility.FindTexture("MoveTool");

		renderMaterial = new Material(Shader.Find("Unlit/Vector"));

		renderUtil = new PreviewRenderUtility();
		renderUtil.camera.orthographic = true;
		renderUtil.camera.orthographicSize = 1f;
		renderUtil.camera.clearFlags = CameraClearFlags.SolidColor;
		renderUtil.camera.nearClipPlane = 0.1f;
		renderUtil.camera.farClipPlane = 100.0f;
		renderUtil.camera.transform.position = new Vector3(0, 0, -1);
	}

	public void OnGUI()
	{
		Event guiEvent = Event.current;

		Rect toolRect = EditorGUILayout.BeginHorizontal(/*EditorStyles.toolbar*/);
		//Texture[] images = new Texture2D[] { panImage, zoomImage, moveImage };
		//GUILayout.Toolbar(0, images);
		EditorGUILayout.LabelField("This is where the GUI goes");
		EditorGUILayout.EndHorizontal();

		Rect viewRect = new Rect(0, toolRect.yMax, this.position.width, this.position.height - toolRect.yMax);

		ViewTool activeTool = ViewTool.None;
		if (guiEvent.isScrollWheel)
		{
			activeTool = ViewTool.Zoom;
		}
		if (guiEvent.alt)
		{
			if (guiEvent.control)
			{
				activeTool = ViewTool.Zoom;
			}
			else
			{
				activeTool = ViewTool.Pan;
			}
		}

		MouseCursor activeCursor = MouseCursor.Arrow;
		switch (activeTool)
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
			//GUILayout.BeginArea(toolRect);
			//GUILayout.BeginHorizontal();
			//GUILayout.Toolbar(0, images, EditorStyles.toolbarButton);
			//GUILayout.EndHorizontal();
			//GUILayout.EndArea();

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
				selected.DrawEditorHandles(true);
			}

			renderUtil.EndAndDrawPreview(viewRect);
		}
		else if (guiEvent.isScrollWheel && (EditorWindow.mouseOverWindow == this))
		{
			float zoomFactor = HandleUtility.niceMouseDeltaZoom;
			renderUtil.camera.orthographicSize *= (1 - zoomFactor * .02f);

			handled = true;
		}
		else if (guiEvent.isMouse)
		{
			Camera camera = renderUtil.camera;
			float viewScale = camera.orthographicSize * 2f / viewRect.height;

			if (activeTool == ViewTool.Pan)
			{
				camera.transform.position += (Vector3) (guiEvent.delta * new Vector2(-viewScale, viewScale));
			
				handled = true;
			}
			else if (activeTool == ViewTool.Zoom)
			{
				float zoomFactor = HandleUtility.niceMouseDeltaZoom;
				renderUtil.camera.orthographicSize *= (1 + zoomFactor * .005f);

				handled = true;
			}

			if (!handled)
			{
				// Convert the event to shape space for convenience
				Vector2 mousePosition = guiEvent.mousePosition;
				Vector2 delta = guiEvent.delta;
				Vector2 cameraPos = camera.transform.position;
				Vector2 viewPos = mousePosition - viewRect.center;

				guiEvent.mousePosition = new Vector2(
					viewPos.x * viewScale + cameraPos.x,
					viewPos.y * -viewScale + cameraPos.y
				);
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
						Debug.Log("ValidateCommand Delete");
						handled = true;
					}
					break;
				case Cmd_SelectAll:
					if (shapes.Count > 0)
					{
						Debug.Log("ValidateCommand SelectAll");
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
			}
		}

		if (handled)
		{
			guiEvent.Use();
		}
	}

	public void Update()
	{
		// HACK this is the ONLY way to actually get new GUI events
		// to determine if the modifier keys are down.
		// http://www.isthenewinputhereyet.com
		if (EditorWindow.mouseOverWindow == this)
		{
			Repaint();
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

		PolyShape testPoly3 = new PolyShape(new Vector2(1, 2), 0.4f, 3);
		testPoly3.colorOutline = Color.black;
		PolyShape testPoly4 = new PolyShape(new Vector2(1, 1), 0.4f, 4);
		testPoly4.colorOutline = Color.black;
		testPoly4.RotateAround(new Vector2(1, 1), 45);
		PolyShape testPoly5 = new PolyShape(new Vector2(1, 0), 0.4f, 5);
		testPoly5.colorOutline = Color.black;
		PolyShape testPoly6 = new PolyShape(new Vector2(1, -1), 0.4f, 6);
		testPoly6.colorOutline = Color.black;
		testPoly6.RotateAround(new Vector2(1, -1), 30);

		PolyShape testShape = new PolyShape(new Vector2(2, 0), 0.4f, 4);
		testShape.colorOutline = Color.black;
		for (int i = 0; i < testShape.vertices.Length; i++)
		{
			testShape.vertices[i].segmentCurves = true;
		}

		testWindow.backgroundColor = Color.white;
		testWindow.Shapes = new List<VectorShape>() { testPoint, testLine, testCircle, testPoly3, testPoly4, testPoly5, testPoly6, testShape };
		testWindow.Selection = new List<VectorShape>() { testLine, testShape, testPoly5 };
		testWindow.Focus();
	}
}
