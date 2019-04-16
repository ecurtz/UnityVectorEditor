using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.VectorGraphics;

public class VectorLineMeshBuilder
{
	const int Buffer_Padding = 16;
	const int Buffer_Size = 16384 - Buffer_Padding;

	private List<Vector3> vertexList;
	private Vector3[] vertexArray;
	private int vertexCount;

	private List<Vector4> dataList;
	private Vector4[] dataArray;
	// dataCount == vertexCount

	private List<int> triangleList;
	private int[] triangleArray;
	private int triangleCount;

	private int vertexCountOnOpen;
	private Vector2 previousDirection;
	private float drawingLength;

	public VectorLineMeshBuilder()
	{
		vertexList = new List<Vector3>(Buffer_Size + Buffer_Padding);
		vertexArray = vertexList.GetBackingArray();
		vertexCount = 0;

		dataList = new List<Vector4>(Buffer_Size + Buffer_Padding);
		dataArray = dataList.GetBackingArray();

		triangleList = new List<int>(3 * (Buffer_Size + Buffer_Padding));
		triangleArray = triangleList.GetBackingArray();
		triangleCount = 0;

		vertexCountOnOpen = 0;
		drawingLength = 0f;
	}

	public void Reset()
	{
		vertexCount = 0;
		triangleCount = 0;
		vertexCountOnOpen = 0;
		drawingLength = 0f;
	}

	/// <summary>
	/// Open a new section of drawing.
	/// </summary>
	/// <param name="point">Initial vertex position</param>
	public void BeginPolyLine(Vector2 point)
	{
		if (vertexCount > vertexCountOnOpen)
		{
			Debug.LogError("BeginDrawing called on active drawing");
			return;
		}
		if (vertexCount > Buffer_Size)
		{
			Debug.LogWarning("Out of space in buffers");
			return;
		}

		vertexCountOnOpen = vertexCount;

		vertexArray[vertexCount++] = point;
		vertexArray[vertexCount++] = point;

		previousDirection = Vector2.zero;
		drawingLength = 0f;
	}

	/// <summary>
	/// Add a straight segment onto the drawing.
	/// </summary>
	/// <param name="point">New vertex position</param>
	public void LineTo(Vector2 point)
	{
		if (vertexCount <= vertexCountOnOpen)
		{
			Debug.LogError("LineTo called on inactive drawing");
			return;
		}
		if (vertexCount > Buffer_Size)
		{
			Debug.LogWarning("Out of space in buffers");
			return;
		}

		int previousVertex = vertexCount - 2;
		Vector2 segment = (point - (Vector2)vertexArray[previousVertex]);
		Vector2 direction = segment.normalized;
		Vector2 offset = Vector2.Perpendicular(direction);

		if (previousVertex == vertexCountOnOpen) // First segment
		{
			Vector4 data = new Vector4(0, 1, offset.x, offset.y);
			dataArray[previousVertex++] = data;
			dataArray[previousVertex++] = -data;

			drawingLength += segment.magnitude;
			data.x = drawingLength;

			dataArray[vertexCount] = data;
			vertexArray[vertexCount++] = point;
			dataArray[vertexCount] = -data;
			vertexArray[vertexCount++] = point;
		}
		else
		{
			Vector4 data;
			Vector4 previousData = dataArray[previousVertex];
			Vector2 previousOffset = new Vector2(previousData.z, previousData.w);

			float extent = 1f + Vector2.Dot(offset, previousOffset);

			if (extent < 0.5f)
			{
				Vector2 joinOffset;
				Vector2 midOffset;

				if (extent < Mathf.Epsilon)
				{
					joinOffset = previousDirection;
				}
				else
				{
					joinOffset = (offset + previousOffset).normalized;
				}

				extent = 1f + Vector2.Dot(previousOffset, joinOffset);
				midOffset = (previousOffset + joinOffset) / extent;
				data = new Vector4(drawingLength, 1, midOffset.x, midOffset.y);
				dataArray[previousVertex++] = data;
				dataArray[previousVertex--] = -data;

				data = new Vector4(drawingLength, 0, 0, 0);
				dataArray[vertexCount] = data;
				vertexArray[vertexCount++] = vertexArray[previousVertex];

				// cross product to check which way the corner goes
				float sign = -Mathf.Sign(previousDirection.x * direction.y - previousDirection.y * direction.x);
				data = new Vector4(drawingLength, sign, sign * joinOffset.x * extent, sign * joinOffset.y * extent);
				dataArray[vertexCount] = data;
				vertexArray[vertexCount++] = vertexArray[previousVertex];

				triangleArray[triangleCount++] = vertexCount - 2;
				if (sign > 0)
				{
					triangleArray[triangleCount++] = vertexCount - 4;
					triangleArray[triangleCount++] = vertexCount - 1;
				}
				else
				{
					triangleArray[triangleCount++] = vertexCount - 1;
					triangleArray[triangleCount++] = vertexCount - 3;
				}

				midOffset = (offset + joinOffset) / extent;
				data = new Vector4(drawingLength, 1, midOffset.x, midOffset.y);
				dataArray[vertexCount] = data;
				vertexArray[vertexCount++] = vertexArray[previousVertex];
				dataArray[vertexCount] = -data;
				vertexArray[vertexCount++] = vertexArray[previousVertex];

				triangleArray[triangleCount++] = vertexCount - 4;
				if (sign > 0)
				{
					triangleArray[triangleCount++] = vertexCount - 3;
					triangleArray[triangleCount++] = vertexCount - 2;
				}
				else
				{
					triangleArray[triangleCount++] = vertexCount - 1;
					triangleArray[triangleCount++] = vertexCount - 3;
				}
			}
			else
			{
				Vector2 joinOffset = (offset + previousOffset) / extent;
				data = new Vector4(drawingLength, 1, joinOffset.x, joinOffset.y);
				dataArray[previousVertex++] = data;
				dataArray[previousVertex++] = -data;
			}

			drawingLength += segment.magnitude;

			data = new Vector4(drawingLength, 1, offset.x, offset.y);
			dataArray[vertexCount] = data;
			vertexArray[vertexCount++] = point;
			dataArray[vertexCount] = -data;
			vertexArray[vertexCount++] = point;
		}

		triangleArray[triangleCount++] = vertexCount - 4;
		triangleArray[triangleCount++] = vertexCount - 2;
		triangleArray[triangleCount++] = vertexCount - 3;

		triangleArray[triangleCount++] = vertexCount - 2;
		triangleArray[triangleCount++] = vertexCount - 1;
		triangleArray[triangleCount++] = vertexCount - 3;

		previousDirection = direction;
	}

	/// <summary>
	/// Add a quadratic curve segment onto the drawing.
	/// </summary>
	/// <param name="control">Control point position</param>
	/// <param name="point">New vertex position</param>
	/// <param name="steps">Number of segments to include</param>
	public void CurveTo(Vector2 control, Vector2 point, int steps)
	{
		CurveTo(point, control, control, steps);
	}

	/// <summary>
	/// Add a cubic curve segment onto the drawing.
	/// </summary>
	/// <param name="controlA">Control point A position</param>
	/// <param name="controlB">Control point B position</param>
	/// <param name="point">New vertex position</param>
	/// <param name="steps">Number of segments to include</param>
	public void CurveTo(Vector2 controlA, Vector2 controlB, Vector2 point, int steps)
	{
		if (vertexCount <= vertexCountOnOpen)
		{
			Debug.LogError("CurveTo called on inactive drawing");
			return;
		}
		if (vertexCount > Buffer_Size)
		{
			Debug.LogWarning("Out of space in buffers");
			return;
		}

		int previousVertex = vertexCount - 2;
		Vector2 previousPoint = vertexArray[previousVertex];

		BezierSegment bezier = new BezierSegment();
		bezier.P0 = previousPoint;
		bezier.P1 = controlA;
		bezier.P2 = controlB;
		bezier.P3 = point;

		float length = VectorUtils.SegmentLength(bezier) / steps;

		float step = 1f / steps;
		float t = step;

		Vector2 bezierPoint = VectorUtils.Eval(bezier, t);
		Vector2 tangent = VectorUtils.EvalTangent(bezier, t);
		Vector2 offset = Vector2.Perpendicular(tangent);
		Vector4 data;

		LineTo(bezierPoint);

		for (int i = 1; i < steps; i++)
		{
			t += step;
			drawingLength += length; // Good enough for our purposes.

			bezierPoint = VectorUtils.EvalFull(bezier, t, out tangent);
			offset = Vector2.Perpendicular(tangent);
			data = new Vector4(drawingLength, 1, offset.x, offset.y);
			dataArray[vertexCount] = data;
			vertexArray[vertexCount++] = bezierPoint;
			dataArray[vertexCount] = -data;
			vertexArray[vertexCount++] = bezierPoint;

			triangleArray[triangleCount++] = vertexCount - 4;
			triangleArray[triangleCount++] = vertexCount - 2;
			triangleArray[triangleCount++] = vertexCount - 3;

			triangleArray[triangleCount++] = vertexCount - 2;
			triangleArray[triangleCount++] = vertexCount - 1;
			triangleArray[triangleCount++] = vertexCount - 3;
		}

		previousDirection = tangent;
	}

	public void EndPolyLine(bool closed = false)
	{
		if (closed)
		{
		}
		else if (vertexCount > vertexCountOnOpen)
		{
			Vector2 offset = Vector2.Perpendicular(previousDirection);
			Vector4 data = new Vector4(drawingLength, 1, offset.x, offset.y);
			dataArray[vertexCount - 2] = data;
			dataArray[vertexCount - 1] = -data;
		}

		vertexCountOnOpen = vertexCount;
	}

	public void Circle(Vector2 center, float radius, int steps)
	{
		if (vertexCount > vertexCountOnOpen)
		{
			Debug.LogError("Circle called on active drawing");
			return;
		}
		if ((vertexCount + steps) > Buffer_Size)
		{
			Debug.LogWarning("Out of space in buffers");
			return;
		}

		vertexCountOnOpen = vertexCount;

		int segments = Mathf.CeilToInt(steps / 4f);
		float angle = 0f;
		float angleDelta = Mathf.PI / (2 * segments);
		Vector2[] offsets = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			offsets[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			angle += angleDelta;
		}

		Vector2 offset;
		Vector2 point;
		Vector4 data;

		float length = angleDelta * radius;
		for (int quadrant = 0; quadrant < 4; quadrant++)
		{
			for (int i = 0; i < segments; i++)
			{
				offset = offsets[i];
				point = center + offset * radius;
				data = new Vector4(drawingLength, 1, -offset.x, -offset.y);

				dataArray[vertexCount] = data;
				vertexArray[vertexCount++] = point;
				dataArray[vertexCount] = -data;
				vertexArray[vertexCount++] = point;

				// This is ugly, but we'll clean it up after the loop
				triangleArray[triangleCount++] = vertexCount - 2;
				triangleArray[triangleCount++] = vertexCount - 0;
				triangleArray[triangleCount++] = vertexCount - 1;

				triangleArray[triangleCount++] = vertexCount - 0;
				triangleArray[triangleCount++] = vertexCount + 1;
				triangleArray[triangleCount++] = vertexCount - 1;

				drawingLength += length;

				offsets[i].x = -offset.y;
				offsets[i].y = offset.x;
			}
		}

		// Fix the out of bounds triangles by repeating first point
		offset = offsets[0];
		point = center + offset * radius;
		data = new Vector4(drawingLength, 1, -offset.x, -offset.y);

		dataArray[vertexCount] = data;
		vertexArray[vertexCount++] = point;
		dataArray[vertexCount] = -data;
		vertexArray[vertexCount++] = point;

		vertexCountOnOpen = vertexCount;
	}

	public Mesh GetMesh()
	{
		Mesh mesh = new Mesh();

		vertexList.SetActiveSize(vertexCount);
		mesh.SetVertices(vertexList);

		dataList.SetActiveSize(vertexCount);
		mesh.SetUVs(0, dataList);

		triangleList.SetActiveSize(triangleCount);
		mesh.SetTriangles(triangleList, 0);

		mesh.UploadMeshData(false);
		return mesh;
	}
}

// Extension class for System.Collections.Generic.List<T> to get
// its backing array field via reflection.
// Author: Jackson Dunstan, http://JacksonDunstan.com/articles/3066
public static class ListBackingArrayGetter
{
	// Name of the backing array field
	private const string FieldName = "_items";

	// Flags passed to Type.GetField to get the backing array field
	private const BindingFlags GetFieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

	// Cached backing array FieldInfo instances per Type
	private static readonly Dictionary<System.Type, FieldInfo> itemsFields = new Dictionary<System.Type, FieldInfo>();

	// Get a List's backing array
	public static TElement[] GetBackingArray<TElement>(this List<TElement> list)
	{
		// Check if the FieldInfo is already in the cache
		var listType = typeof(List<TElement>);
		FieldInfo fieldInfo;
		if (!itemsFields.TryGetValue(listType, out fieldInfo))
		{
			// Generate the FieldInfo and add it to the cache
			fieldInfo = listType.GetField(FieldName, GetFieldFlags);
			itemsFields.Add(listType, fieldInfo);
		}

		// Get the backing array of the given List
		var items = (TElement[])fieldInfo.GetValue(list);
		return items;
	}
}

// Extension class for System.Collections.Generic.List<T> to set
// the value of its active size field via reflection.
public static class ListSizeSetter
{
	// Name of the size field
	private const string FieldName = "_size";

	// Flags passed to Type.GetField to get the size field
	private const BindingFlags GetFieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

	// Cached backing array FieldInfo instances per Type
	private static readonly Dictionary<System.Type, FieldInfo> itemsFields = new Dictionary<System.Type, FieldInfo>();

	// Set a List's active size
	public static void SetActiveSize<TElement>(this List<TElement> list, int size)
	{
		// Check if the FieldInfo is already in the cache
		var listType = typeof(List<TElement>);
		FieldInfo fieldInfo;
		if (!itemsFields.TryGetValue(listType, out fieldInfo))
		{
			// Generate the FieldInfo and add it to the cache
			fieldInfo = listType.GetField(FieldName, GetFieldFlags);
			itemsFields.Add(listType, fieldInfo);
		}

		// Set the active size of the given List
		int newSize = size;
		if (newSize < 0) newSize = 0;
		if (newSize > list.Capacity) newSize = list.Capacity;

		fieldInfo.SetValue(list, newSize);
	}
}
