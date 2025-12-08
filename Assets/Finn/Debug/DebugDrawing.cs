using UnityEngine;

public static class DebugDrawing
{
    /// <summary>
    /// Draws a 2D rectangle in world space using Debug.DrawLine.
    /// The rectangle is defined by its minimum (bottom-left) and maximum (top-right) corners.
    /// Assumes the Z-coordinate is the same for all points (2D in XY plane).
    /// </summary>
    /// <param name="min">The minimum corner (e.g., bottom-left) in world space.</param>
    /// <param name="max">The maximum corner (e.g., top-right) in world space.</param>
    /// <param name="color">The color of the rectangle lines.</param>
    /// <param name="duration">How long the lines should be visible for (0 for one frame).</param>
    /// <param name="depthTest">Should the line be obscured by objects closer to the camera?</param>
    public static void DrawRect(Vector3 min, Vector3 max, Color color, float duration = 0.0f, bool depthTest = true)
    {
        // Define the other two corners
        Vector3 topRight = new Vector3(max.x, max.y, min.z); // Max X, Max Y
        Vector3 bottomLeft = new Vector3(min.x, min.y, min.z); // Min X, Min Y
        Vector3 topLeft = new Vector3(min.x, max.y, min.z); // Min X, Max Y
        Vector3 bottomRight = new Vector3(max.x, min.y, min.z); // Max X, Min Y

        // Line 1: Bottom
        Debug.DrawLine(bottomLeft, bottomRight, color, duration, depthTest);
        // Line 2: Right
        Debug.DrawLine(bottomRight, topRight, color, duration, depthTest);
        // Line 3: Top
        Debug.DrawLine(topRight, topLeft, color, duration, depthTest);
        // Line 4: Left
        Debug.DrawLine(topLeft, bottomLeft, color, duration, depthTest);
    }
}