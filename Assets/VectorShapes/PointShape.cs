using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Vector point.
/// </summary>
public class PointShape : VectorShape
{

	/// <summary>
	/// Visual size of points.
	/// </summary>
	public static float pointRadius = 0.1f;

	/// <summary>
	/// Position of point.
	/// </summary>
	public Vector2 position;

	/// <summary>
	/// New point from location.
	/// </summary>
	/// <param name="location">Position of point</param>
	public PointShape(Vector2 location)
	{
		this.position = location;
	}

	/// <summary>
	/// New point from coordinates.
	/// </summary>
	/// <param name="x">X position of point</param>
	/// <param name="y">X position of point</param>
	public PointShape(float x, float y)
	{
		this.position = new Vector2(x, y);
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		return Vector2.Distance(pt, position);
	}

	/// <summary>
	/// Tests if a shape contains a point
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Is the point inside the shape?</returns>
	public override bool Contains(Vector2 pt)
	{
		return false;
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
		position = matrix.MultiplyPoint(position);

		Dirty = true;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		var seg1 = VectorUtils.MakePathLine(
			new Vector2(position.x, position.y + pointRadius),
			new Vector2(position.x, position.y - pointRadius)
		);
		var seg2 = VectorUtils.MakePathLine(
			new Vector2(position.x + pointRadius, position.y),
			new Vector2(position.x - pointRadius, position.y)
		);

		PathProperties pathProps = new PathProperties()
		{
			Stroke = new Stroke()
			{
				Color = colorOutline,
				HalfThickness = penSize / 2f / Screen.dpi
			}
		};

		Path path1 = new Path()
		{
			Contour = new BezierContour()
			{
				Segments = seg1
			},
			PathProps = pathProps
		};

		Path path2 = new Path()
		{
			Contour = new BezierContour()
			{
				Segments = seg2
			},
			PathProps = pathProps
		};

		SceneNode markNode = new SceneNode()
		{
			Transform = Matrix2D.identity,
			Drawables = new List<IDrawable>
			{
				path1, path2
			}
		};

		tessellationScene.Root = markNode;

		shapeGeometry = VectorUtils.TessellateScene(tessellationScene, tessellationOptions);
		shapeDirty = false;
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
		collider.radius = pointRadius;
	}

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
		shapeBounds = new Rect(position, Vector2.zero);
		boundsDirty = false;
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the point to the active camera using editor handles.
	/// </summary>
	/// <param name="active">Is it the selected shape?</param>
	public override void DrawEditorHandles(bool active)
	{
		/*
		Color colorPrev = Handles.color;
		Handles.color = colorOutline;

		Vector3[] handlePoints = new Vector3[2];

		// Draw a + to mark the point
		handlePoints[0].x = position.x;
		handlePoints[0].y = position.y + penExtent;
		handlePoints[1].x = position.x;
		handlePoints[1].y = position.y - penExtent;
		Handles.DrawAAPolyLine(handleDrawTexture, penSize, handlePoints);

		handlePoints[0].x = position.x + penExtent;
		handlePoints[0].y = position.y;
		handlePoints[1].x = position.x - penExtent;
		handlePoints[1].y = position.y;
		Handles.DrawAAPolyLine(handleDrawTexture, penSize, handlePoints);

		Handles.color = colorPrev;
		*/
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