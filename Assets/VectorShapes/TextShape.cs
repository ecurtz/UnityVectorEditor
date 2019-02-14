using System.Collections.Generic;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Vector point.
/// </summary>
public class TextShape : VectorShape
{
	/// <summary>
	/// Position of text origin.
	/// </summary>
	public Vector2 position;

	/// <summary>
	/// Rotation of text (in degrees).
	/// </summary>
	public float rotation;

	/// <summary>
	/// Text.
	/// </summary>
	public string text;

	/// <summary>
	/// Font.
	/// </summary>
	[SerializeField]
	protected string font;

	/// <summary>
	/// Font size.
	/// </summary>
	[SerializeField]
	protected int size;

	protected TextShape()
	{
	}

	/// <summary>
	/// New point from location.
	/// </summary>
	/// <param name="location">Position of point</param>
	/// <param name="content">Text to display</param>
	public TextShape(Vector2 location, string content)
	{
		position = location;
		text = content;
	}

	public static TextShape Create()
	{
		//TextShape shape = ScriptableObject.CreateInstance<TextShape>();
		TextShape shape = new TextShape();

		return shape;
	}

	/// <summary>
	/// New point from location.
	/// </summary>
	/// <param name="location">Position of point</param>
	/// <param name="content">Text to display</param>
	public static TextShape Create(Vector2 location, string content)
	{
		TextShape shape = Create();

		shape.position = location;
		shape.text = content;

		return shape;
	}

	/// <summary>
	/// Copy of the shape.
	/// </summary>
	/// <returns>New shape with properties of existing shape</returns>
	public override VectorShape Duplicate()
	{
		return Create(position, text);
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
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public override bool IsInside(Rect rect)
	{
		return rect.Contains(position);
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
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="mode">Snap modes to consider</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override SnapPoint GetSnap(Vector2 pt, SnapPoint.Mode mode)
	{
		SnapPoint snap = new SnapPoint();

		return snap;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		PathProperties pathProps = new PathProperties()
		{
			Stroke = new Stroke()
			{
				Color = colorOutline,
				HalfThickness = penSize / 2f * penToMeshScale
			}
		};

		Shape textShape = new Shape()
		{
			Contours = new BezierContour[]
			{
			},
			PathProps = pathProps
		};

		shapeNode = new SceneNode()
		{
			Transform = matrixTransform,
			Shapes = new List<Shape>
			{
				textShape
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
	}

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
		shapeBounds = new Rect(position, Vector2.zero);
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
	}

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public override void WriteToXML(XmlWriter writer, Vector2 origin, float scale)
	{
	}

#if UNITY_EDITOR
	/// <summary>
	/// Draw the point to the active camera using editor handles.
	/// </summary>
	/// <param name="selected">Is the shape selected?</param>
	/// <param name="active">Is it the active shape?</param>
	public override void DrawEditorHandles(bool selected, bool active = false)
	{
		base.DrawEditorHandles(selected, active);

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
	/// <param name="active">Is it the active shape?</param>
	/// <returns>Did the shape handle the event?</returns>
	public override bool HandleEditorEvent(Event currEvent, bool active)
	{
		return false;
	}
#endif

}