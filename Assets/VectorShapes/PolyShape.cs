using System.Collections.Generic;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Closed shape of points connected by straight and curved segments.
/// </summary>
public class PolyShape : VectorShape
{
	/// <summary>
	/// Point on shape.
	/// </summary>
	[System.Serializable]
	public class Vertex
	{
		public enum Component
		{
			None,
			Position,
			EnterCP,
			ExitCP,
			Segment
		}

		/// <summary>
		/// Position of vertex
		/// </summary>
		public Vector2 position;
		/// <summary>
		/// Position of control point entering vertex
		/// </summary>
		public Vector2 enterCP;
		/// <summary>
		/// Position of control point exiting vertex
		/// </summary>
		public Vector2 exitCP;

		/// <summary>
		/// Does the following segment curve?
		/// </summary>
		public bool segmentCurves = false;
	}

	/// <summary>
	/// Array of vertices
	/// </summary>
	public Vertex[] vertices;

	/// <summary>
	/// Are curved segments continuous at vertices?
	/// </summary>
	public bool continuousCurves = true;

#if UNITY_EDITOR
	/// <summary>
	/// Scale of a control point handle
	/// </summary>
	public static float handleScale = 0.1f;

	/// <summary>
	/// Index of active vertex
	/// </summary>
	public int activeIndex = -1;

	/// <summary>
	/// Active component of active vertex
	/// </summary>
	public Vertex.Component activeComponent = Vertex.Component.None;
#endif

	protected PolyShape()
	{
	}

	/// <summary>
	/// New polygon start from a single origin point.
	/// </summary>
	/// <param name="point">Starting point for polygon</param>
	public PolyShape(Vector2 point)
	{
		vertices = new Vertex[1];
		vertices[0] = new Vertex();
		vertices[0].position = point;

		closed = false;
	}

	/// <summary>
	/// New regular n-sided polygon at location.
	/// </summary>
	/// <param name="center">Center of polygon</param>
	/// <param name="radius">Size of polygon</param>
	/// <param name="sides">Number of sides</param>
	public PolyShape(Vector2 center, float radius, int sides)
	{
		int vertexCount = Mathf.Max(sides, 3);
		vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
			float angle = Mathf.PI * 2f * ((float)i / vertexCount);
			vertices[i].position.x = center.x + Mathf.Sin(angle) * radius;
			vertices[i].position.y = center.y + Mathf.Cos(angle) * radius;
		}

		for (int i = 0; i < vertexCount; i++)
		{
			InitializeControlPoints(i);
		}
	}

	/// <summary>
	/// New shape from a rectangle.
	/// </summary>
	/// <param name="rectangle">Rectangle</param>
	public PolyShape(Rect rectangle)
	{
		int vertexCount = 4;
		vertices = new Vertex[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
		}

		vertices[0].position.x = rectangle.xMin;
		vertices[0].position.y = rectangle.yMin;
		vertices[1].position.x = rectangle.xMax;
		vertices[1].position.y = rectangle.yMin;
		vertices[2].position.x = rectangle.xMax;
		vertices[2].position.y = rectangle.yMax;
		vertices[3].position.x = rectangle.xMin;
		vertices[3].position.y = rectangle.yMax;

		closed = true;

		for (int i = 0; i < vertexCount; i++)
		{
			InitializeControlPoints(i);
		}
	}

	/// <summary>
	/// New shape from array of points.
	/// </summary>
	/// <param name="points">Array of vertex positions</param>
	/// <param name="curve">Do the segments curve?</param>
	public PolyShape(Vector2[] points, bool curve = false, bool close = false)
	{
		int vertexCount = points.Length;
		vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
			vertices[i].position = points[i];
			vertices[i].segmentCurves = curve;
		}

		for (int i = 0; i < vertexCount; i++)
		{
			InitializeControlPoints(i);
		}

		closed = close;
	}

	/// <summary>
	/// New PolyShape from Unity contour data.
	/// </summary>
	/// <param name="contour">Contour data</param>
	public PolyShape(BezierContour contour)
	{
		int vertexCount = contour.Segments.Length;
		vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
		}
		for (int i = 0; i < vertexCount; i++)
		{
			BezierPathSegment segment = contour.Segments[i];
			vertices[i].position = segment.P0;
			vertices[i].exitCP = segment.P1;
			vertices[NextIndex(i)].enterCP = segment.P2;
			vertices[i].segmentCurves = true;
		}

		closed = contour.Closed;
	}


	/// <summary>
	/// New PolyShape from Unity shape data.
	/// </summary>
	/// <param name="shape">Shape data</param>
	/// <param name="shapeTransform">Transform matrix</param>
	public PolyShape(Shape shape, Matrix2D shapeTransform)
	{
		int vertexCount = 0;
		foreach (BezierContour contour in shape.Contours)
		{
			vertexCount += contour.Segments.Length;
		}
		vertices = new Vertex[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
		}

		foreach (BezierContour contour in shape.Contours)
		{
			for (int i = 0; i < contour.Segments.Length; i++)
			{
				BezierPathSegment segment = contour.Segments[i];
				vertices[i].position = shapeTransform.MultiplyPoint(segment.P0);
				vertices[i].exitCP = shapeTransform.MultiplyPoint(segment.P1);
				vertices[NextIndex(i)].enterCP = shapeTransform.MultiplyPoint(segment.P2);
				vertices[i].segmentCurves = true;
			}

			closed = contour.Closed;
		}

		if (shape.PathProps.Stroke != null)
		{
			colorOutline = shape.PathProps.Stroke.Color;
		}
	}

	protected static PolyShape Create()
	{
		//PolyShape shape = ScriptableObject.CreateInstance<PolyShape>();
		PolyShape shape = new PolyShape();

		return shape;
	}

	/// <summary>
	/// New polygon start from a single origin point.
	/// </summary>
	/// <param name="point">Starting point for polygon</param>
	public static PolyShape Create(Vector2 point)
	{
		PolyShape shape = Create();

		shape.vertices = new Vertex[1];
		shape.vertices[0] = new Vertex();
		shape.vertices[0].position = point;

		shape.closed = false;

		return shape;
	}

	/// <summary>
	/// New regular n-sided polygon at location.
	/// </summary>
	/// <param name="center">Center of polygon</param>
	/// <param name="radius">Size of polygon</param>
	/// <param name="sides">Number of sides</param>
	public static PolyShape Create(Vector2 center, float radius, int sides)
	{
		PolyShape shape = Create();

		int vertexCount = Mathf.Max(sides, 3);
		shape.vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			shape.vertices[i] = new Vertex();
			float angle = Mathf.PI * 2f * ((float)i / vertexCount);
			shape.vertices[i].position.x = center.x + Mathf.Sin(angle) * radius;
			shape.vertices[i].position.y = center.y + Mathf.Cos(angle) * radius;
		}

		for (int i = 0; i < vertexCount; i++)
		{
			shape.InitializeControlPoints(i);
		}

		return shape;
	}

	/// <summary>
	/// New shape from a rectangle.
	/// </summary>
	/// <param name="rectangle">Rectangle</param>
	public static PolyShape Create(Rect rectangle)
	{
		PolyShape shape = Create();

		int vertexCount = 4;
		shape.vertices = new Vertex[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			shape.vertices[i] = new Vertex();
		}

		shape.vertices[0].position.x = rectangle.xMin;
		shape.vertices[0].position.y = rectangle.yMin;
		shape.vertices[1].position.x = rectangle.xMax;
		shape.vertices[1].position.y = rectangle.yMin;
		shape.vertices[2].position.x = rectangle.xMax;
		shape.vertices[2].position.y = rectangle.yMax;
		shape.vertices[3].position.x = rectangle.xMin;
		shape.vertices[3].position.y = rectangle.yMax;

		shape.closed = true;

		for (int i = 0; i < vertexCount; i++)
		{
			shape.InitializeControlPoints(i);
		}

		return shape;
	}

	/// <summary>
	/// New shape from array of points.
	/// </summary>
	/// <param name="points">Array of vertex positions</param>
	/// <param name="curve">Do the segments curve?</param>
	public static PolyShape Create(Vector2[] points, bool curve = false, bool close = false)
	{
		PolyShape shape = Create();

		int vertexCount = points.Length;
		shape.vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			shape.vertices[i] = new Vertex();
			shape.vertices[i].position = points[i];
			shape.vertices[i].segmentCurves = curve;
		}

		for (int i = 0; i < vertexCount; i++)
		{
			shape.InitializeControlPoints(i);
		}

		shape.closed = close;

		return shape;
	}

	/// <summary>
	/// New PolyShape from Unity contour data.
	/// </summary>
	/// <param name="contour">Contour data</param>
	public static PolyShape Create(BezierContour contour)
	{
		PolyShape shape = Create();

		int vertexCount = contour.Segments.Length;
		shape.vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			shape.vertices[i] = new Vertex();
		}
		for (int i = 0; i < vertexCount; i++)
		{
			BezierPathSegment segment = contour.Segments[i];
			shape.vertices[i].position = segment.P0;
			shape.vertices[i].exitCP = segment.P1;
			shape.vertices[shape.NextIndex(i)].enterCP = segment.P2;
			shape.vertices[i].segmentCurves = true;
		}

		shape.closed = contour.Closed;

		return shape;
	}


	/// <summary>
	/// New PolyShape from Unity shape data.
	/// </summary>
	/// <param name="unityShape">Shape data</param>
	/// <param name="shapeTransform">Transform matrix</param>
	public static PolyShape Create(Shape unityShape, Matrix2D shapeTransform)
	{
		PolyShape shape = Create();

		int vertexCount = 0;
		foreach (BezierContour contour in unityShape.Contours)
		{
			vertexCount += contour.Segments.Length;
		}
		shape.vertices = new Vertex[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			shape.vertices[i] = new Vertex();
		}

		foreach (BezierContour contour in unityShape.Contours)
		{
			for (int i = 0; i < contour.Segments.Length; i++)
			{
				BezierPathSegment segment = contour.Segments[i];
				shape.vertices[i].position = shapeTransform.MultiplyPoint(segment.P0);
				shape.vertices[i].exitCP = shapeTransform.MultiplyPoint(segment.P1);
				shape.vertices[shape.NextIndex(i)].enterCP = shapeTransform.MultiplyPoint(segment.P2);
				shape.vertices[i].segmentCurves = true;
			}

			shape.closed = contour.Closed;
		}

		if (unityShape.PathProps.Stroke != null)
		{
			shape.colorOutline = unityShape.PathProps.Stroke.Color;
		}

		return shape;
	}

	/// <summary>
	/// Copy of the shape.
	/// </summary>
	/// <returns>New shape with properties of existing shape</returns>
	public override VectorShape Duplicate()
	{
		PolyShape duplicate = Create();

		int vertexCount = vertices.Length;
		duplicate.vertices = new Vertex[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			duplicate.vertices[i] = new Vertex();
			duplicate.vertices[i].position = vertices[i].position;
			duplicate.vertices[i].enterCP = vertices[i].enterCP;
			duplicate.vertices[i].exitCP = vertices[i].exitCP;
			duplicate.vertices[i].segmentCurves = vertices[i].segmentCurves;
		}

		duplicate.closed = closed;

		return duplicate;
	}

	/// <summary>
	/// Vertex index preceeding given index.
	/// </summary>
	int PreviousIndex(int i)
	{
		return (i > 0) ? i - 1 : vertices.Length - 1;
	}

	/// <summary>
	/// Vertex index following given index.
	/// </summary>
	int NextIndex(int i)
	{
		return (i + 1) % vertices.Length;
	}

	/// <summary>
	/// Initialize control points to reasonjable value based on neighoring vertices.
	/// </summary>
	void InitializeControlPoints(int index)
	{
		if (index < 0) return;
		if (index >= vertices.Length) return;
		if (vertices.Length < 2) return;

		Vector2 prevOffset = vertices[PreviousIndex(index)].position - vertices[index].position;
		Vector2 nextOffset = vertices[index].position - vertices[NextIndex(index)].position;

		if (!closed)
		{
			if (index == 0)
				prevOffset = -nextOffset;
			if (index == (vertices.Length - 1))
				nextOffset = -prevOffset;
		}

		Vector2 direction = prevOffset.normalized + nextOffset.normalized;
		direction.Normalize();

		vertices[index].enterCP = vertices[index].position + direction * (prevOffset.magnitude * 0.5f);
		vertices[index].exitCP = vertices[index].position + direction * (-nextOffset.magnitude * 0.5f);
	}

	/// <summary>
	/// Add a new line segment onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	public void LineTo(Vector2 pt)
	{
		if (vertices.Length == 0)
		{
			Debug.LogWarning("LineTo with no starting vertex.");
			return;
		}

		if (closed)
		{
			Debug.LogWarning("Appending vertices to closed PolyShape.");
		}

		int index = vertices.Length;
		int prev = PreviousIndex(index);
		System.Array.Resize(ref vertices, vertices.Length + 1);
		vertices[index] = new Vertex();
		vertices[index].position = pt;
		vertices[index - 1].segmentCurves = false;

		Dirty = true;
	}

	/// <summary>
	/// Add a quadratic curve segment onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	/// <param name="control">Control point position</param>
	public void CurveTo(Vector2 pt, Vector2 control)
	{
		if (vertices.Length == 0)
		{
			Debug.LogWarning("CurveTo with no starting vertex.");
			return;
		}

		if (closed)
		{
			Debug.LogWarning("Appending vertices to closed PolyShape.");
		}

		int index = vertices.Length;
		int prev = PreviousIndex(index);
		System.Array.Resize(ref vertices, vertices.Length + 1);
		vertices[index] = new Vertex();
		vertices[index].position = pt;
		vertices[index - 1].segmentCurves = true;
		vertices[index - 1].exitCP = control;
		vertices[index].enterCP = control;

		Dirty = true;
	}

	/// <summary>
	/// Add a cubic curve segment onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	/// <param name="controlA">Control point A position</param>
	/// <param name="controlB">Control point B position</param>
	public void CurveTo(Vector2 pt, Vector2 controlA, Vector2 controlB)
	{
		if (vertices.Length == 0)
		{
			Debug.LogWarning("CurveTo with no starting vertex.");
			return;
		}

		if (closed)
		{
			Debug.LogWarning("Appending vertices to closed PolyShape.");
		}

		int index = vertices.Length;
		int prev = PreviousIndex(index);
		System.Array.Resize(ref vertices, vertices.Length + 1);
		vertices[index] = new Vertex();
		vertices[index].position = pt;
		vertices[index - 1].segmentCurves = true;
		vertices[index - 1].exitCP = controlA;
		vertices[index].enterCP = controlB;

		Dirty = true;
	}

	/// <summary>
	/// Add a new circular arc onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	/// <param name="sweep">Sweep of angle connecting point (in degrees)</param>
	public void ArcTo(Vector2 pt, float sweep)
	{
		if (vertices.Length == 0) return;
		if (Mathf.Approximately(sweep, 0f))
		{
			LineTo(pt);
			return;
		}

		int index = vertices.Length;
		int prev = PreviousIndex(index);
		float sweepRads = sweep * Mathf.Deg2Rad;
		// I'm not actually sure this is how DXF does it, but it looks right in my test.
		float sinSweep = Mathf.Sin(sweepRads);
		float offset = sinSweep * sinSweep * Mathf.Sign(sweep);

		ArcTo(pt, sweepRads, offset);
	}

	/// <summary>
	/// Add a new circular arc onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	/// <param name="bulge">Amount of bulge in the arc</param>
	/// <remarks>
	/// The bulge factor is the tangent of one fourth the included angle for an arc segment,
	/// made negative if the arc goes clockwise from the start point to the endpoint.
	/// A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle.
	/// </remarks>
	public void ArcToDXF(Vector2 pt, float bulge)
	{
		if (vertices.Length == 0) return;
		if (Mathf.Approximately(bulge, 0f))
		{
			LineTo(pt);
			return;
		}

		float sweepRads = Mathf.Atan(bulge) * 4f;
		// I'm not actually sure this is how DXF does it, but it looks right in my test.
		float sinSweep = Mathf.Sin(sweepRads);
		float offset = sinSweep * sinSweep * Mathf.Sign(bulge);

		ArcTo(pt, sweepRads, offset);
	}

	/// <summary>
	/// Internal method to add a new circular arc onto the shape.
	/// </summary>
	/// <param name="pt">New vertex position</param>
	/// <param name="sweepRads">Angle swept (in radians!)</param>
	/// <param name="offset">Amount arc center if offset from segment</param>
	protected void ArcTo(Vector2 pt, float sweepRads, float offset)
	{
		if (vertices.Length == 0)
		{
			Debug.LogWarning("ArcTo with no starting vertex.");
			return;
		}

		if (closed)
		{
			Debug.LogWarning("Appending vertices to closed PolyShape.");
		}

		int index = vertices.Length;
		int prev = PreviousIndex(index);

		Vector2 position = vertices[prev].position;
		Vector2 segment = pt - position;
		Vector2 midpoint = Vector2.Lerp(pt, position, 0.5f);
		Vector2 center = midpoint + Vector2.Perpendicular(segment) * offset;
		float radius = (position - center).magnitude;

		int arcCount = Mathf.CeilToInt(Mathf.Abs(sweepRads) / (90f * Mathf.Deg2Rad));
		Vector2[] arcPoints = new Vector2[arcCount];
		float arcAngle = Mathf.Atan2(position.y - center.y, position.x - center.x);
		float arcAngle2 = Mathf.Atan2(pt.y - center.y, pt.x - center.x);
		float arcStep = sweepRads / arcCount;
		for (int i = 0; i < arcCount; i++)
		{
			arcAngle += arcStep;
			arcPoints[i].x = center.x + radius * Mathf.Cos(arcAngle);
			arcPoints[i].y = center.y + radius * Mathf.Sin(arcAngle);
		}
		arcPoints[arcCount - 1] = pt;

		System.Array.Resize(ref vertices, vertices.Length + arcCount);

		for (int i = 0; i < arcCount; i++)
		{
			vertices[prev].segmentCurves = true;

			vertices[index] = new Vertex();
			vertices[index].position = arcPoints[i];

			Vector2 a = vertices[prev].position - center;
			Vector2 b = vertices[index].position - center;
			float q1 = a.sqrMagnitude;
			float q2 = q1 + a.x * b.x + a.y * b.y;
			float k2 = 4f / 3f * (Mathf.Sqrt(2f * q1 * q2) - q2) / (a.x * b.y - a.y * b.x);

			vertices[prev].exitCP.x = center.x + a.x - k2 * a.y;
			vertices[prev].exitCP.y = center.y + a.y + k2 * a.x;
			vertices[index].enterCP.x = center.x + b.x + k2 * b.y;
			vertices[index].enterCP.y = center.y + b.y - k2 * b.x;

			prev = index;
			index++;
		}

		Dirty = true;
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		float distance = Mathf.Infinity;
		float segDist;

		int count = closed ? vertices.Length : vertices.Length - 1;
		for (int i = 0; i < count; i++)
		{
			Vertex vert = vertices[i];
			Vertex vertNext = vertices[NextIndex(i)];

			if (vertices[i].segmentCurves)
			{
				segDist = VectorShapeUtils.DistancePointToBezierCurve(pt, vert.position, vert.exitCP, vertNext.enterCP, vertNext.position);
			}
			else
			{
				segDist = VectorShapeUtils.DistancePointToLineSegment(pt, vert.position, vertNext.position);
			}

			if (segDist < distance)
			{
				distance = segDist;
			}
		}

		return distance;
	}

	/// <summary>
	/// Tests if a shape contains a point
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Is the point inside the shape?</returns>
	public override bool Contains(Vector2 pt)
	{
		int crossings = 0;

		//for (int i = 0; i < vertices.Length; i++)
		//{
		//	Vertex vert = vertices[i];
		//	Vertex vertNext = vertices[NextIndex(i)];

		//	if (vertices[i].segmentCurves)
		//	{
		//	}
		//	else
		//	{
		//	}
		//}

		return ((crossings % 2) == 1);
	}

	/// <summary>
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public override bool IsInside(Rect rect)
	{
		if (boundsDirty) GenerateBounds();

		if (shapeBounds.xMin < rect.xMin) return false;
		if (shapeBounds.xMax > rect.xMax) return false;
		if (shapeBounds.yMin < rect.yMin) return false;
		if (shapeBounds.yMax > rect.yMax) return false;

		return true;
	}

	/// <summary>
	/// Rotate the shape around a point.
	/// </summary>
	/// <param name="center">Center of rotation</param>
	/// <param name="angle">Angle in degrees</param>
	public override void RotateAround(Vector2 center, float angle)
	{
		Matrix2D matrix = Matrix2D.Translate(center) * Matrix2D.RotateRH(angle * Mathf.Deg2Rad) * Matrix2D.Translate(-center);
		TransformBy(matrix);
	}

	/// <summary>
	/// Change the origin of the shape.
	/// </summary>
	/// <param name="offset">Direction to move</param>
	public override void TranslateBy(Vector2 offset)
	{
		Matrix2D matrix = Matrix2D.Translate(offset);
		TransformBy(matrix);
	}

	/// <summary>
	/// Change the size of the shape.
	/// </summary>
	/// <param name="scale">Scaling factor to apply</param>
	public override void ScaleBy(float scale)
	{
		if (scale < Mathf.Epsilon)
		{
			Debug.LogWarning("Scale must be greater than zero.");
			return;
		}

		Matrix2D matrix = Matrix2D.Scale(new Vector2(scale, scale));
		TransformBy(matrix);
	}

	/// <summary>
	/// Transform the shape by an arbitrary matrix.
	/// </summary>
	/// <param name="matrix">Matrix to transform shape</param>
	public override void TransformBy(Matrix2D matrix)
	{
		for (int i = 0; i < vertices.Length; i++)
		{
			Vertex vert = vertices[i];
			vert.position = matrix.MultiplyPoint(vert.position);
			vert.enterCP = matrix.MultiplyPoint(vert.enterCP);
			vert.exitCP = matrix.MultiplyPoint(vert.exitCP);
		}

		Dirty = true;
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="mode">Snap modes to consider</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override SnapPoint GetSnap(Vector2 pt, SnapPoint.Mode mode)
	{
		SnapPoint snap = new SnapPoint();
		float distance = float.MaxValue;

		if ((mode & SnapPoint.Mode.Center) != 0)
		{
			if (closed && (vertices.Length > 0))
			{
				Vector2 center = new Vector2();
				for (int i = 0; i < vertices.Length; i++)
				{
					center += vertices[i].position;
				}

				center /= vertices.Length;
				float d = Vector2.Distance(pt, center);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Center;
					snap.point = center;
				}
			}
		}

		if ((mode & SnapPoint.Mode.Endpoint) != 0)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				float d = Vector2.Distance(pt, vertices[i].position);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Endpoint;
					snap.point = vertices[i].position;
				}
			}
		}

		if ((mode & SnapPoint.Mode.Midpoint) != 0)
		{
			int count = closed ? vertices.Length : vertices.Length - 1;
			for (int i = 0; i < count; i++)
			{
				Vertex vert = vertices[i];
				Vertex vertNext = vertices[NextIndex(i)];
				Vector2 midPoint;

				if (vertices[i].segmentCurves)
				{
					midPoint = VectorShapeUtils.EvaluateCubicCurve(vert.position, vert.exitCP, vertNext.enterCP, vertNext.position, 0.5f);
				}
				else
				{
					midPoint = (vert.position + vertNext.position) / 2f;
				}
				float d = Vector2.Distance(pt, midPoint);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Midpoint;
					snap.point = midPoint;
				}
			}
		}

		if ((mode & SnapPoint.Mode.Edge) != 0)
		{
			int count = closed ? vertices.Length : vertices.Length - 1;
			for (int i = 0; i < count; i++)
			{
				Vertex vert = vertices[i];
				Vertex vertNext = vertices[NextIndex(i)];
				Vector2 closest;

				if (vertices[i].segmentCurves)
				{
					closest = VectorShapeUtils.ClosetPointOnBezierCurve(pt, vert.position, vert.exitCP, vertNext.enterCP, vertNext.position);
				}
				else
				{
					closest = VectorShapeUtils.ClosestPointOnLineSegment(pt, vert.position, vertNext.position);
				}
				float d = Vector2.Distance(pt, closest);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Edge;
					snap.point = closest;
				}
			}
		}

		return snap;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		Shape shape = new Shape()
		{
			Contours = new BezierContour[]
			{
				new BezierContour()
				{
					Segments = new BezierPathSegment[vertices.Length],
					Closed = closed
				}
			},
			PathProps = new PathProperties()
			{
				Stroke = new Stroke()
				{
					Color = colorOutline,
					HalfThickness = penSize / 2f * penToMeshScale
				}
			}
		};

		if (closed && (colorFill != Color.clear))
		{
			shape.Fill = new SolidFill()
			{
				Color = colorFill
			};
		}

		for (int i = 0; i < vertices.Length; i++)
		{
			shape.Contours[0].Segments[i].P0 = vertices[i].position;
			if (vertices[i].segmentCurves)
			{
				shape.Contours[0].Segments[i].P1 = vertices[i].exitCP;
				shape.Contours[0].Segments[i].P2 = vertices[NextIndex(i)].enterCP;
			}
			else
			{
				Vector2 midPoint = (vertices[i].position + vertices[NextIndex(i)].position) / 2f;
				shape.Contours[0].Segments[i].P1 = midPoint;
				shape.Contours[0].Segments[i].P2 = midPoint;
			}
		}

		shapeNode = new SceneNode()
		{
			Transform = matrixTransform,
			Shapes = new List<Shape>
			{
				shape
			}
		};

		tessellationScene.Root = shapeNode;

		shapeGeometry = VectorUtils.TessellateScene(tessellationScene, tessellationOptions);
		shapeDirty = false;
	}

	/// <summary>
	/// Build a mesh for display with the VectorLineShader.
	/// </summary>
	protected override void GenerateLineMesh()
	{
		Vertex vert = vertices[0];
		Vertex vertPrevious;

		lineBuilder.BeginPolyLine(vert.position);

		for (int i = 1; i < vertices.Length; i++)
		{
			vertPrevious = vert;
			vert = vertices[i];

			if (vertPrevious.segmentCurves)
			{
				lineBuilder.CurveTo(vertPrevious.exitCP, vert.enterCP, vert.position, 8);
			}
			else
			{
				lineBuilder.LineTo(vert.position);
			}
		}

		if (closed)
		{
			if (vert.segmentCurves)
			{
				lineBuilder.CurveTo(vert.exitCP, vertices[0].enterCP, vertices[0].position, 8);
			}
			else
			{
				lineBuilder.LineTo(vertices[0].position);
			}
		}

		lineBuilder.EndPolyLine(closed);
	}

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
	// TO DO
	// http://www.iquilezles.org/www/articles/bezierbbox/bezierbbox.htm
		int bezierSteps = VectorShapeUtils.bezierSteps;
		List<Vector2> pointList = new List<Vector2>();
		float step = 1f / bezierSteps;

		for (int i = 0; i < vertices.Length; i++)
		{
			Vertex vert = vertices[i];

			pointList.Add(vert.position);
			if (vert.segmentCurves)
			{
				Vertex vertNext = vertices[NextIndex(i)];
				float t = step;
				for (int j = 1; j < bezierSteps; j++)
				{
					pointList.Add(VectorShapeUtils.EvaluateCubicCurve(vert.position, vert.exitCP, vertNext.enterCP, vertNext.position, t));
					t += step;
				}
			}
		}

		shapeBounds = VectorUtils.Bounds(pointList);
		boundsDirty = false;
	}

	/// <summary>
	/// Build a 2D collider for the shape.
	/// </summary>
	protected override void AddColliderToGO(GameObject target)
	{
		PolygonCollider2D[] polyColliders = target.GetComponents<PolygonCollider2D>();
		PolygonCollider2D polyCollider = null;

		for (int i = 0; i < polyColliders.Length; i++)
		{
			if (polyColliders[i].name == this.guid)
			{
				polyCollider = polyColliders[i];
			}
		}

		EdgeCollider2D[] edgeColliders = target.GetComponents<EdgeCollider2D>();
		EdgeCollider2D edgeCollider = null;

		for (int i = 0; i < edgeColliders.Length; i++)
		{
			if (edgeColliders[i].name == this.guid)
			{
				edgeCollider = edgeColliders[i];
				break;
			}
		}

		if (closed)
		{
			if (polyCollider == null)
			{
				polyCollider = target.AddComponent<PolygonCollider2D>();
				polyCollider.name = this.guid;
			}

			if (edgeCollider != null)
			{
				Object.Destroy(edgeCollider);
			}
		}
		else
		{
			if (edgeCollider == null)
			{
				edgeCollider = target.AddComponent<EdgeCollider2D>();
				edgeCollider.name = this.guid;
			}

			if (polyCollider != null)
			{
				Object.Destroy(polyCollider);
			}
		}

		int bezierSteps = VectorShapeUtils.bezierSteps;
		List<Vector2> pointList = new List<Vector2>();
		float step = 1f / bezierSteps;

		int edgeCount = closed ? vertices.Length : vertices.Length - 1;
		for (int i = 0; i < edgeCount; i++)
		{
			Vertex vert = vertices[i];

			pointList.Add(vert.position);
			if (vert.segmentCurves)
			{
				Vertex vertNext = vertices[NextIndex(i)];
				float t = step;
				for (int j = 1; j < bezierSteps; j++)
				{
					pointList.Add(VectorShapeUtils.EvaluateCubicCurve(vert.position, vert.exitCP, vertNext.enterCP, vertNext.position, t));
					t += step;
				}
			}
		}

		if (closed)
		{
			polyCollider.points = pointList.ToArray();
		}
		else
		{
			edgeCollider.points = pointList.ToArray();
		}
	}

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public override void WriteToXML(XmlWriter writer, Vector2 origin, float scale)
	{
		writer.WriteStartElement("path");

		writer.WriteStartAttribute("stroke");
		writer.WriteValue(VectorShapeFilesSVG.ConvertColor(colorOutline));
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("stroke-width");
		writer.WriteValue("1mm");
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("fill");
		writer.WriteValue(VectorShapeFilesSVG.ConvertColor(colorFill));
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("d");
		if (vertices.Length > 1)
		{
			Vertex vert = vertices[0];
			writer.WriteValue("M ");
			writer.WriteValue(vert.position.x);
			writer.WriteValue(" ");
			writer.WriteValue(-vert.position.y);

			for (int i = 1; i < vertices.Length; i++)
			{
				Vertex vertNext = vertices[i];

				if (vert.segmentCurves)
				{
					writer.WriteValue(" C ");
					writer.WriteValue(vert.exitCP.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vert.exitCP.y);
					writer.WriteValue(" ");
					writer.WriteValue(vertNext.enterCP.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vertNext.enterCP.y);
					writer.WriteValue(" ");
					writer.WriteValue(vertNext.position.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vertNext.position.y);
				}
				else
				{
					writer.WriteValue(" L ");
					writer.WriteValue(vertNext.position.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vertNext.position.y);
				}

				vert = vertNext;
			}

			if (closed)
			{
				if (vert.segmentCurves)
				{
					Vertex vertNext = vertices[0];
					writer.WriteValue(" C ");
					writer.WriteValue(vert.exitCP.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vert.exitCP.y);
					writer.WriteValue(" ");
					writer.WriteValue(vertNext.enterCP.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vertNext.enterCP.y);
					writer.WriteValue(" ");
					writer.WriteValue(vertNext.position.x);
					writer.WriteValue(" ");
					writer.WriteValue(-vertNext.position.y);
				}
				writer.WriteValue(" Z");
			}
		}
		writer.WriteEndAttribute();

		writer.WriteEndElement();
}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the shape to the active camera using editor handles.
	/// </summary>
	/// <param name="selected">Is the shape selected?</param>
	/// <param name="active">Is it the active shape?</param>
	public override void DrawEditorHandles(bool selected, bool active = false)
	{
		base.DrawEditorHandles(selected, active);

		/*
		Vector2 midPoint = new Vector2();
		for (int i = 0; i < vertices.Length; i++)
		{
			int j = NextIndex(i);
			if (vertices[i].segmentCurves)
			{
				Handles.DrawBezier(vertices[i].position, vertices[j].position, vertices[i].exitCP, vertices[j].enterCP, colorOutline, handleDrawTexture, penSize);
			}
			else
			{
				midPoint = (vertices[i].position + vertices[j].position) / 2f;
				Handles.DrawBezier(vertices[i].position, vertices[j].position, midPoint, midPoint, colorOutline, handleDrawTexture, penSize);
			}
		}
		*/
		if (selected)
		{
			if (boundsDirty) GenerateBounds();
			Handles.DrawSolidRectangleWithOutline(shapeBounds, Color.clear, Handles.color);
		}

		if (active)
		{
			Color colorPrev = Handles.color;
			Vector3[] points = new Vector3[2];
			float handleSize = handleDrawSize * handleScale;

			Handles.color = controlHandleColor;
			for (int i = 0; i < vertices.Length; i++)
			{
				bool curveVertex = false;
				points[0] = vertices[i].position;
				if (vertices[PreviousIndex(i)].segmentCurves)
				{
					curveVertex = true;
					points[1] = vertices[i].enterCP;

					Handles.DrawAAPolyLine(handleDrawTexture, penSize / 2f, points);
					Handles.DrawSolidDisc(points[1], Vector3.forward, handleSize);
				}
				if (vertices[i].segmentCurves)
				{
					curveVertex = true;
					points[1] = vertices[i].exitCP;

					Handles.DrawAAPolyLine(handleDrawTexture, penSize / 2f, points);
					Handles.DrawSolidDisc(points[1], Vector3.forward, handleSize);
				}

				if (curveVertex)
				{
					Handles.DrawSolidDisc(points[0], Vector3.forward, handleSize);
				}
				else
				{
					Rect handleRect = new Rect(points[0].x - handleSize, points[0].y - handleSize, handleSize * 2f, handleSize * 2f);
					Handles.DrawSolidRectangleWithOutline(handleRect, controlHandleColor, controlHandleColor);
				}
			}

			Handles.color = colorPrev;
		}
	}

	/// <summary>
	/// Respond to GUI input events in editor.
	/// </summary>
	/// <param name="currEvent">The current event</param>
	/// <param name="active">Is it the active shape?</param>
	/// <returns>Did the shape handle the event?</returns>
	public override bool HandleEditorEvent(Event currEvent, bool active)
	{
		if (active)
		{
			float handleDist = handleScale * 1.5f;
			float handleDistSqr = handleDist * handleDist;

			if (currEvent.type == EventType.MouseDown)
			{
				activeIndex = -1;
				activeComponent = Vertex.Component.None;

				// Check the control points
				for (int i = 0; i < vertices.Length; i++)
				{
					if (vertices[i].segmentCurves || vertices[PreviousIndex(i)].segmentCurves)
					{
						if (Vector2.SqrMagnitude(currEvent.mousePosition - vertices[i].enterCP) < handleDistSqr)
						{
							activeIndex = i;
							activeComponent = Vertex.Component.EnterCP;
							return true;
						}

						if (Vector2.SqrMagnitude(currEvent.mousePosition - vertices[i].exitCP) < handleDistSqr)
						{
							activeIndex = i;
							activeComponent = Vertex.Component.ExitCP;
							return true;
						}
					}
				}

				// Check the vertex points
				for (int i = 0; i < vertices.Length; i++)
				{
					if (Vector2.SqrMagnitude(currEvent.mousePosition - vertices[i].position) < handleDistSqr)
					{
						activeIndex = i;
						activeComponent = Vertex.Component.Position;
						return true;
					}
				}

				// Check the segments
				for (int i = 0; i < vertices.Length; i++)
				{
				}
			}

			if (currEvent.type == EventType.MouseDrag)
			{
				if (activeIndex > -1)
				{
					switch (activeComponent)
					{
						case Vertex.Component.Position:
							Vector2 delta = currEvent.mousePosition - vertices[activeIndex].position;
							vertices[activeIndex].position = currEvent.mousePosition;
							vertices[activeIndex].enterCP += delta;
							vertices[activeIndex].exitCP += delta;
							Dirty = true;
							break;

						case Vertex.Component.EnterCP:
							vertices[activeIndex].enterCP = currEvent.mousePosition;
							if (continuousCurves)
							{
								float dist = Vector2.Distance(vertices[activeIndex].position, vertices[activeIndex].exitCP);
								vertices[activeIndex].exitCP = vertices[activeIndex].position;
								vertices[activeIndex].exitCP += (vertices[activeIndex].position - vertices[activeIndex].enterCP).normalized * dist;
							}
							Dirty = true;
							break;

						case Vertex.Component.ExitCP:
							vertices[activeIndex].exitCP = currEvent.mousePosition;
							if (continuousCurves)
							{
								float dist = Vector2.Distance(vertices[activeIndex].position, vertices[activeIndex].enterCP);
								vertices[activeIndex].enterCP = vertices[activeIndex].position;
								vertices[activeIndex].enterCP += (vertices[activeIndex].position - vertices[activeIndex].exitCP).normalized * dist;
							}
							Dirty = true;
							break;

						case Vertex.Component.Segment:
							break;
					}

					return true;
				}
			}
		}

		return false;
	}
#endif

}