using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Base class of drawable vector based 2d elements.
/// </summary>
public abstract class VectorShape
{

	/// <summary>
	/// Pen size for drawing.
	/// </summary>
	public static float penSize = 8f;

	/// <summary>
	/// Outline color.
	/// </summary>
	public Color colorOutline = Color.black;

	/// <summary>
	/// Fill color.
	/// </summary>
	public Color colorFill = Color.clear;

	/// <summary>
	/// ID.
	/// </summary>
	protected string guid = System.Guid.NewGuid().ToString();

	/// <summary>
	/// Transform matrix.
	/// </summary>
	protected Matrix2D matrixTransform = Matrix2D.identity;

	/// <summary>
	/// Inverse of transform matrix.
	/// </summary>
	protected Matrix2D matrixInverse = Matrix2D.identity;

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
	/// Bounds rectangle for shape (may be approximate for some shapes).
	/// </summary>
	protected Rect shapeBounds = Rect.zero;

	protected bool shapeDirty = true;
	protected bool inverseDirty = false;
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
	protected static VectorUtils.TessellationOptions tessellationOptions;

	static VectorShape()
	{
#if UNITY_EDITOR
		handleDrawTexture = new Texture2D(1, 2);
		handleDrawTexture.SetPixel(0, 0, Color.white);
		handleDrawTexture.SetPixel(0, 1, Color.clear);
		handleDrawTexture.Apply();
#endif
		tessellationScene = new Scene();

		tessellationOptions = new VectorUtils.TessellationOptions()
		{
			StepDistance = 0.01f,
			MaxCordDeviation = float.MaxValue,
			MaxTanAngleDeviation = Mathf.PI / 2.0f,
			SamplingStepSize = 0.01f
		};
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
				GenerateMesh();
			}

			return shapeMesh;
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
				shapeMesh = null;
				shapeDirty = true;
				boundsDirty = true;
				inverseDirty = true;
			}
		}
		get
		{
			return shapeDirty;
		}
	}

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
	/// Transform the shape by an arbitrary matrix.
	/// </summary>
	/// <param name="matrix">Matrix to transform shape</param>
	public abstract void TransformBy(Matrix2D matrix);

	/// <summary>
	/// Build the shape geometry into a mesh.
	/// </summary>
	protected void GenerateMesh()
	{
		if ((shapeMesh != null) && (!shapeDirty)) return;

		GenerateGeometry();

		shapeMesh = new Mesh();
		VectorUtils.FillMesh(shapeMesh, shapeGeometry, 1.0f);
	}

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
	public abstract void DrawEditorHandles(bool selected, bool active = false);

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