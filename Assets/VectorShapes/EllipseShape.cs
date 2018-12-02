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
public class EllipseShape : VectorShape
{
	protected Vector2 position;
	protected Vector2 majorAxis;
	protected float eccentricity;

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
	/// Eccentricty of ellipse (ratio of focuss distance to major axis).
	/// </summary>
	public float Eccentricity
	{
		set
		{
			if ((eccentricity >= 0f) && (eccentricity < 1f))
			{
				eccentricity = value;
				Dirty = true;
			}
			else
			{
				Debug.LogWarning("Invalid value for eccentricity: " + value);
			}
		}
		get
		{
			return eccentricity;
		}
	}

	/// <summary>
	/// Major axis of the ellipse.
	/// </summary>
	public Vector2 MajorAxis
	{
		set
		{
			majorAxis = value;
			Dirty = true;
		}
		get
		{
			return majorAxis;
		}
	}

	/// <summary>
	/// Minor axis of the ellipse (read only).
	/// </summary>
	public Vector2 MinorAxis
	{
		get
		{
			Vector2 minorAxis = Vector2.Perpendicular(majorAxis) * Mathf.Sqrt(1f - (eccentricity * eccentricity));
			return minorAxis;
		}
	}

	/// <summary>
	/// New ellipse from center point and axis radii.
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="radX">Radius of ellipse on X axis</param>
	/// <param name="radY">Radius of ellipse on Y axis</param>
	public EllipseShape(Vector2 center, float radX, float radY, float rotation = 0f)
	{
		position = center;
		if (radX >= radY)
		{
			majorAxis = Vector2.right * radX;
			eccentricity = Mathf.Sin(Mathf.Atan2(radX, radY));
		}
		else
		{
			majorAxis = Vector2.up * radY;
			eccentricity = Mathf.Sin(Mathf.Atan2(radY, radX));
		}

		majorAxis = Matrix2D.Rotate(rotation).MultiplyVector(majorAxis);
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		Vector2 focus1 = position + majorAxis * eccentricity;
		Vector2 focus2 = position - majorAxis * eccentricity;

		float distance1 = Vector2.Distance(pt, focus1);
		float distance2 = Vector2.Distance(pt, focus2);
		// HACK - this is not correct
		return ((distance1 + distance2) - (majorAxis.magnitude * 2f));
	}

	/// <summary>
	/// Tests if a shape contains a point
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Is the point inside the shape?</returns>
	public override bool Contains(Vector2 pt)
	{
		Vector2 focus1 = position + majorAxis * eccentricity;
		Vector2 focus2 = position - majorAxis * eccentricity;

		float distance1 = Vector2.Distance(pt, focus1);
		float distance2 = Vector2.Distance(pt, focus2);
		return ((distance1 + distance2) < (majorAxis.magnitude * 2f));
	}

	/// <summary>
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public override bool IsInside(Rect rect)
	{
		if (boundsDirty) GenerateBounds();

		if (rect.xMin > shapeBounds.xMin) return false;
		if (rect.xMax < shapeBounds.xMax) return false;
		if (rect.yMin > shapeBounds.yMin) return false;
		if (rect.yMax < shapeBounds.yMax) return false;

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
		majorAxis = matrix.MultiplyVector(majorAxis);

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
		position = matrix.MultiplyPoint(position);
		majorAxis = matrix.MultiplyVector(majorAxis);

		Dirty = true;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		Shape ellipse = new Shape();
		float rotation = Mathf.Atan2(MajorAxis.y, MajorAxis.x);
		VectorUtils.MakeEllipseShape(ellipse, Vector2.zero, MajorAxis.magnitude, MinorAxis.magnitude);

		ellipse.PathProps = new PathProperties()
		{
			Stroke = new Stroke()
			{
				Color = colorOutline,
				HalfThickness = penSize / Screen.dpi
			}
		};
		if (colorFill != Color.clear)
		{
			ellipse.Fill = new SolidFill()
			{
				Color = colorFill
			};
		}

		shapeNode = new SceneNode()
		{
			Transform = matrixTransform * Matrix2D.Translate(position) * Matrix2D.Rotate(rotation),
			Shapes = new List<Shape>
			{
				ellipse
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
		float a = MajorAxis.magnitude;
		float b = MinorAxis.magnitude;
		float theta = Mathf.Atan2(MajorAxis.y, MajorAxis.x);

		float sinTheta = Mathf.Sin(theta);
		float cosTheta = Mathf.Cos(theta);
		float extentX = Mathf.Sqrt((a * a * cosTheta * cosTheta) + (b * b * sinTheta * sinTheta));
		float extentY = Mathf.Sqrt((a * a * sinTheta * sinTheta) + (b * b * cosTheta * cosTheta));

		shapeBounds.xMin = position.x - extentX;
		shapeBounds.xMax = position.x + extentX;
		shapeBounds.yMin = position.y - extentY;
		shapeBounds.yMax = position.y + extentY;

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

		// HACK - this is REALLY wrong
		if (collider == null)
		{
			collider = collider.gameObject.AddComponent<CircleCollider2D>();
			collider.name = this.guid;
		}

		collider.offset = position;
		collider.radius = majorAxis.magnitude;
	}

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public override void WriteToXML(XmlWriter writer, Vector2 origin, float scale)
	{
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the circle to the active camera using editor handles.
	/// </summary>
	/// <param name="selected">Is the shape selected?</param>
	/// <param name="active">Is it the active shape?</param>
	public override void DrawEditorHandles(bool selected, bool active = false)
	{
		/*
		Vector3 handleP0 = new Vector3();
		Vector3 handleP1 = new Vector3();
		Vector3 handleP2 = new Vector3();	
		Vector3 handleP3 = new Vector3();

		if (colorFill != Color.clear)
		{
			Color colorPrev = Handles.color;
			Vector3 center3 = new Vector3(position.x, position.y, 0);

			Handles.color = colorFill;
			Handles.DrawSolidDisc(center3, Vector3.forward, radius);
			Handles.color = colorPrev;
		}

		// Draw the circle using 4 Bezier curves so we can control the pen size
		handleP0.x = position.x;
		handleP0.y = position.y + radius;
		handleP1.x = position.x + radius * bezierCircleConst;
		handleP1.y = position.y + radius;
		handleP2.x = position.x + radius;
		handleP2.y = position.y + radius * bezierCircleConst;
		handleP3.x = position.x + radius;
		handleP3.y = position.y;
		Handles.DrawBezier(handleP0, handleP3, handleP1, handleP2, colorOutline, handleDrawTexture, penSize);

		handleP0.x = position.x + radius;
		handleP0.y = position.y;
		handleP1.x = position.x + radius;
		handleP1.y = position.y - radius * bezierCircleConst;
		handleP2.x = position.x + radius * bezierCircleConst;
		handleP2.y = position.y - radius;
		handleP3.x = position.x;
		handleP3.y = position.y - radius;
		Handles.DrawBezier(handleP0, handleP3, handleP1, handleP2, colorOutline, handleDrawTexture, penSize);

		handleP0.x = position.x;
		handleP0.y = position.y - radius;
		handleP1.x = position.x - radius * bezierCircleConst;
		handleP1.y = position.y - radius;
		handleP2.x = position.x - radius;
		handleP2.y = position.y - radius * bezierCircleConst;
		handleP3.x = position.x - radius;
		handleP3.y = position.y;
		Handles.DrawBezier(handleP0, handleP3, handleP1, handleP2, colorOutline, handleDrawTexture, penSize);

		handleP0.x = position.x - radius;
		handleP0.y = position.y;
		handleP1.x = position.x - radius;
		handleP1.y = position.y + radius * bezierCircleConst;
		handleP2.x = position.x - radius * bezierCircleConst;
		handleP2.y = position.y + radius;
		handleP3.x = position.x;
		handleP3.y = position.y + radius;
		Handles.DrawBezier(handleP0, handleP3, handleP1, handleP2, colorOutline, handleDrawTexture, penSize);
		*/

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