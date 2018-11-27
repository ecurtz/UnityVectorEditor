﻿using System.Collections.Generic;
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
	/// <param name="points">Array of vertex positions</param>
	/// <param name="curve">Do the segments curve?</param>
	public PolyShape(Vector2[] points, bool curve = false)
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
	/// Add a new vertex onto the shape.
	/// </summary>
	/// <param name="pt">New vertex</param>
	public void AppendVertex(Vector2 pt)
	{
		int newIndex = vertices.Length;
		int previousIndex = PreviousIndex(newIndex);

		Vertex newVertex = new Vertex();
		newVertex.position = pt;
		List<Vertex> newVertices = new List<Vertex>(vertices);
		newVertices.Add(newVertex);
		vertices = newVertices.ToArray();

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

		for (int i = 0; i < vertices.Length; i++)
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
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
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
		writer.WriteValue(VectorShapeSVGExporter.ConvertColor(colorOutline));
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("stroke-width");
		writer.WriteValue("0.01");
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("fill");
		writer.WriteValue(VectorShapeSVGExporter.ConvertColor(colorFill));
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