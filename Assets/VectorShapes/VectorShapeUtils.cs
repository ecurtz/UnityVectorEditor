using UnityEngine;

public static class VectorShapeUtils
{
	/// <summary>
	/// Union of two Rects
	/// </summary>
	/// <returns>The union.</returns>
	/// <param name="rectA">Rect A</param>
	/// <param name="rectB">Rect B</param>
	public static Rect RectUnion(Rect rectA, Rect rectB)
	{
		return Rect.MinMaxRect(Mathf.Min(rectA.xMin, rectB.xMin),
							   Mathf.Min(rectA.yMin, rectB.yMin),
							   Mathf.Max(rectA.xMax, rectB.xMax),
							   Mathf.Max(rectA.yMax, rectB.yMax));
	}


	/// <summary>
	/// Point on a quadratic curve between pt0 and pt2.
	/// </summary>
	/// <param name="pt0">Starting point</param>
	/// <param name="pt1">Control point</param>
	/// <param name="pt2">Ending point</param>
	/// <param name="t">Distance along curve</param>
	public static Vector2 EvaluateQuadraticCurve(Vector2 pt0, Vector2 pt1, Vector2 pt2, float t)
	{
		Vector2 p0 = Vector2.Lerp(pt0, pt1, t);
		Vector2 p1 = Vector2.Lerp(pt1, pt2, t);
		return Vector2.Lerp(p0, p1, t);
	}

	/// <summary>
	/// Point on a cubic curve between pt0 and pt3.
	/// </summary>
	/// <param name="pt0">Starting point</param>
	/// <param name="pt1">Control point</param>
	/// <param name="pt2">Control point</param>
	/// <param name="pt3">Ending point</param>
	/// <param name="t">Distance along curve</param>
	public static Vector2 EvaluateCubicCurve(Vector2 pt0, Vector2 pt1, Vector2 pt2, Vector2 pt3, float t)
	{
		Vector2 p0 = EvaluateQuadraticCurve(pt0, pt1, pt2, t);
		Vector2 p1 = EvaluateQuadraticCurve(pt1, pt2, pt3, t);
		return Vector2.Lerp(p0, p1, t);
	}

	/// <summary>
	/// Closest point on a line segment to given point.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="segA">Start of line segment</param>
	/// <param name="segB">End of line segment</param>
	/// <returns>Closet point on segment</returns>
	public static Vector2 ClosestPointOnLineSegment(Vector2 pt, Vector2 segA, Vector2 segB)
	{
		float segLength = (segB - segA).sqrMagnitude;
		if (segLength < Mathf.Epsilon) // Segment is actually a point
			return segA;

		float t = Vector2.Dot(pt - segA, segB - segA) / segLength;
		if (t < 0.0) // Beyond the 'a' end of the segment
			return segA;
		if (t > 1.0) // Beyond the 'b' end of the segment
			return segB;

		// Projection falls on the segment
		Vector2 projection = segA + t * (segB - segA);
		return projection;
	}

	/// <summary>
	/// Distance between a point and a line segment.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="segA">Start of line segment</param>
	/// <param name="segB">End of line segment</param>
	/// <returns>Distance</returns>
	public static float DistancePointToLineSegment(Vector2 pt, Vector2 segA, Vector2 segB)
	{
		Vector2 closest = ClosestPointOnLineSegment(pt, segA, segB);
		return Vector2.Distance(pt, closest);
	}

	/// <summary>
	/// Number of steps when approximating Bezier curves.
	/// </summary>
	public static int bezierSteps = 12;

	/// <summary>
	/// APPROXIMATE closest point on a bezier curve segment to given point.
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="curveA">Start of curve</param>
	/// <param name="controlA">Control point A</param>
	/// <param name="controlB">Control point B</param>
	/// <param name="curveB">End of curve</param>
	/// <returns>Closest point on the curve (approximate)</returns>
	public static Vector2 ClosetPointOnBezierCurve(Vector2 pt, Vector2 curveA, Vector2 controlA, Vector2 controlB, Vector2 curveB)
	{
		Vector2 closest = curveA;
		float sqrDistance = (pt - curveA).sqrMagnitude;

		float step = 1f / bezierSteps;
		float t = step;
		for (int i = 1; i < bezierSteps; i++)
		{
			Vector2 curvePt = EvaluateCubicCurve(curveA, controlA, controlB, curveB, t);
			float sqrDistance2 = (pt - curvePt).sqrMagnitude;
			if (sqrDistance2 < sqrDistance)
			{
				sqrDistance = sqrDistance2;
				closest = curvePt;
			}

			t += step;
		}

		return closest;
	}

	/// <summary>
	/// Distance between a point and a Bezier curve
	/// </summary>
	/// <param name="pt">Test point</param>
	/// <param name="curveA">Start of curve</param>
	/// <param name="controlA">Control point A</param>
	/// <param name="controlB">Control point B</param>
	/// <param name="curveB">End of curve</param>
	/// <returns>Distance (approximate)</returns>
	public static float DistancePointToBezierCurve(Vector2 pt, Vector2 curveA, Vector2 controlA, Vector2 controlB, Vector2 curveB)
	{
		Vector2 closest = ClosetPointOnBezierCurve(pt, curveA, controlA, curveB, controlB);

		return Vector2.Distance(pt, closest);
	}
}
