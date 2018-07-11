using System.Collections;
using System.Collections.Generic;
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

	/// <summary>
	/// Constant for generating circle using Bezier curves.
	/// </summary>
	const float bezierCircleConst = 0.55191502449f;

	/// <summary>
	/// Position of center.
	/// </summary>
	public Vector2 position;

	/// <summary>
	/// Radius of circle.
	/// </summary>
	public float radius = 1f;

	/// <summary>
	/// New circle from center point and radius.
	/// </summary>
	/// <param name="center">Center of circle</param>
	/// <param name="rad">Radius of circle</param>
	public CircleShape(Vector2 center, float rad)
	{
		this.position = center;
		this.radius = rad;
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
	/// Tesselate the shape into a mesh.
	/// </summary>
	protected override void GenerateMesh()
	{
		var circle = VectorUtils.MakeCircle(position, radius);
		// Draw the circle using 4 Bezier curves
		/*
		var circle = new Shape()
		{
			Contours = new BezierContour[]
			{
				new BezierContour()
				{
					Segments = new BezierPathSegment[5],
					Closed = true
				}
			}
		};

		circle.Contours[0].Segments[0].P0.x = position.x;
		circle.Contours[0].Segments[0].P0.y = position.y + radius;
		circle.Contours[0].Segments[0].P1.x = position.x + radius * bezierCircleConst;
		circle.Contours[0].Segments[0].P1.y = position.y + radius;
		circle.Contours[0].Segments[0].P2.x = position.x + radius;
		circle.Contours[0].Segments[0].P2.y = position.y + radius * bezierCircleConst;

		circle.Contours[0].Segments[1].P0.x = position.x + radius;
		circle.Contours[0].Segments[1].P0.y = position.y;
		circle.Contours[0].Segments[1].P1.x = position.x + radius;
		circle.Contours[0].Segments[1].P1.y = position.y - radius * bezierCircleConst;
		circle.Contours[0].Segments[1].P2.x = position.x + radius * bezierCircleConst;
		circle.Contours[0].Segments[1].P2.y = position.y - radius;

		circle.Contours[0].Segments[2].P0.x = position.x;
		circle.Contours[0].Segments[2].P0.y = position.y - radius;
		circle.Contours[0].Segments[2].P1.x = position.x - radius * bezierCircleConst;
		circle.Contours[0].Segments[2].P1.y = position.y - radius;
		circle.Contours[0].Segments[2].P2.x = position.x - radius;
		circle.Contours[0].Segments[2].P2.y = position.y - radius * bezierCircleConst;

		circle.Contours[0].Segments[3].P0.x = position.x - radius;
		circle.Contours[0].Segments[3].P0.y = position.y;
		circle.Contours[0].Segments[3].P1.x = position.x - radius;
		circle.Contours[0].Segments[3].P1.y = position.y + radius * bezierCircleConst;
		circle.Contours[0].Segments[3].P2.x = position.x - radius * bezierCircleConst;
		circle.Contours[0].Segments[3].P2.y = position.y + radius;

		circle.Contours[0].Segments[4].P0.x = circle.Contours[0].Segments[0].P0.x;
		circle.Contours[0].Segments[4].P0.y = circle.Contours[0].Segments[0].P0.y;
		*/

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

		SceneNode circleNode = new SceneNode()
		{
			Transform = Matrix2D.identity,
			Drawables = new List<IDrawable>
			{
				circle
			}
		};

		tessellationScene.Root = circleNode;

		shapeMesh = new Mesh();
		var circleGeometry = VectorUtils.TessellateScene(tessellationScene, tessellationOptions);
		VectorUtils.FillMesh(shapeMesh, circleGeometry, 1.0f);
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
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
		shapeBounds = new Rect(position - new Vector2(radius, radius), new Vector2(radius * 2, radius * 2));
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the circle to the active camera using editor handles.
	/// </summary>
	/// <param name="active">Is it the selected shape?</param>
	public override void DrawEditorHandles(bool active)
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
	/// <param name="active">Is it the selected shape?</param>
	/// <returns>Did the shape handle the event?</returns>
	public override bool HandleEditorEvent(Event currEvent, bool active)
	{
		return false;
	}
#endif

}