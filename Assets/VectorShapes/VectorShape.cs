using System.Collections.Generic;
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
	static public float penSize = 8f;

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

#if UNITY_EDITOR
	/// <summary>
	/// Shared texture for drawing AA lines in the editor.
	/// </summary>
	protected static Texture2D handleDrawTexture;

	/// <summary>
	/// Scale for drawing handles in the editor.
	/// </summary>
	public static float handleDrawSize = 0f;
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
#endif
		tessellationScene = new Scene();

		tessellationOptions = new VectorUtils.TessellationOptions()
		{
			StepDistance = 0.05f,
			MaxCordDeviation = float.MaxValue,
			MaxTanAngleDeviation = Mathf.PI / 2.0f,
			SamplingStepSize = 0.01f
		};
	}

	public List<VectorUtils.Geometry> shapeGeometry = null;
	protected Mesh shapeMesh = null;
	protected bool shapeDirty = true;

	protected Rect shapeBounds = Rect.zero;
	protected bool boundsDirty = true;

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
			}
		}
		get
		{
			return (shapeDirty || boundsDirty);
		}
	}

	/// <summary>
	/// Point on a quadratic curve between pt0 and pt2.
	/// </summary>
	/// <param name="pt0">Starting point</param>
	/// <param name="pt1">Control point</param>
	/// <param name="pt2">Ending point</param>
	/// <param name="t">Distance along curve</param>
	public static Vector2 EvaluateQuadraticCurve(Vector2 pt0, Vector2 pt1, Vector2 pt2, float t)
	{
		Vector2 p0 = Vector2.Lerp(pt0, pt1, t);
		Vector2 p1 = Vector2.Lerp(pt1, pt2, t);
		return Vector2.Lerp(p0, p1, t);
	}

	/// <summary>
	/// Point on a cubic curve between pt0 and pt3.
	/// </summary>
	/// <param name="pt0">Starting point</param>
	/// <param name="pt1">Control point</param>
	/// <param name="pt2">Control point</param>
	/// <param name="pt3">Ending point</param>
	/// <param name="t">Distance along curve</param>
	public static Vector2 EvaluateCubicCurve(Vector2 pt0, Vector2 pt1, Vector2 pt2, Vector2 pt3, float t)
	{
		Vector2 p0 = EvaluateQuadraticCurve(pt0, pt1, pt2, t);
		Vector2 p1 = EvaluateQuadraticCurve(pt1, pt2, pt3, t);
		return Vector2.Lerp(p0, p1, t);
	}

	/// <summary>
	/// Distance between a point and a line segment.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="segA">Start of line segment</param>
	/// <param name="segB">End of line segment</param>
	/// <returns>Distance</returns>
	public static float DistancePointToLineSegment(Vector2 pt, Vector2 segA, Vector2 segB)
	{
		float segLength = (segB - segA).sqrMagnitude;
		if (segLength < Mathf.Epsilon) // Segment is actually a point
			return (pt - segA).magnitude;
		
		float t = Vector2.Dot(pt - segA, segB - segA) / segLength;
		if (t < 0.0) // Beyond the 'a' end of the segment
			return (pt - segA).magnitude;
		if (t > 1.0) // Beyond the 'b' end of the segment
			return (pt - segB).magnitude;

		// Projection falls on the segment
		Vector2 projection = segA + t * (segB - segA);
		return (pt - projection).magnitude;
	}

	/// <summary>
	/// Number of steps when approximating Bezier curves.
	/// </summary>
	public static int bezierSteps = 12;

#if UNITY_EDITOR
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
	/// Distance between a point and a Bezier curve
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="curveA">Start of curve</param>
	/// <param name="controlA">Control point A</param>
	/// <param name="controlB">Control point B</param>
	/// <param name="curveB">End of curve</param>
	/// <returns>Distance (approximate)</returns>
	public static float DistancePointToBezierCurve(Vector2 pt, Vector2 curveA, Vector2 controlA, Vector2 controlB, Vector2 curveB)
	{
		float sqrDistance = (pt - curveA).sqrMagnitude;

		float step = 1f / bezierSteps;
		float t = step;
		for (int i = 1; i < bezierSteps; i++)
		{
			Vector2 curvePt = EvaluateCubicCurve(curveA, controlA, controlB, curveB, t);
			float sqrDistance2 = (pt - curvePt).sqrMagnitude;
			if (sqrDistance2 < sqrDistance)
				sqrDistance = sqrDistance2;

			t += step;
		}

		return Mathf.Sqrt(sqrDistance);
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