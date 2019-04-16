using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

#if DXF_SUPPORT

using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

public static class VectorShapeFilesDXF
{
	static float dxfScale = 1f;
	public static Vector2 ConvertPoint(DxfPoint pt)
	{
		return new Vector2((float)pt.X * dxfScale, (float)pt.Y * dxfScale);
	}
	public static Vector2 ConvertPoint(double ptX, double ptY)
	{
		return new Vector2((float)ptX * dxfScale, (float)ptY * dxfScale);
	}
	public static Vector2 ConvertVector(DxfVector vector)
	{
		return new Vector2((float)vector.X * dxfScale, (float)vector.Y * dxfScale);
	}

	public static Color32 ConvertColor(DxfColor color)
	{
		int intColor = 0;
		try
		{
			color.ToRGB();
		}
		catch (System.NotSupportedException)
		{
		}

		byte colorRed = (byte)((intColor >> 16) & 0xFF);
		byte colorGreen = (byte)((intColor >> 8) & 0xFF);
		byte colorBlue = (byte)((intColor >> 0) & 0xFF);
		return new Color32(colorRed, colorGreen, colorBlue, 255);
	}

	/// <summary>
	/// Parse DXF into VectorShape list.
	/// </summary>
	public static List<VectorShape> ReadDXF(Stream dxfStream)
	{
		List<VectorShape> shapes = new List<VectorShape>();

		DxfFile dxfFile = DxfFile.Load(dxfStream);

		Dictionary<string, Color32> layerColors = new Dictionary<string, Color32>();
		foreach (DxfLayer layer in dxfFile.Layers)
		{
			layerColors.Add(layer.Name, ConvertColor(layer.Color));
		}

		foreach (DxfEntity entity in dxfFile.Entities)
		{
			VectorShape shape = null;

			switch (entity.EntityType)
			{
				case DxfEntityType.Point:
					DxfModelPoint point = entity as DxfModelPoint;
					shape = new PointShape(ConvertPoint(point.Location));
					break;

				case DxfEntityType.Line:
					DxfLine line = entity as DxfLine;
					Vector2[] endpoints = new Vector2[2];
					endpoints[0] = ConvertPoint(line.P1);
					endpoints[1] = ConvertPoint(line.P2);
					shape = new PolyShape(endpoints);
					break;

				case DxfEntityType.Spline:
					DxfSpline spline = entity as DxfSpline;
					if ((spline.NumberOfControlPoints % spline.DegreeOfCurve) != 1)
					{
						Debug.LogError("Invalid spline data! Wrong number of points. " + spline);
						break;
					}

					Vector2[] controlPoints = new Vector2[spline.NumberOfControlPoints];
					for (int i = 0; i < controlPoints.Length; i++)
					{
						controlPoints[i] = ConvertPoint(spline.ControlPoints[i]);
					}
					shape = new PolyShape(controlPoints[0]);
					PolyShape shapeSpline = shape as PolyShape;

					switch (spline.DegreeOfCurve)
					{
						case 1:

							for (int i = 1; i < controlPoints.Length; i++)
							{
								shapeSpline.LineTo(controlPoints[i]);
							}
							break;

						case 2:
							for (int i = 1; i < controlPoints.Length; i += 2)
							{
								shapeSpline.CurveTo(controlPoints[i + 1], controlPoints[i]);
							}
							break;

						case 3:
							for (int i = 1; i < controlPoints.Length; i += 3)
							{
								shapeSpline.CurveTo(controlPoints[i + 2], controlPoints[i], controlPoints[i + 1]);
							}
							break;

						default:
							Debug.LogWarning("Spline with unsupported curve of degree: " + spline.DegreeOfCurve);
							break;
					}
					break;

				case DxfEntityType.Arc:
					DxfArc arc = entity as DxfArc;
					// If the arc is a complete circle just make one of those
					float startAngle = (float)arc.StartAngle;
					while (startAngle < 0f)
					{
						startAngle += 360f;
					}
					float endAngle = (float)arc.EndAngle;
					while (endAngle < startAngle)
					{
						endAngle += 360f;
					}

					float sweep = endAngle - startAngle;
					shape = new CircleShape(ConvertPoint(arc.Center), (float)arc.Radius, startAngle, sweep);
					break;

				case DxfEntityType.Circle:
					DxfCircle circle = entity as DxfCircle;
					shape = new CircleShape(ConvertPoint(circle.Center), (float)circle.Radius * dxfScale);
					break;

				case DxfEntityType.Ellipse:
					DxfEllipse ellipse = entity as DxfEllipse;
					// If the ellipse is actually a circle just make one of those
					if (Mathf.Approximately((float)ellipse.MinorAxisRatio, 1f))
					{
						shape = new CircleShape(ConvertPoint(ellipse.Center), (float)ellipse.MajorAxis.Length * dxfScale);
					}
					else
					{
						shape = new EllipseShape(ConvertPoint(ellipse.Center), ConvertVector(ellipse.MajorAxis), (float)ellipse.MinorAxisRatio);
					} 
					break;

				case DxfEntityType.Polyline:
					DxfPolyline polyline = entity as DxfPolyline;
					if (polyline.ContainsVertices)
					{
						Vector2[] vertices = new Vector2[polyline.Vertices.Count];
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = ConvertPoint(polyline.Vertices[i].Location);
						}

						shape = new PolyShape(vertices[0]);
						PolyShape shapePolyline = shape as PolyShape;

						for (int i = 1; i < vertices.Length; i++)
						{
							float bulge = (float)polyline.Vertices[i - 1].Bulge;
							shapePolyline.ArcToDXF(vertices[i], bulge);
						}

						if (polyline.IsClosed)
						{
							float bulge = (float)polyline.Vertices[vertices.Length - 1].Bulge;
							shapePolyline.ArcToDXF(vertices[0], bulge);
							shape.Closed = true;
						}
					}
					break;

				case DxfEntityType.LwPolyline:
					{
						DxfLwPolyline lwPolyline = entity as DxfLwPolyline;
						Vector2[] vertices = new Vector2[lwPolyline.Vertices.Count];
						for (int i = 0; i < vertices.Length; i++)
						{
							DxfLwPolylineVertex lwpVertex = lwPolyline.Vertices[i];
							vertices[i] = ConvertPoint(lwpVertex.X, lwpVertex.Y);
						}

						shape = new PolyShape(vertices[0]);
						PolyShape shapePolyline = shape as PolyShape;

						for (int i = 1; i < vertices.Length; i++)
						{
							float bulge = (float)lwPolyline.Vertices[i - 1].Bulge;
							shapePolyline.ArcToDXF(vertices[i], bulge);
						}

						if (lwPolyline.IsClosed)
						{
							float bulge = (float)lwPolyline.Vertices[vertices.Length - 1].Bulge;
							shapePolyline.ArcToDXF(vertices[0], bulge);
							shape.Closed = true;
						}
					}
					break;

				default:
					Debug.Log("Unhandled entity of type: " + entity.EntityType);
					break;
			}

			if (shape != null)
			{
				if (entity.IsVisible)
				{
					Color32 shapeColor = ConvertColor(entity.Color);
					//layerColors.TryGetValue(entity.Layer, out shapeColor);

					shape.colorOutline = shapeColor;
					shapes.Add(shape);
				}
			}
		}

		return shapes;
	}
}

#endif
