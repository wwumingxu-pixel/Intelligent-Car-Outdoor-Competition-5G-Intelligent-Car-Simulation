using UnityEngine;

public class UserVisionAlgorithm : MonoBehaviour
{
    [SerializeField, Range(0, 255)] private int brightnessThreshold = 170;

    public void Process(
        Color32[] source,
        Color32[] destination,
        int width,
        int height)
    {
        for (int i = 0; i < source.Length; i++)
        {
            Color32 pixel = source[i];
            int luminance = (pixel.r * 77 + pixel.g * 150 + pixel.b * 29) >> 8;
            byte value = luminance >= brightnessThreshold ? (byte)255 : (byte)0;
            destination[i] = new Color32(value, value, value, 255);
        }
    }
}
