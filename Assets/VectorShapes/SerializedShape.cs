using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shape", menuName = "Testing/Shape")]
[System.Serializable]
public class SerializedShape : ScriptableObject
{
	/// <remarks>
	/// Have to do this crap to use Unity serialization
	/// which can't handle class inheritance
	/// </remarks>
	[SerializeField]
	protected List<CircleShape> circles;

	[SerializeField]
	protected List<EllipseShape> ellipses;

	[SerializeField]
	protected List<PointShape> points;

	[SerializeField]
	protected List<PolyShape> polys;

	[SerializeField]
	protected List<TextShape> texts;

	[System.NonSerialized]
	protected List<VectorShape> _components;
	public List<VectorShape> components
	{
		get
		{
			int componentCount = 0;
			componentCount += circles.Count;
			componentCount += ellipses.Count;
			componentCount += points.Count;
			componentCount += polys.Count;
			componentCount += texts.Count;

			if (_components == null) _components = new List<VectorShape>();
			if (_components.Count != componentCount)
			{
				_components.Clear();
				_components.AddRange(circles);
				_components.AddRange(ellipses);
				_components.AddRange(points);
				_components.AddRange(polys);
				_components.AddRange(texts);
			}

			return _components;
		}
	}

	private void OnEnable()
	{
		if (circles == null) circles = new List<CircleShape>();
		if (ellipses == null) ellipses = new List<EllipseShape>();
		if (points == null) points = new List<PointShape>();
		if (polys == null) polys = new List<PolyShape>();
		if (texts == null) texts = new List<TextShape>();
	}

	public void SetShapes(List<VectorShape> shapes)
	{
		circles.Clear();
		ellipses.Clear();
		points.Clear();
		polys.Clear();
		texts.Clear();

		foreach (VectorShape shape in shapes)
		{
			AddShape(shape);
		}
	}

	public void AddShapes(List<VectorShape> shapes)
	{
		foreach (VectorShape shape in shapes)
		{
			AddShape(shape);
		}
	}

	public void AddShape(VectorShape shape)
	{
		if (shape is CircleShape)
		{
			circles.Add(shape as CircleShape);
		}
		else if (shape is EllipseShape)
		{
			ellipses.Add(shape as EllipseShape);
		}
		else if (shape is PointShape)
		{
			points.Add(shape as PointShape);
		}
		else if (shape is PolyShape)
		{
			polys.Add(shape as PolyShape);
		}
		else if (shape is TextShape)
		{
			texts.Add(shape as TextShape);
		}
		else if (shape is CompoundShape)
		{
			AddShapes((shape as CompoundShape).components);
		}
		else
		{
			Debug.LogWarning("Unknown shape! " + shape);
		}
	}
}
