using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Unity.VectorGraphics;

/// <summary>
/// Base class of drawable vector based 2d elements.
/// </summary>
public abstract class VectorShape : System.IDisposable
{
	public class ShapeProxy : ScriptableObject, ISerializationCallbackReceiver
	{
		[HideInInspector]
		public VectorShape shape;

		[Range(0f, 25f)]
		public float penSize;

		public Color32 colorOutline;
		public Color32 colorFill;

		public void OnBeforeSerialize()
		{
			if (shape != null)
			{
				shape.penSize = penSize;
				shape.colorOutline = colorOutline;
				shape.colorFill = colorFill;
			}
		}

		public void OnAfterDeserialize()
		{
		}
	}

	public ShapeProxy GetShapeProxy()
	{
		ShapeProxy proxy = ScriptableObject.CreateInstance<ShapeProxy>();
		proxy.name = "Shape";
		proxy.shape = this;
		proxy.penSize = penSize;
		proxy.colorOutline = colorOutline;
		proxy.colorFill = colorFill;

		return proxy;
	}

	public class SnapPoint
	{
		[System.Flags]
		public enum Mode
		{
			None = 0,
			Center = 1,
			Endpoint = 2,
			Midpoint = 4,
			Edge = 8
		}
		public Mode mode = Mode.None;

		public Vector2 point;
	}

	/// <summary>
	/// Pen size for drawing.
	/// </summary>
	[Range(0f, 25f)]
	public float penSize = 2f;

	/// <summary>
	/// Scale between pen and mesh units.
	/// </summary>
	public float penToMeshScale = 0.01f;

	/// <summary>
	/// Outline color.
	/// </summary>
	public Color colorOutline = Color.black;

	/// <summary>
	/// Fill color.
	/// </summary>
	public Color colorFill = Color.clear;

	/// <summary>
	/// Does the last vertex connect back to the first?
	/// </summary>
	[SerializeField]
	protected bool closed = true;

	/// <summary>
	/// ID.
	/// </summary>
	[SerializeField]
	protected string guid = new System.Guid().ToString();

	/// <summary>
	/// Transform matrix.
	/// </summary>
	[SerializeField]
	protected Matrix2D matrixTransform = Matrix2D.identity;

	/// <summary>
	/// Geometry data generated for shape.
	/// </summary>
	protected List<VectorUtils.Geometry> shapeGeometry = null;

	/// <summary>
	/// Scene data generated for shape.
	/// </summary>
	protected SceneNode shapeNode = null;

	/// <summary>
	/// Mesh generated for tesselated shape.
	/// </summary>
	protected Mesh shapeMesh = null;

	/// <summary>
	/// Line renderer mesh for shape outline.
	/// </summary>
	protected Mesh lineMesh = null;

	/// <summary>
	/// Bounds rectangle for shape (may be approximate for some shapes).
	/// </summary>
	protected Rect shapeBounds = Rect.zero;

	protected bool shapeDirty = true;
	protected bool lineDirty = true;
	protected bool boundsDirty = true;

	/// <summary>
	/// Level To which the shape selected.
	/// </summary>
	public enum SelectionLevel
	{
		None,
		Shape,
		Component,
		Vertex
	}

#if UNITY_EDITOR
	/// <summary>
	/// Shared texture for drawing AA lines in the editor.
	/// </summary>
	protected static Texture2D handleDrawTexture;

	/// <summary>
	/// Scale for drawing handles in the editor.
	/// </summary>
	public static float handleDrawSize = 0f;

	/// <summary>
	/// Color of a vertex handle
	/// </summary>
	public static Color vertexHandleColor = Color.red;

	/// <summary>
	/// Color of a control handle
	/// </summary>
	public static Color controlHandleColor = Color.red;
#endif

	/// <summary>
	/// Shared scene for tessellating shape meshes.
	/// </summary>
	protected static Scene tessellationScene;

	/// <summary>
	/// Shared settings for tessellating shape meshes.
	/// </summary>
	public static VectorUtils.TessellationOptions tessellationOptions;

	/// <summary>
	/// Shared builder for making line meshes.
	/// </summary>
	public static VectorLineMeshBuilder lineBuilder;

	static VectorShape()
	{
		tessellationScene = new Scene();

		tessellationOptions = new VectorUtils.TessellationOptions()
		{
			StepDistance = 50f,
			MaxCordDeviation = float.MaxValue,
			MaxTanAngleDeviation = Mathf.PI / 16.0f,
			SamplingStepSize = 0.05f
		};

		lineBuilder = new VectorLineMeshBuilder();
	}

	/// <summary>
	/// Dispose of the shape mesh.
	/// </summary>
	public void Dispose()
	{
		if (shapeMesh != null)
		{
#if UNITY_EDITOR
			UnityEngine.Object.DestroyImmediate(shapeMesh);
#else
			UnityEngine.Object.Destroy(shapeMesh);
#endif
		}

		shapeDirty = true;
		shapeMesh = null;

		if (lineMesh != null)
		{
#if UNITY_EDITOR
			UnityEngine.Object.DestroyImmediate(lineMesh);
#else
			UnityEngine.Object.Destroy(lineMesh);
#endif
		}

		lineDirty = true;
		lineMesh = null;
	}

	/// <summary>
	/// Mesh built from the tesselated shape.
	/// </summary>
	public Mesh ShapeMesh
	{
		get
		{
			if ((shapeMesh == null) || shapeDirty)
			{
				if (shapeMesh != null)
				{
#if UNITY_EDITOR
					UnityEngine.Object.DestroyImmediate(shapeMesh);
#else
					UnityEngine.Object.Destroy(shapeMesh);
#endif
				}

				GenerateGeometry();

				shapeMesh = new Mesh();
				if (shapeGeometry != null)
				{
					VectorUtils.FillMesh(shapeMesh, shapeGeometry, 1.0f);
				}
				shapeDirty = false;
			}

			return shapeMesh;
		}
	}

	/// <summary>
	/// Mesh of the shape outline for VectorLineShader.
	/// </summary>
	public Mesh LineMesh
	{
		get
		{
			if ((lineMesh == null) || lineDirty)
			{
				if (lineMesh != null)
				{
#if UNITY_EDITOR
					UnityEngine.Object.DestroyImmediate(lineMesh);
#else
					UnityEngine.Object.Destroy(lineMesh);
#endif
				}

				lineBuilder.Reset();

				GenerateLineMesh();

				lineMesh = lineBuilder.GetMesh();
				lineDirty = false;
			}

			return lineMesh;
		}
	}

	/// <summary>
	/// Mesh of the filled shape (may be empty).
	/// </summary>
	public Mesh FillMesh
	{
		get
		{
			return null;
		}
	}

	/// <summary>
	/// 2D bounding box of the shape.
	/// </summary>
	public Rect ShapeBounds
	{
		get
		{
			if (boundsDirty)
			{
				GenerateBounds();
			}

			return shapeBounds;
		}
	}

	/// <summary>
	/// Tessellated geometry of the shape.
	/// </summary>
	public List<VectorUtils.Geometry> ShapeGeometry
	{
		get
		{
			if ((shapeGeometry == null) || shapeDirty)
			{
				GenerateGeometry();
			}

			return shapeGeometry;
		}
	}

	/// <summary>
	/// Is the shape dirty?
	/// </summary>
	public bool Dirty
	{
		set
		{
			if (value)
			{
				shapeDirty = true;
				lineDirty = true;
				boundsDirty = true;
			}
		}
		get
		{
			return shapeDirty;
		}
	}

	/// <summary>
	/// Is the shape closed?
	/// </summary>
	public bool Closed
	{
		set
		{
			closed = value;
		}
		get
		{
			return closed;
		}
	}

	/// <summary>
	/// ID of the shape
	/// </summary>
	public string ID
	{
		set
		{
			guid = value;
		}
		get
		{
			return guid;
		}
	}

	/// <summary>
	/// Copy of the shape.
	/// </summary>
	/// <returns>New shape with properties of existing shape</returns>
	public abstract VectorShape Duplicate();

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public abstract float Distance(Vector2 pt);

	/// <summary>
	/// Tests if a shape contains a point.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Is the point inside the shape?</returns>
	public abstract bool Contains(Vector2 pt);

	/// <summary>
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public abstract bool IsInside(Rect rect);

	/// <summary>
	/// Rotate the shape around a point.
	/// </summary>
	/// <param name="center">Center of rotation</param>
	/// <param name="angle">Angle in degrees</param>
	public abstract void RotateAround(Vector2 center, float angle);

	/// <summary>
	/// Change the origin of the shape.
	/// </summary>
	/// <param name="offset">Offset to move shape</param>
	public abstract void TranslateBy(Vector2 offset);

	/// <summary>
	/// Change the size of the shape.
	/// </summary>
	/// <param name="scale">Scaling factor to apply</param>
	public abstract void ScaleBy(float scale);

	/// <summary>
	/// Transform the shape by an arbitrary matrix.
	/// </summary>
	/// <param name="matrix">Matrix to transform shape</param>
	public abstract void TransformBy(Matrix2D matrix);

	/// <summary>
	/// Build a mesh for display with the VectorLineShader.
	/// </summary>
	protected abstract void GenerateLineMesh();

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="mode">Snap modes to consider</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public abstract SnapPoint GetSnap(Vector2 pt, SnapPoint.Mode mode);

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected abstract void GenerateGeometry();

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected abstract void GenerateBounds();

	/// <summary>
	/// Add a 2D collider for the shape to a game object.
	/// </summary>
	protected abstract void AddColliderToGO(GameObject target);

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public abstract void WriteToXML(XmlWriter writer, Vector2 origin, float scale);

#if UNITY_EDITOR
	/// <summary>
	/// Draw the shape to the active camera using editor handles.
	/// </summary>
	/// <param name="selected">Is the shape selected?</param>
	/// <param name="active">Is it the active shape?</param>
	public virtual void DrawEditorHandles(bool selected, bool active = false)
	{
		if (handleDrawTexture == null)
		{
			handleDrawTexture = new Texture2D(1, 2);
			handleDrawTexture.SetPixel(0, 0, Color.white);
			handleDrawTexture.SetPixel(0, 1, Color.clear);
			handleDrawTexture.Apply();
		}
	}

	/// <summary>
	/// Respond to GUI input events in editor.
	/// </summary>
	/// <param name="currEvent">The current event</param>
	/// <param name="active">Is it the active shape?</param>
	/// <returns>Did the shape handle the event?</returns>
	public abstract bool HandleEditorEvent(Event currEvent, bool active);

	/// <summary>
	/// Calculate distance from GUI ray to shape.
	/// </summary>
	/// <returns>Distance from ray in screen space</returns>
	//public abstract float DistanceFromRay();
#endif

}