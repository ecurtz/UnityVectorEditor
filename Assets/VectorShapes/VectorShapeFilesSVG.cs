using System.IO;
using System.Xml;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

public class VectorShapeFilesSVG
{
	/// <summary>
	/// Measurement units for SVG files.
	/// </summary>
	public enum Unit
	{
		None,
		Pixels,
		Millimeters,
		Centimeters,
		Inches
	}
	private const float InchToMM = 25.4f;
	private const float InchToCM = 2.54f;

	/// <summary>
	/// Convert between SVG units.
	/// </summary>
	public static string UnitSuffix(Unit unit)
	{
		switch (unit)
		{
			case Unit.Pixels:
				return "px";
			case Unit.Millimeters:
				return "mm";
			case Unit.Centimeters:
				return "cm";
			case Unit.Inches:
				return "in";
			default:
				return "";
		}
	}


	/// <summary>
	/// Convert between SVG units.
	/// </summary>
	public static float ConvertUnits(float measurement, Unit fromUnit, Unit toUnit)
	{
		float result = measurement;

		switch (fromUnit)
		{
			case Unit.Pixels:
				switch (toUnit)
				{
					case Unit.Millimeters:
						result = measurement / Screen.dpi * InchToMM;
						break;
					case Unit.Centimeters:
						result = measurement / Screen.dpi * InchToCM;
						break;
					case Unit.Inches:
						result = measurement / Screen.dpi;
                 		break;
				}
				break;

			case Unit.Millimeters:
				switch (toUnit)
				{
					case Unit.Pixels:
						result = measurement * Screen.dpi / InchToMM;
						break;
					case Unit.Centimeters:
						result = measurement / 10f;
						break;
					case Unit.Inches:
						result = measurement / InchToMM;
						break;
				}
				break;

			case Unit.Centimeters:
				switch (toUnit)
				{
					case Unit.Pixels:
						result = measurement * Screen.dpi / InchToCM;
						break;
					case Unit.Millimeters:
						result = measurement * 10f;
						break;
					case Unit.Inches:
						result = measurement / InchToCM;
						break;
				}
				break;

			case Unit.Inches:
				switch (toUnit)
				{
					case Unit.Pixels:
						result = measurement * Screen.dpi;
						break;
					case Unit.Millimeters:
						result = measurement * InchToMM;
						break;
					case Unit.Centimeters:
						result = measurement * InchToCM;
						break;
				}
				break;
		}

		return result;
	}

	/// <summary>
	/// Convert Unity color to SVG string
	/// </summary>
	public static string ConvertColor(Color color)
	{
		if (color.a <= Mathf.Epsilon) return "none";

		return "#" + ColorUtility.ToHtmlStringRGB(color);
	}

	private static VectorShape ParseContour(BezierContour contour, Matrix2D transform)
	{
		VectorShape vectorShape = PolyShape.Create(contour);
		vectorShape.TransformBy(transform);

		return vectorShape;
	}

	private static VectorShape TryParseShapeToCircle(Shape shape, Matrix2D transform)
	{
		if (shape.Contours.Length > 1) return null;

		BezierContour contour = shape.Contours[0];
		if (contour.Segments.Length < 5) return null;
		if (!contour.Closed) return null;

		BezierSegment[] segments = new BezierSegment[contour.Segments.Length - 1];
		for (int i = 0; i < segments.Length; i++)
		{
			segments[i].P0 = transform.MultiplyPoint(contour.Segments[i].P0);
			segments[i].P1 = transform.MultiplyPoint(contour.Segments[i].P1);
			segments[i].P2 = transform.MultiplyPoint(contour.Segments[i].P2);
			segments[i].P3 = transform.MultiplyPoint(contour.Segments[(i + 1)].P0);
		}

		Rect shapeBounds = VectorUtils.Bounds(VectorUtils.BezierSegmentsToPath(segments));
		Vector2 center = shapeBounds.center;
		float radius = (shapeBounds.width + shapeBounds.height) / 4f;
		float error = radius / 200f;
		for (int i = 0; i < segments.Length; i++)
		{
			if (Mathf.Abs(Vector2.Distance(center, segments[i].P0) - radius) > error)
			{
				return null;
			}

			Vector2 midpoint = VectorUtils.Eval(segments[i], 0.5f);
			if (Mathf.Abs(Vector2.Distance(center, midpoint) - radius) > error)
			{
				return null;
			}
		}

		CircleShape circle = CircleShape.Create(center, radius);
		circle.colorOutline = Color.red;
		return circle;
	}

	private static VectorShape ParseShape(Shape shape, Matrix2D transform)
	{
		VectorShape vectorShape = PolyShape.Create(shape, transform);

		return vectorShape;
	}

	/// <summary>
	/// Add node and children into shape list.
	/// </summary>
	public static void RecurseSVGNodes(SceneNode node, Matrix2D nodeTransform, List<VectorShape> shapes)
	{
		if (node.Shapes != null)
		{
			foreach (Shape shape in node.Shapes)
			{
				VectorShape vectorShape = TryParseShapeToCircle(shape, nodeTransform);
				if (vectorShape == null)
				{
					vectorShape = ParseShape(shape, nodeTransform);
				}
				if (vectorShape != null)
				{
					shapes.Add(vectorShape);
				}
				//foreach (BezierContour contour in shape.Contours)
				//{
				//	VectorShape vectorShape = ParseContour(contour, nodeTransform);
				//	if (vectorShape != null)
				//	{
				//		shapes.Add(vectorShape);
				//		if (shape.PathProps.Stroke != null)
				//		{
				//			vectorShape.colorOutline = shape.PathProps.Stroke.Color;
				//			if ((vectorShape.colorOutline.g > 0.99f) && (vectorShape.colorOutline.b < Mathf.Epsilon) && (vectorShape.colorOutline.r < Mathf.Epsilon))
				//			{
				//				foreach (BezierPathSegment segment in contour.Segments)
				//				{
				//					Debug.Log(nodeTransform.MultiplyPoint(segment.P0).x);
				//				}
				//			}
				//		}
				//	}
				//}
			}
		}

		if (node.Children != null)
		{
			foreach (SceneNode child in node.Children)
			{
				Matrix2D childTransform = nodeTransform * child.Transform;
				RecurseSVGNodes(child, childTransform, shapes);
			}
		}
	}

	/// <summary>
	/// Parse SVG into VectorShape list.
	/// </summary>
	public static List<VectorShape> ReadSVG(System.IO.TextReader svg)
	{
		List<VectorShape> shapes = new List<VectorShape>();

		SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(svg);
		//Debug.Log(sceneInfo.SceneViewport);

		Matrix2D rootTransform = Matrix2D.Scale(new Vector2(0.01f, -0.01f)) * sceneInfo.Scene.Root.Transform;
		RecurseSVGNodes(sceneInfo.Scene.Root, rootTransform, shapes);

		return shapes;
	}

	protected XmlWriter svgWriter;
	protected Unit svgUnits;

	/// <summary>
	/// .
	/// </summary>
	public VectorShapeFilesSVG()
	{
	}

	/// <summary>
	/// Initialize an XmlWriter for outputting svg data to a stream.
	/// </summary>
	/// <param name="stream">Output stream</param>
	/// <param name="bounds">Document rect</param>
	/// <param name="unit">Unit for measurements</param>
	public void Open(Stream stream, Rect bounds, Unit unit = Unit.Millimeters)
	{
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.Indent = true;
		settings.NewLineOnAttributes = false;

		svgWriter = XmlWriter.Create(stream, settings);
		svgUnits = unit;

		svgWriter.WriteStartDocument();
		svgWriter.WriteStartElement("svg", "http://www.w3.org/2000/svg");

		svgWriter.WriteStartAttribute("width");
		svgWriter.WriteValue(bounds.width);
		svgWriter.WriteValue(UnitSuffix(unit));
		svgWriter.WriteEndAttribute();

		svgWriter.WriteStartAttribute("height");
		svgWriter.WriteValue(bounds.height);
		svgWriter.WriteValue(UnitSuffix(unit));
		svgWriter.WriteEndAttribute();

		svgWriter.WriteStartAttribute("viewBox");
		svgWriter.WriteValue(bounds.x);
		svgWriter.WriteValue(" ");
		svgWriter.WriteValue(-(bounds.y + bounds.height));
		svgWriter.WriteValue(" ");
		svgWriter.WriteValue(bounds.width);
		svgWriter.WriteValue(" ");
		svgWriter.WriteValue(bounds.height);
		svgWriter.WriteEndAttribute();
	}

	/// <summary>
	/// Open a new element tag in the xml.
	/// </summary>
	/// <param name="element">ID of element block to open</param>
	public void OpenElement(string element)
	{
		svgWriter.WriteStartElement(element);
	}

	/// <summary>
	/// Close an element tag in the xml.
	/// </summary>
	/// <param name="element">ID of element block to close (ignored)</param>
	public void CloseElement(string element = null)
	{
		svgWriter.WriteEndElement();
	}

	/// <summary>
	/// Add a list of shapes to SVG as a new group.
	/// </summary>
	/// <param name="shapes">List of shapes</param>
	/// <param name="id">ID of group</param>
	public void AddShapeGroup(List<VectorShape> shapes, string id)
	{
		svgWriter.WriteStartElement("g");

		svgWriter.WriteStartAttribute("id");
		svgWriter.WriteValue(id);
		svgWriter.WriteEndAttribute();

		foreach (VectorShape shape in shapes)
		{
			shape.WriteToXML(svgWriter, Vector2.zero, 1f);
		}

		svgWriter.WriteEndElement();
	}

	/// <summary>
	/// Add an element that reuses a defined object.
	/// </summary>
	public void AddUseElement(string id, Vector2 position, float rotation)
	{
		svgWriter.WriteStartElement("use");

		svgWriter.WriteStartAttribute("xlink", "href", "http://www.w3.org/1999/xlink");
		svgWriter.WriteValue("#" + id);
		svgWriter.WriteEndAttribute();

		svgWriter.WriteStartAttribute("transform");
		svgWriter.WriteValue("translate(" + position.x + ", " + -position.y + ") rotate(" + -rotation + ")");
		svgWriter.WriteEndAttribute();

		svgWriter.WriteEndElement();
	}

	/// <summary>
	/// Close the writer.
	/// </summary>
	public void Close()
	{
		if (svgWriter == null) return;

		svgWriter.WriteEndElement();
		svgWriter.WriteEndDocument();

		svgWriter.Close();
	}
}
