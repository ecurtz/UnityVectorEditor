using System.Collections.Generic;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

/// <remarks>
/// Ellipse equations are taken from Luc Maisonobe's
/// "Quick computation of the distance between a point and an ellipse"
/// </remarks>

[System.Serializable]
/// <summary>
/// Vector ellipse.
/// </summary>
public class EllipseShape : VectorShape
{
	/// <summary>
	/// Position of center.
	/// </summary>
	[SerializeField]
	protected Vector2 position;

	/// <summary>
	/// Major axis of the ellipse.
	/// </summary>
	[SerializeField]
	protected Vector2 majorAxis;

	/// <summary>
	/// Eccentricty of ellipse (ratio of focuss distance to major axis).
	/// </summary>
	[SerializeField]
	protected float eccentricity;

	/// <summary>
	/// Starting angle for elliptical arcs (in radians).
	/// </summary>
	[SerializeField]
	protected float startAngle = 0f;

	/// <summary>
	/// Sweep angle for elliptical arcs (in radians).
	/// </summary>
	[SerializeField]
	protected float sweepAngle = Mathf.PI * 2f;

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
	/// Starting angle for elliptical arcs (in degrees).
	/// </summary>
	public float StartAngle
	{
		set
		{
			startAngle = value * Mathf.Deg2Rad;
			Dirty = true;
		}
		get
		{
			return startAngle * Mathf.Rad2Deg;
		}
	}

	/// <summary>
	/// Sweep angle for elliptical arcs (in degrees).
	/// </summary>
	public float SweepAngle
	{
		set
		{
			sweepAngle = value * Mathf.Deg2Rad;
			if (Mathf.Approximately(value, 0f) || (Mathf.Abs(value) >= 360f))
			{
				closed = true;
			}
			else
			{
				closed = false;
			}
			Dirty = true;
		}
		get
		{
			return sweepAngle * Mathf.Rad2Deg;
		}
	}

	protected EllipseShape()
	{
	}

	/// <summary>
	/// New ellipse from center point and axis radii (SVG format).
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="radX">Radius of ellipse on X axis</param>
	/// <param name="radY">Radius of ellipse on Y axis</param>
	/// <param name="rotation">Rotation from x-axis (in degrees)</param>
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

		majorAxis = Matrix2D.RotateRH(rotation * Mathf.Deg2Rad).MultiplyVector(majorAxis);
	}

	/// <summary>
	/// New ellipse from center point, major axis, and minor axis ratio (DXF format).
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="major">Major axis of the ellipse</param>
	/// <param name="ratio">Ratio of minor axis to major axis</param>
	public EllipseShape(Vector2 center, Vector2 major, float ratio)
	{
		position = center;
		majorAxis = major;
		float majorLength = major.magnitude;
		float minorLength = major.magnitude * ratio;

		eccentricity = Mathf.Sqrt((majorLength * majorLength) - (minorLength * minorLength)) / majorLength;
	}

	/// <summary>
	/// New elliptical arc from center point, major axis, minor axis ration, and angles.
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="major">Major axis of the ellipse</param>
	/// <param name="ratio">Ratio of minor axis to major axis</param>
	/// <param name="angle">Starting angle of arc (in degrees)</param>
	/// <param name="sweep">Sweep of arc (in degrees)</param>
	public EllipseShape(Vector2 center, Vector2 major, float ratio, float angle, float sweep)
	{
		position = center;
		majorAxis = major;
		float majorLength = major.magnitude;
		float minorLength = major.magnitude * ratio;

		eccentricity = Mathf.Sqrt((majorLength * majorLength) - (minorLength * minorLength)) / majorLength;

		startAngle = angle * Mathf.Deg2Rad;
		sweepAngle = sweep * Mathf.Deg2Rad;

		if (Mathf.Approximately(sweep, 0f) || (Mathf.Abs(sweep) >= 360f))
		{
			closed = true;
		}
		else
		{
			closed = false;
		}
	}

	protected static EllipseShape Create()
	{
		//EllipseShape shape = ScriptableObject.CreateInstance<EllipseShape>();
		EllipseShape shape = new EllipseShape();

		return shape;
	}

	/// <summary>
	/// New ellipse from center point and axis radii (SVG format).
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="radX">Radius of ellipse on X axis</param>
	/// <param name="radY">Radius of ellipse on Y axis</param>
	/// <param name="rotation">Rotation from x-axis (in degrees)</param>
	public static EllipseShape Create(Vector2 center, float radX, float radY, float rotation = 0f)
	{
		EllipseShape shape = Create();

		shape.position = center;
		if (radX >= radY)
		{
			shape.majorAxis = Vector2.right * radX;
			shape.eccentricity = Mathf.Sin(Mathf.Atan2(radX, radY));
		}
		else
		{
			shape.majorAxis = Vector2.up * radY;
			shape.eccentricity = Mathf.Sin(Mathf.Atan2(radY, radX));
		}

		shape.majorAxis = Matrix2D.RotateRH(rotation * Mathf.Deg2Rad).MultiplyVector(shape.majorAxis);

		return shape;
	}

	/// <summary>
	/// New ellipse from center point, major axis, and minor axis ratio (DXF format).
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="major">Major axis of the ellipse</param>
	/// <param name="ratio">Ratio of minor axis to major axis</param>
	public static EllipseShape Create(Vector2 center, Vector2 major, float ratio)
	{
		EllipseShape shape = Create();

		shape.position = center;
		shape.majorAxis = major;
		float majorLength = major.magnitude;
		float minorLength = major.magnitude * ratio;

		shape.eccentricity = Mathf.Sqrt((majorLength * majorLength) - (minorLength * minorLength)) / majorLength;

		return shape;
	}

	/// <summary>
	/// New elliptical arc from center point, major axis, minor axis ration, and angles.
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="major">Major axis of the ellipse</param>
	/// <param name="ratio">Ratio of minor axis to major axis</param>
	/// <param name="angle">Starting angle of arc (in degrees)</param>
	/// <param name="sweep">Sweep of arc (in degrees)</param>
	public static EllipseShape Create(Vector2 center, Vector2 major, float ratio, float angle, float sweep)
	{
		EllipseShape shape = Create();

		shape.position = center;
		shape.majorAxis = major;
		float majorLength = major.magnitude;
		float minorLength = major.magnitude * ratio;

		shape.eccentricity = Mathf.Sqrt((majorLength * majorLength) - (minorLength * minorLength)) / majorLength;

		shape.startAngle = angle * Mathf.Deg2Rad;
		shape.sweepAngle = sweep * Mathf.Deg2Rad;

		if (Mathf.Approximately(sweep, 0f) || (Mathf.Abs(sweep) >= 360f))
		{
			shape.closed = true;
		}
		else
		{
			shape.closed = false;
		}

		return shape;
	}

	/// <summary>
	/// Copy of the shape.
	/// </summary>
	/// <returns>New shape with properties of existing shape</returns>
	public override VectorShape Duplicate()
	{
		float ratio = MinorAxis.magnitude / MajorAxis.magnitude;
		return Create(position, majorAxis, ratio, startAngle * Mathf.Rad2Deg, sweepAngle * Mathf.Rad2Deg);
	}

	private static Vector2 ClosestEllipsePoint(Vector2 point, float semiMajor, float semiMinor)
	{
		Vector2 p = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y));

		float t = Mathf.PI / 4;

		float a = semiMajor;
		float b = semiMinor;

		Vector2 pt;

		float cosT, sinT, deltaC, deltaT;
		Vector2 e; Vector2 r; Vector2 q;

		for (int i = 0; i < 3; i++)
		{
			cosT = Mathf.Cos(t);
			sinT = Mathf.Sin(t);

			pt.x = a * cosT;
			pt.y = b * sinT;

			e.x = (a * a - b * b) * (cosT * cosT * cosT) / a;
			e.y = (b * b - a * a) * (sinT * sinT * sinT) / b;

			r = pt - e;
			q = p - e;

			deltaC = r.magnitude * Mathf.Asin((r.x * q.y - r.y * q.x) / (r.magnitude * q.magnitude));
			deltaT = deltaC / Mathf.Sqrt(a * a + b * b - pt.sqrMagnitude);

			t += deltaT;
			t = Mathf.Clamp(t, 0f, Mathf.PI / 2);
		}

		pt.x = a * Mathf.Cos(t) * Mathf.Sign(point.x);
		pt.y = b * Mathf.Sin(t) * Mathf.Sign(point.y);

		return pt;
	}

	/// <summary>
	/// Closest point on the shape to a point.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Closest point on shape</returns>
	public Vector2 ClosestPoint(Vector2 pt)
	{
		float theta = Mathf.Atan2(MajorAxis.y, MajorAxis.x);

		Vector2 major = MajorAxis;
		Vector2 minor = MinorAxis;

		Matrix2D matrix = Matrix2D.RotateRH(-theta) * Matrix2D.Translate(-position);

		Vector2 standardPt = matrix.MultiplyPoint(pt);

		Vector2 closestPt = ClosestEllipsePoint(standardPt, major.magnitude, minor.magnitude);

		closestPt = matrix.Inverse().MultiplyPoint(closestPt);

		return closestPt;
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		Vector2 closest = ClosestPoint(pt);

		return Vector2.Distance(pt, closest);
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
		Matrix2D matrix = Matrix2D.Translate(center) * Matrix2D.RotateRH(angle * Mathf.Deg2Rad) * Matrix2D.Translate(-center);
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

		position *= scale;
		majorAxis *= scale;

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
			Vector2[] centerPoints =
			{
				position,
				position + majorAxis * eccentricity,
				position - majorAxis * eccentricity,
			};

			foreach (Vector2 testPt in centerPoints)
			{
				float d = Vector2.Distance(pt, testPt);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Midpoint;
					snap.point = testPt;
				}
			}
		}

		if ((mode & SnapPoint.Mode.Midpoint) != 0)
		{
			Vector2[] midPoints =
			{
				position + majorAxis,
				position + MinorAxis,
				position - majorAxis,
				position - MinorAxis,
			};

			foreach (Vector2 testPt in midPoints)
			{
				float d = Vector2.Distance(pt, testPt);
				if (d < distance)
				{
					distance = d;
					snap.mode = SnapPoint.Mode.Midpoint;
					snap.point = testPt;
				}
			}
		}

		if ((mode & SnapPoint.Mode.Edge) != 0)
		{
			Vector2 closest = ClosestPoint(pt);
			float d = Vector2.Distance(pt, closest);

			if (d < distance)
			{
				distance = d;
				snap.mode = SnapPoint.Mode.Edge;
				snap.point = closest;
			}
		}

		return snap;
	}

	protected BezierSegment[] GenerateSegments()
	{
		int numCurves = 4; // Supposed to calculate from max error
		float theta = Mathf.Atan2(MajorAxis.y, MajorAxis.x);

		BezierSegment[] segments = new BezierSegment[numCurves];
		float deltaAngle = sweepAngle / numCurves;
		float sinTheta = Mathf.Sin(theta);
		float cosTheta = Mathf.Cos(theta);
		float angleB = startAngle;
		float a = MajorAxis.magnitude;
		float b = MinorAxis.magnitude;
		float t = Mathf.Tan(0.5f * deltaAngle);
		float alpha = Mathf.Sin(deltaAngle) * (Mathf.Sqrt(4f + 3f * t * t) - 1f) / 3f;

		float sinAngleB = Mathf.Sin(angleB);
		float cosAngleB = Mathf.Cos(angleB);
		float aSinAngleB = a * sinAngleB;
		float aCosAngleB = a * cosAngleB;
		float bSinAngleB = b * sinAngleB;
		float bCosAngleB = b * cosAngleB;
		Vector2 ptB = new Vector2();
		ptB.x = position.x + aCosAngleB * cosTheta - bSinAngleB * sinTheta;
		ptB.y = position.y + aCosAngleB * sinTheta + bSinAngleB * cosTheta;
		Vector2 dotB = new Vector2();
		dotB.x = -aSinAngleB * cosTheta - bCosAngleB * sinTheta;
		dotB.y = -aSinAngleB * sinTheta + bCosAngleB * cosTheta;

		for (int i = 0; i < numCurves; ++i)
		{
			float angleA = angleB;
			Vector2 ptA = ptB;
			Vector2 dotA = dotB;

			angleB += deltaAngle;
			sinAngleB = Mathf.Sin(angleB);
			cosAngleB = Mathf.Cos(angleB);
			aSinAngleB = a * sinAngleB;
			aCosAngleB = a * cosAngleB;
			bSinAngleB = b * sinAngleB;
			bCosAngleB = b * cosAngleB;
			ptB.x = position.x + aCosAngleB * cosTheta - bSinAngleB * sinTheta;
			ptB.y = position.y + aCosAngleB * sinTheta + bSinAngleB * cosTheta;
			dotB.x = -aSinAngleB * cosTheta - bCosAngleB * sinTheta;
			dotB.y = -aSinAngleB * sinTheta + bCosAngleB * cosTheta;

			segments[i].P0 = ptA;
			segments[i].P1.x = ptA.x + alpha * dotA.x;
			segments[i].P1.y = ptA.y + alpha * dotA.y;
			segments[i].P2.x = ptB.x - alpha * dotB.x;
			segments[i].P2.y = ptB.y - alpha * dotB.y;
			segments[i].P3 = ptB;
		}

		return segments;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		Shape ellipse = new Shape();

		ellipse.PathProps = new PathProperties()
		{
			Stroke = new Stroke()
			{
				Color = colorOutline,
				HalfThickness = penSize / 2f * penToMeshScale
			}
		};
		if (colorFill != Color.clear)
		{
			ellipse.Fill = new SolidFill()
			{
				Color = colorFill
			};
		}

		BezierSegment[] segments = GenerateSegments();

		ellipse.Contours = new BezierContour[1];
		ellipse.Contours[0] = new BezierContour();
		ellipse.Contours[0].Segments = VectorUtils.BezierSegmentsToPath(segments);

		shapeNode = new SceneNode()
		{
			Transform = matrixTransform,
			Shapes = new List<Shape>
			{
				ellipse
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
		BezierSegment[] segments = GenerateSegments();

		lineBuilder.BeginPolyLine(segments[0].P0);

		for (int i = 0; i < segments.Length; i++)
		{
			BezierSegment segment = segments[i];
			lineBuilder.CurveTo(segment.P1, segment.P2, segment.P3, 8);
		}

		lineBuilder.EndPolyLine(closed);
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
		base.DrawEditorHandles(selected, active);

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