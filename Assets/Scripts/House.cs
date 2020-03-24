using System.Collections;
using UnityEngine;

[System.Serializable]
public class House
{
    public HouseShape shape;
    public HouseColor color;
    public Vector3 position;

    public House(HouseShape newShape, HouseColor newColor, Vector3 newPosition)
    {
        shape = newShape;
        color = newColor;
        position = newPosition;
    }
}

[System.Serializable]
public class HouseShape
{
    public Mesh shape;
    public float offset; //vert distance between center of house and ground

    public HouseShape(Mesh newShape, float newOffset)
    {
        shape = newShape;
        offset = newOffset;
    }
}

//created to easily match a color with its name
[System.Serializable]
public class HouseColor
{
    public Color color;
    public string colorName;

    public HouseColor(Color newColor, string newColorName)
    {
        color = newColor;
        colorName = newColorName;
    }
}