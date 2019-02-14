using System.Collections.Generic;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.VectorGraphics;

[System.Serializable]
/// <summary>
/// Vector shape composed of other shapes.
/// </summary>
public class CompoundShape : VectorShape
{
	private List<VectorShape> _components;
	public List<VectorShape> components { get {return _components;} }

	/// <summary>
	/// New empty compound shape.
	/// </summary>
	public CompoundShape()
	{
		_components = new List<VectorShape>();
	}

	/// <summary>
	/// New empty compound shape.
	/// </summary>
	public static CompoundShape Create()
	{
		//CompoundShape shape = ScriptableObject.CreateInstance<CompoundShape>();
		CompoundShape shape = new CompoundShape();

		shape._components = new List<VectorShape>();

		return shape;
	}

	/// <summary>
	/// Copy of the shape.
	/// </summary>
	/// <returns>New shape with properties of existing shape</returns>
	public override VectorShape Duplicate()
	{
		CompoundShape duplicate = Create();
		foreach (VectorShape component in components)
		{
			duplicate.AddComponent(component.Duplicate());
		}

		return duplicate;
	}

	/// <summary>
	/// Add a component shape.
	/// </summary>
	public void AddComponent(VectorShape shape)
	{
		_components.Add(shape);

		Dirty = true;
	}

	/// <summary>
	/// Distance between a point and the shape.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <returns>Distance from point to nearest point on shape</returns>
	public override float Distance(Vector2 pt)
	{
		float distance = float.MaxValue;
		foreach (VectorShape component in _components)
		{
			float componentDistance = component.Distance(pt);
			distance = Mathf.Min(distance, componentDistance);
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
		foreach (VectorShape component in _components)
		{
			if (component.Contains(pt)) return true;
		}

		return false;
	}

	/// <summary>
	/// Tests if a shape is inside a rectangle.
	/// </summary>
	/// <param name="rect">Test rectangle</param>
	/// <returns>Is the shape entirely inside the rectangle?</returns>
	public override bool IsInside(Rect rect)
	{
		foreach (VectorShape component in _components)
		{
			if (!component.IsInside(rect)) return false;
		}

		return true;
	}

	/// <summary>
	/// Rotate the shape around a point.
	/// </summary>
	/// <param name="center">Center of rotation</param>
	/// <param name="angle">Angle in degrees</param>
	public override void RotateAround(Vector2 center, float angle)
	{
		foreach (VectorShape component in _components)
		{
			component.RotateAround(center, angle);
		}

		Dirty = true;
	}

	/// <summary>
	/// Change the origin of the shape.
	/// </summary>
	/// <param name="offset">Direction to move</param>
	public override void TranslateBy(Vector2 offset)
	{
		foreach (VectorShape component in _components)
		{
			component.TranslateBy(offset);
		}

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

		foreach (VectorShape component in _components)
		{
			component.ScaleBy(scale);
		}

		Dirty = true;
	}

	/// <summary>
	/// Transform the shape by an arbitrary matrix.
	/// </summary>
	/// <param name="matrix">Matrix to transform shape</param>
	public override void TransformBy(Matrix2D matrix)
	{
		foreach (VectorShape component in _components)
		{
			component.TransformBy(matrix);
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

		return snap;
	}

	/// <summary>
	/// Tessellate the shape into geometry data.
	/// </summary>
	protected override void GenerateGeometry()
	{
		if ((shapeGeometry != null) && (!shapeDirty)) return;

		shapeGeometry = new List<VectorUtils.Geometry>();

		foreach (VectorShape component in _components)
		{
			shapeGeometry.AddRange(component.ShapeGeometry);
		}

		shapeDirty = false;
	}

	/// <summary>
	/// Build a mesh for display with the VectorLineShader.
	/// </summary>
	protected override void GenerateLineMesh()
	{
		foreach (VectorShape component in _components)
		{
			shapeGeometry.AddRange(component.ShapeGeometry);
		}
	}

	/// <summary>
	/// Build a 2D bounding box for the shape.
	/// </summary>
	protected override void GenerateBounds()
	{
		if (_components.Count == 0)
		{
			shapeBounds = new Rect();
		}
		else
		{
			shapeBounds = _components[0].ShapeBounds;
			for (int i = 1; i < _components.Count; i++)
			{
				shapeBounds = VectorShapeUtils.RectUnion(shapeBounds, _components[i].ShapeBounds);
			}
		}
		boundsDirty = false;
	}

	/// <summary>
	/// Build a 2D collider for the shape.
	/// </summary>
	protected override void AddColliderToGO(GameObject target)
	{
		CompositeCollider2D collider = new CompositeCollider2D();
		//foreach (VectorShape component in components)
		//{
		//}

	}

	/// <summary>
	/// Serialize the shape to an XML writer.
	/// </summary>
	public override void WriteToXML(XmlWriter writer, Vector2 origin, float scale)
	{
		//writer.WriteStartElement("circle");

		//foreach (VectorShape component in components)
		//{
		//}

		//writer.WriteEndElement();
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

		foreach (VectorShape component in _components)
		{
			component.DrawEditorHandles(selected, active);
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
		bool handled = false;
		foreach (VectorShape component in _components)
		{
			handled |= component.HandleEditorEvent(currEvent, active);
		}

		return handled;
	}
#endif
}