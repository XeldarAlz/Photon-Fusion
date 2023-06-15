using UnityEngine;

// This class is responsible for calculating and providing access to the screen borders in the world coordinates.
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

    // This method calculates and sets the screen borders in world coordinates.
    private void GetScreenBorders()
    {
        var topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        TopRight = topRight;
        TopLeft = new Vector3(-topRight.x, topRight.y, topRight.z);

        var bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
        BottomLeft = bottomLeft;
        BottomRight = new Vector3(topRight.x, bottomLeft.y, topRight.z);
    }
}