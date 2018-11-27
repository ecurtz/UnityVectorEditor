using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class VectorShapeIcons
{
	static Material renderMaterial;

	static VectorShapeIcons()
	{
		renderMaterial = new Material(Shader.Find("Unlit/Vector"));
	}

#if UNITY_EDITOR
	/// <summary>
	/// Create a Texture2D icon of a SVG image (editor only version).
	/// </summary>
	/// <param name="svg">String containing svg content.</param>
	/// <param name="renderUtil">PreviewRenderUtility to use for drawing</param>
	public static Texture2D GetIcon(string svg, PreviewRenderUtility renderUtil)
	{
		// Parse the SVG
		SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(new System.IO.StringReader(svg));
		int width = Mathf.CeilToInt(sceneInfo.SceneViewport.width);
		int height = Mathf.CeilToInt(sceneInfo.SceneViewport.height);
		if ((width > 64) || (height > 64)) Debug.LogWarning("SVG icon of unusual size!");

		// Save the render state and get a temporary render texture
		RenderTexture activeTexture = RenderTexture.active;
		renderUtil.camera.targetTexture = RenderTexture.GetTemporary(width, height, 8, RenderTextureFormat.ARGB32);
		renderUtil.camera.backgroundColor = Color.clear;

		// Generate the mesh
		Mesh iconMesh = new Mesh();
		VectorUtils.TessellationOptions tessellationOptions = new VectorUtils.TessellationOptions()
		{
			StepDistance = 0.05f,
			MaxCordDeviation = float.MaxValue,
			MaxTanAngleDeviation = Mathf.PI / 2.0f,
			SamplingStepSize = 0.01f
		};
		List<VectorUtils.Geometry> iconGeometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);
		VectorUtils.FillMesh(iconMesh, iconGeometry, 1f);

		// Activate the render texture and draw the mesh into it
		RenderTexture.active = renderUtil.camera.targetTexture;
		float cameraSize = renderUtil.camera.orthographicSize;
		Vector3 cameraPosition = renderUtil.camera.transform.position;
		renderUtil.camera.orthographicSize = sceneInfo.SceneViewport.height / 2;
		renderUtil.camera.transform.position = new Vector3(sceneInfo.SceneViewport.center.x, sceneInfo.SceneViewport.center.y, -1);
		// HACK until FillMesh() flpYAxis is fixed
		renderUtil.camera.transform.Rotate(0, 0, 180f);
		renderUtil.DrawMesh(iconMesh, Matrix4x4.identity, renderMaterial, 0);
		renderUtil.camera.Render();
		renderUtil.camera.transform.Rotate(0, 0, 180f);
		renderUtil.camera.orthographicSize = cameraSize;
		renderUtil.camera.transform.position = cameraPosition;
		Texture2D iconTexture = new Texture2D(width, height);
		iconTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		iconTexture.Apply();

		// Restore the render state and release the temporary render texture
		RenderTexture.active = activeTexture;
		RenderTexture.ReleaseTemporary(renderUtil.camera.targetTexture);

		return iconTexture;
	}

	/// <summary>
	/// Create a Texture2D icon out of a VectorShape (editor only version).
	/// </summary>
	public static Texture2D GetIcon(VectorShape shape, PreviewRenderUtility renderUtil)
	{
		Rect shapeBounds = shape.ShapeBounds;
		int width = Mathf.CeilToInt(shapeBounds.width);
		int height = Mathf.CeilToInt(shapeBounds.height);

		// Save the render state and get a temporary render texture
		RenderTexture activeTexture = RenderTexture.active;
		renderUtil.camera.targetTexture = RenderTexture.GetTemporary(width, height, 8, RenderTextureFormat.ARGB32);
		renderUtil.camera.backgroundColor = Color.clear;
		Matrix4x4 drawMatrix = Matrix4x4.identity;
		renderUtil.DrawMesh(shape.ShapeMesh, drawMatrix, renderMaterial, 0);

		// Activate the render texture and draw the shape into it
		RenderTexture.active = renderUtil.camera.targetTexture;
		float cameraSize = renderUtil.camera.orthographicSize;
		Vector3 cameraPosition = renderUtil.camera.transform.position;
		renderUtil.camera.orthographicSize = shapeBounds.height / 2;
		renderUtil.camera.transform.position = new Vector3(shapeBounds.center.x, shapeBounds.center.y, -1);
		renderUtil.camera.Render();
		renderUtil.camera.orthographicSize = cameraSize;
		renderUtil.camera.transform.position = cameraPosition;
		Texture2D iconTexture = new Texture2D(width, height);
		iconTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		iconTexture.Apply();

		// Restore the render state and release the temporary render texture
		RenderTexture.active = activeTexture;
		RenderTexture.ReleaseTemporary(renderUtil.camera.targetTexture);

		return iconTexture;
	}
#endif

	/// <summary>
	/// Create a Texture2D icon of a SVG image.
	/// </summary>
	/// <param name="svg">String containing svg content.</param>
	public static Texture2D GetIcon(string svg)
	{
		// Parse the SVG
		SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(new System.IO.StringReader(svg));
		int width = Mathf.CeilToInt(sceneInfo.SceneViewport.width);
		int height = Mathf.CeilToInt(sceneInfo.SceneViewport.height);
		if ((width > 64) || (height > 64)) Debug.LogWarning("SVG icon of unusual size!");

		VectorUtils.TessellationOptions tessellationOptions = new VectorUtils.TessellationOptions()
		{
			StepDistance = 0.05f,
			MaxCordDeviation = float.MaxValue,
			MaxTanAngleDeviation = Mathf.PI / 2.0f,
			SamplingStepSize = 0.01f
		};
		List<VectorUtils.Geometry> iconGeometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);

		Sprite sprite = VectorUtils.BuildSprite(iconGeometry, 1f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
		Texture2D iconTexture = VectorUtils.RenderSpriteToTexture2D(sprite, width, height, renderMaterial);

		return iconTexture;
	}

	/// <summary>
	/// Create a Texture2D icon out of a VectorShape.
	/// </summary>
	public static Texture2D GetIcon(VectorShape shape)
	{
		Rect shapeBounds = shape.ShapeBounds;
		int width = Mathf.CeilToInt(shapeBounds.width);
		int height = Mathf.CeilToInt(shapeBounds.height);
		List<VectorUtils.Geometry> iconGeometry = shape.ShapeGeometry;

		Sprite sprite = VectorUtils.BuildSprite(iconGeometry, 1f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
		Texture2D iconTexture = VectorUtils.RenderSpriteToTexture2D(sprite, width, height, renderMaterial);

		return iconTexture;
	}

	const string svgIconHeader =
		"<?xml version = \"1.0\" encoding=\"UTF-8\"?>" +
		"<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">" +
		"<svg xmlns = \"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\">";

	const string svgIconFooter =
		"</svg>";

	// Editing tools
	public static string iconView =
		svgIconHeader +
		"<path d = \"M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z\"/>" +
		svgIconFooter;

	public static string iconBrush =
		svgIconHeader +
		"<path d = \"M20.71,4.63L19.37,3.29C19,2.9 18.35,2.9 17.96,3.29L9,12.25L11.75,15L20.71,6.04C21.1,5.65 21.1,5 20.71,4.63M7,14A3,3 0 0,0 4,17C4,18.31 2.84,19 2,19C2.92,20.22 4.5,21 6,21A4,4 0 0,0 10,17A3,3 0 0,0 7,14Z\"/>" +
		svgIconFooter;
	
	public static string iconMagnify =
		svgIconHeader +
		"<path d = \"M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z\"/>" +
		svgIconFooter;

	public static string iconArrowAll =
		svgIconHeader +
		"<path d = \"M13,11H18L16.5,9.5L17.92,8.08L21.84,12L17.92,15.92L16.5,14.5L18,13H13V18L14.5,16.5L15.92,17.92L12,21.84L8.08,17.92L9.5,16.5L11,18V13H6L7.5,14.5L6.08,15.92L2.16,12L6.08,8.08L7.5,9.5L6,11H11V6L9.5,7.5L8.08,6.08L12,2.16L15.92,6.08L14.5,7.5L13,6V11Z\"/>" +
		svgIconFooter;

	public static string iconSelectionRect =
		svgIconHeader +
		"<path d = \"M4,3H5V5H3V4A1,1 0 0,1 4,3M20,3A1,1 0 0,1 21,4V5H19V3H20M15,5V3H17V5H15M11,5V3H13V5H11M7,5V3H9V5H7M21,20A1,1 0 0,1 20,21H19V19H21V20M15,21V19H17V21H15M11,21V19H13V21H11M7,21V19H9V21H7M4,21A1,1 0 0,1 3,20V19H5V21H4M3,15H5V17H3V15M21,15V17H19V15H21M3,11H5V13H3V11M21,11V13H19V11H21M3,7H5V9H3V7M21,7V9H19V7H21Z\"/>" +
		svgIconFooter;
	
	public static string iconShapes =
		svgIconHeader +
		"<path d = \"M11,13.5V21.5H3V13.5H11M12,2L17.5,11H6.5L12,2M17.5,13C20,13 22,15 22,17.5C22,20 20,22 17.5,22C15,22 13,20 13,17.5C13,15 15,13 17.5,13Z\"/>" +
		svgIconFooter;

	// Simple shapes
	public static string iconPoint =
		svgIconHeader +
		"<path d = \"M12,10A2,2 0 0,0 10,12C10,13.11 10.9,14 12,14C13.11,14 14,13.11 14,12A2,2 0 0,0 12,10Z\"/>" +
		svgIconFooter;
	
	public static string iconCircle =
		svgIconHeader +
		"<path d = \"M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z\"/>" +
		svgIconFooter;
	
	public static string iconTriangle =
		svgIconHeader +
		"<path d = \"M12,2L1,21H23M12,6L19.53,19H4.47\"/>" +
		svgIconFooter;

	public static string iconSquare =
		svgIconHeader +
		"<path d = \"M3,3H21V21H3V3M5,5V19H19V5H5Z\"/>" +
		svgIconFooter;
	
	public static string iconPentagon =
		svgIconHeader +
		"<path d = \"M12,5L19.6,10.5L16.7,19.4H7.3L4.4,10.5L12,5M12,2.5L2,9.8L5.8,21.5H18.1L22,9.8L12,2.5Z\"/>" +
		svgIconFooter;

	public static string iconHexagon =
		svgIconHeader +
		"<path d = \"M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L5,8.09V15.91L12,19.85L19,15.91V8.09L12,4.15Z\"/>" +
		svgIconFooter;

	public static string iconOctagon =
		svgIconHeader +
		"<path d = \"M8.27,3L3,8.27V15.73L8.27,21H15.73C17.5,19.24 21,15.73 21,15.73V8.27L15.73,3M9.1,5H14.9L19,9.1V14.9L14.9,19H9.1L5,14.9V9.1\"/>" +
		svgIconFooter;

	public static string iconPolygon =
		svgIconHeader +
		"<path d = \"M6.5,17H15L18.5,12L15,7H6.5L10,12L6.5,17M15,19H3L7.5,12L3,5H15C15.69,5 16.23,5.3 16.64,5.86L21,12L16.64,18.14C16.23,18.7 15.69,19 15,19Z\"/>" +
		svgIconFooter;
}
