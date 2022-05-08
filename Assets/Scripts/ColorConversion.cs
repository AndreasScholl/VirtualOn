using UnityEngine;

public class ColorConversion
{
    static public Color ConvertColor(int color)
    {
        float b = ((color >> 10) & 0x1f) / (float)0x1f;
        float g = ((color >> 5) & 0x1f) / (float)0x1f;
        float r = (color & 0x1f) / (float)0x1f;
        Color rgbColor = new Color(r, g, b);
        return rgbColor;
    }
}
