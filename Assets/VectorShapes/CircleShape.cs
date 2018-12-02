using System.Collections.Generic;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Vector circle.
/// </summary>
public class CircleShape : VectorShape
{
	protected Vector2 position;
	protected float radius;

	/// <summary>
	/// Position of center.
	/// </summary>
	public Vector2 Position
	{
		set
		{
			position = value;
			Dirty = true;
		}
		get
		{
			return position;
		}
	}

	/// <summary>
	/// Radius of circle.
	/// </summary>
	public float Radius
	{
		set
		{
			radius = value;
			Dirty = true;
		}
		get
		{
			return radius;
		}
	}

	/// <summary>
	/// New circle from center point and radius.
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="rad">Radius of circle</param>
	public CircleShape(Vector2 center, float rad)
	{
		position = center;
		radius = rad;
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		return Mathf.Abs(Vector2.Distance(pt, position) - radius);
	}

	/// <summary>
	/// Tests if a shape contains a point
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Is the point inside the shape?</returns>
	public override bool Contains(Vector2 pt)
	{
		return (Vector2.Distance(pt, position) < radius);
	}

	/// <summary>
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public override bool IsInside(Rect rect)
	{
		if (!rect.Contains(position)) return false;

		Vector2 testPt = position;
		testPt.x = position.x - radius;
		if (!rect.Contains(testPt)) return false;
		testPt.x = position.x + radius;
		if (!rect.Contains(testPt)) return false;

		testPt = position;
		testPt.y = position.y - radius;
		if (!rect.Contains(testPt)) return false;
		testPt.y = position.y + radius;
		if (!rect.Contains(testPt)) return false;

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
		position = matrix.MultiplyPoint(position);

		Dirty = true;
	}

	/// <summary>
	/// Change the origin of the shape.
	/// </summary>
	/// <param name="offset">Direction to move</param>
	public override void TranslateBy(Vector2 offset)
	{
		position += offset;

		Dirty = true;
	}

	/// <summary>
	/// Transform the shape by an arbitrary matrix.
	/// </summary>
	/// <param name="matrix">Matrix to transform shape</param>
	public override void TransformBy(Matrix2D matrix)
	{
		// Attempt to identify uniform scaling
		Vector2 pt0 = matrix.MultiplyPoint(position + new Vector2(0, radius));
		Vector2 pt1 = matrix.MultiplyPoint(position + new Vector2(0, -radius));
		Vector2 pt2 = matrix.MultiplyPoint(position + new Vector2(radius, 0));
		Vector2 pt3 = matrix.MultiplyPoint(position + new Vector2(-radius, 0));

		position = matrix.MultiplyPoint(position);

		float distSqr = Vector2.SqrMagnitude(pt0 - position);
		if (Mathf.Approximately(distSqr, Vector2.SqrMagnitude(pt1 - position)) &&
		    Mathf.Approximately(distSqr, Vector2.SqrMagnitude(pt2 - position)) &&
		    Mathf.Approximately(distSqr, Vector2.SqrMagnitude(pt3 - position)))
		{
			radius = Mathf.Sqrt(distSqr);
		}
		else
		{
			Debug.LogWarning("Ignored matrix change that would destroy circle.");
		}

		Dirty = true;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		Shape circle = new Shape();
		VectorUtils.MakeCircleShape(circle, position, radius);

		circle.PathProps = new PathProperties()
		{
			Stroke = new Stroke()
			{
				Color = colorOutline,
				HalfThickness = penSize / Screen.dpi
			}
		};
		if (colorFill != Color.clear)
		{
			circle.Fill = new SolidFill()
			{
				Color = colorFill
			};
		}

		shapeNode = new SceneNode()
		{
			Transform = matrixTransform,
			Shapes = new List<Shape>
			{
				circle
			}
		};

		tessellationScene.Root = shapeNode;
		shapeGeometry = VectorUtils.TessellateScene(tessellationScene, tessellationOptions);

		shapeMesh = null;
		shapeDirty = false;
	}

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
		shapeBounds = new Rect(position - new Vector2(radius, radius), new Vector2(radius * 2, radius * 2));
		boundsDirty = false;
	}

	/// <summary>
	/// Build a 2D collider for the shape.
	/// </summary>
	protected override void AddColliderToGO(GameObject target)
	{
		CircleCollider2D[] colliders = target.GetComponents<CircleCollider2D>();
		CircleCollider2D collider = null;

		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].name == this.guid)
			{
				collider = colliders[i];
			}
		}

		if (collider == null)
		{
			collider = collider.gameObject.AddComponent<CircleCollider2D>();
			collider.name = this.guid;
		}

		collider.offset = position;
		collider.radius = radius;
	}

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public override void WriteToXML(XmlWriter writer, Vector2 origin, float scale)
	{
		Vector2 svgPosition = (position - origin) * new Vector2(scale, -scale);

		writer.WriteStartElement("circle");

		writer.WriteStartAttribute("cx");
		writer.WriteValue(svgPosition.x);
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("cy");
		writer.WriteValue(svgPosition.y);
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("r");
		writer.WriteValue(radius * scale);
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("stroke");
		writer.WriteValue(VectorShapeFilesSVG.ConvertColor(colorOutline));
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("stroke-width");
		writer.WriteValue("0.01");
		writer.WriteEndAttribute();

		writer.WriteStartAttribute("fill");
		writer.WriteValue(VectorShapeFilesSVG.ConvertColor(colorFill));
		writer.WriteEndAttribute();

		writer.WriteEndElement();
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the circle to the active camera using editor handles.
	/// </summary>
	/// <param name="selected">Is the shape selected?</param>
	/// <param name="active">Is it the active shape?</param>
	public override void DrawEditorHandles(bool selected, bool active = false)
	{
		if (selected)
		{
			if (boundsDirty) GenerateBounds();
			Handles.DrawSolidRectangleWithOutline(shapeBounds, Color.clear, Handles.color);
		}

		if (active)
		{
			
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
		return false;
	}
#endif

}