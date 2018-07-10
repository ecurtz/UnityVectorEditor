using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class VectorShapeEditor : EditorWindow {

	private PreviewRenderUtility renderUtil = null;
	private Material renderMaterial = null;

	//protected static Texture2D panImage = null;
	//protected static Texture2D zoomImage = null;
	//protected static Texture2D moveImage = null;

	private static Rect RectUnion(Rect rectA, Rect rectB)
	{
		return Rect.MinMaxRect(Mathf.Min(rectA.xMin, rectB.xMin),
							   Mathf.Min(rectA.yMin, rectB.yMin),
		                       Mathf.Max(rectA.xMax, rectB.xMax),
		                       Mathf.Max(rectA.yMax, rectB.yMax));
	}

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
		}
		get
		{
			return shapes;
		}
	}

	protected VectorShape activeShape;
	public VectorShape ActiveShape
	{
		set
		{
			activeShape = value;
		}
		get
		{
			return activeShape;
		}
	}

	public void OnEnable()
	{
		//if (panImage == null) panImage = EditorGUIUtility.FindTexture("ViewToolMove");
		//if (zoomImage == null) zoomImage = EditorGUIUtility.FindTexture("ViewToolZoom");
		//if (moveImage == null) moveImage = EditorGUIUtility.FindTexture("MoveTool");

		renderMaterial = new Material(Shader.Find("Unlit/Vector"));

		renderUtil = new PreviewRenderUtility();
		renderUtil.camera.orthographic = true;
		renderUtil.camera.orthographicSize = 1f;
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

		if (guiEvent.type == EventType.Repaint)
		{
			//GUILayout.BeginArea(toolRect);
			//GUILayout.BeginHorizontal();
			//GUILayout.Toolbar(0, images, EditorStyles.toolbarButton);
			//GUILayout.EndHorizontal();
			//GUILayout.EndArea();

			renderUtil.BeginPreview(viewRect, GUIStyle.none);
			// renderUtil.camera.backgroundColor is reset in BeginPreview()
			//renderUtil.camera.backgroundColor = Color.yellow;

			foreach (VectorShape shape in shapes)
			{
				renderUtil.DrawMesh(shape.ShapeMesh, Matrix4x4.identity, renderMaterial, 0);
			}
			renderUtil.Render();

			Handles.SetCamera(renderUtil.camera);
			VectorShape.handleDrawSize = HandleUtility.GetHandleSize(Vector3.zero) * viewRect.height / viewRect.width;

			if (activeShape != null)
			{
				activeShape.DrawEditorHandles(true);
			}

			renderUtil.EndAndDrawPreview(viewRect);
		}
		else if (guiEvent.isScrollWheel && (EditorWindow.mouseOverWindow == this))
		{
			float zoomFactor = HandleUtility.niceMouseDeltaZoom;
			renderUtil.camera.orthographicSize *= (1 - zoomFactor * .02f);

			guiEvent.Use();
		}
		else if (guiEvent.isMouse)
		{
			Camera camera = renderUtil.camera;
			float viewScale = camera.orthographicSize * 2f / viewRect.height;
			bool handled = false;

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

				if (activeShape != null)
				{
					handled = activeShape.HandleEditorEvent(guiEvent, true);
				}

				guiEvent.mousePosition = mousePosition;
				guiEvent.delta = delta;
			}

			if (handled)
			{
				guiEvent.Use();
			}
		}
	}

	void Update()
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

		CircleShape testCircle1 = new CircleShape(new Vector2(0, 2), 1f);
		testCircle1.colorOutline = Color.red;
		CircleShape testCircle2 = new CircleShape(new Vector2(2, 0), 1f);
		testCircle2.colorOutline = Color.green;
		testCircle2.colorFill = Color.yellow;

		PointShape testPoint = new PointShape(0, 0);
		testPoint.colorOutline = Color.cyan;

		PolyShape testPoly = new PolyShape(new Vector2(0, -1.5f), 2, 5);
		testPoly.vertices[0].segmentCurves = true;
		testPoly.vertices[1].segmentCurves = true;
		testPoly.colorOutline = Color.white;
		testPoly.colorFill = Color.blue;
		testPoly.closed = false;

		testPoint.TranslateBy(new Vector2(0, 1));
		testCircle1.RotateAround(new Vector2(1, 2), 180);
		testCircle2.TransformBy(Unity.VectorGraphics.Matrix2D.Scale(new Vector2(.5f, .5f)));
		testPoly.TransformBy(Unity.VectorGraphics.Matrix2D.Scale(new Vector2(-1f, 1f)));

		testWindow.Shapes = new List<VectorShape>() { testCircle1, testCircle2, testPoint, testPoly };
		testWindow.ActiveShape = testPoly;
	}
}
