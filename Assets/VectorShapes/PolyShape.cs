using System.Collections;
using System.Collections.Generic;
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

	/// <summary>
	/// Does the last vertex connect back to the first?
	/// </summary>
	public bool closed = true;

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
	/// New shape from array of points.
	/// </summary>
	public PolyShape(Vector2[] points)
	{
		int vertexCount = points.Length;
		vertices = new Vertex[vertexCount];

		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = new Vertex();
			vertices[i].position = points[i];
		}

		for (int i = 0; i < vertexCount; i++)
		{
			InitializeControlPoints(i);
		}

		if (points[0] != points[points.Length - 1])
		{
			closed = false;
		}
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
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		float distance = Mathf.Infinity;
		float segDist;

		for (int i = 0; i < vertices.Length; i++)
		{
			Vertex vert = vertices[i];
			Vertex vertNext = vertices[NextIndex(i)];

			if (vertices[i].segmentCurves)
			{
				segDist = DistancePointToBezierCurve(pt, vert.position, vert.exitCP, vertNext.enterCP, vertNext.position);
			}
			else
			{
				segDist = DistancePointToLineSegment(pt, vert.position, vertNext.position);
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
	/// Rotate the shape around a point.
	/// </summary>
	/// <param name="center">Center of rotation</param>
	/// <param name="angle">Angle in degrees</param>
	public override void RotateAround(Vector2 center, float angle)
	{
		Matrix2D matrix = Matrix2D.Translate(center) * Matrix2D.Rotate(angle * Mathf.Deg2Rad) * Matrix2D.Translate(-center);
		TransformBy(matrix);

		Dirty = true;
	}

	/// <summary>
	/// Change the origin of the shape.
	/// </summary>
	/// <param name="offset">Direction to move</param>
	public override void TranslateBy(Vector2 offset)
	{
		Matrix2D matrix = Matrix2D.Translate(offset);
		TransformBy(matrix);

		Dirty = true;
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
	/// Tesselate the shape into a mesh.
	/// </summary>
	protected override void GenerateMesh()
	{
		int segmentCount = closed ? vertices.Length + 1: vertices.Length;
		Shape shape = new Shape()
		{
			Contours = new BezierContour[]
			{
				new BezierContour()
				{
					Segments = new BezierPathSegment[segmentCount],
					Closed = closed
				}
			},
			PathProps = new PathProperties()
			{
				Stroke = new Stroke()
				{
					Color = colorOutline,
					HalfThickness = penSize / Screen.dpi
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

		int vertexCount = segmentCount - 1;
		for (int i = 0; i < vertexCount; i++)
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

		if (closed)
		{
			shape.Contours[0].Segments[vertexCount].P0 = vertices[0].position;
		}
		else
		{
			shape.Contours[0].Segments[vertexCount].P0 = vertices[vertexCount].position;
		}

		SceneNode polyNode = new SceneNode()
		{
			Transform = Matrix2D.identity,
			Drawables = new List<IDrawable>
			{
				shape
			}
		};

		tessellationScene.Root = polyNode;

		shapeMesh = new Mesh();
		var polyGeometry = VectorUtils.TessellateScene(tessellationScene, tessellationOptions);
		VectorUtils.FillMesh(shapeMesh, polyGeometry, 1.0f);
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
					pointList.Add(EvaluateCubicCurve(vert.position, vert.exitCP, vertNext.enterCP, vertNext.position, t));
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
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
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
					pointList.Add(EvaluateCubicCurve(vert.position, vert.exitCP, vertNext.enterCP, vertNext.position, t));
					t += step;
				}
			}
		}

		shapeBounds = VectorUtils.Bounds(pointList);
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the point to the active camera using editor handles.
	/// </summary>
	/// <param name="active">Is it the selected shape?</param>
	public override void DrawEditorHandles(bool active)
	{
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
	/// <param name="active">Is it the selected shape?</param>
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