using UnityEngine;

// The ScreenBorders class provides utilities for working with the edges of the screen in a 2D game environment. 
// The screen borders' positions are stored in static properties that are available globally, providing easy access to these values from any part of the codebase.
public class ScreenBorders : MonoBehaviour
{
    // Static properties holding the world coordinates of each corner of the screen.
    public static Vector3 TopLeft { private set; get; }
    public static Vector3 TopRight { private set; get; }
    public static Vector3 BottomLeft { private set; get; }
    public static Vector3 BottomRight { private set; get; }

    private void Awake()
    {
        GetScreenBorders();
    }

    // This method calculates the screen borders based on the main camera's perspective, translating the screen's edge coordinates from screen space to world space. 
    // It assigns these values to the respective properties of the class. 
    private void GetScreenBorders()
    {
        // Get top right screen corner's world coordinates.
        var topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        
        // Assign the calculated world coordinates to the respective static properties.
        TopRight = topRight;
        TopLeft = new Vector3(-topRight.x, topRight.y, topRight.z);

        // Get bottom left screen corner's world coordinates.
        var bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);

        // Assign the calculated world coordinates to the respective static properties.
        BottomLeft = bottomLeft;
        BottomRight = new Vector3(topRight.x, bottomLeft.y, topRight.z);
    }
}