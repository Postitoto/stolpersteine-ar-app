using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PreProcessor
{
    private int _targetSideLength;
    private Texture2D _cropped;
    private Texture2D _scaled;


    public PreProcessor(int targetWidth, int targetHeight)
    {
        Debug.Assert(targetWidth == targetHeight, "only support square target images at the moment");
        _targetSideLength = targetWidth;
        _scaled = new Texture2D(targetWidth, targetHeight);
    }


    public Color32[] Preprocess(Texture2D texture)
    {
        var t_0 = DateTime.Now;
        var cropped = CropCenterSquare(texture);
        var scaled = Scaled(cropped, _targetSideLength, _targetSideLength, FilterMode.Bilinear);
        Debug.Log("Preprocessing took: " + (DateTime.Now - t_0).TotalSeconds + "s");
        return scaled.GetPixels32();
    }


    /// <summary>
    ///     Crops out max center square of texture
    /// </summary>
    /// <param name="texture">texture to be cropped</param>
    public Texture2D CropCenterSquare(Texture2D texture)
    {
        var cropWidth = texture.width > texture.height ? true : false;
        var squareLen = cropWidth ? texture.height : texture.width;
        int squareX = cropWidth ? (texture.width - squareLen) / 2 : 0;
        int squareY = !cropWidth ? (texture.height - squareLen) / 2 : 0;

        if (_cropped == null || _cropped.width != squareLen || _cropped.height != squareLen)
        {
            //if the width/height of _cropped is changed somewhere, this might lead to a memory leak
            _cropped = new Texture2D(squareLen, squareLen);
        }

        _cropped.SetPixels(texture.GetPixels(Mathf.FloorToInt(squareX), Mathf.FloorToInt(squareY),
                                            Mathf.FloorToInt(squareLen), Mathf.FloorToInt(squareLen)));
        Debug.Log(string.Format("cropping at X: {0}, Y: {1}, W: {2}, H: {2}", squareX, squareY, squareLen));
        _cropped.Apply();
        return _cropped;
    }


    // Methods from  https://github.com/derenlei/Unity_Detection2AR/blob/main/Assets/Scripts/TextureTools.cs:

    /// <summary>
    /// Returns the scaled texture.
    /// </summary>
    /// <param name="tex">Texure to scale</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="mode">Filtering mode</param>
    public Texture2D Scaled(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        GPUScale(tex, width, height, mode);

        //Get rendered data back to a new texture
        //Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        _scaled.Resize(width, height);
        _scaled.ReadPixels(texR, 0, 0, true);
        return _scaled;
    }

    // Internal unility that renders the source texture into the RTT - the scaling method itself.
    private static void GPUScale(Texture2D src, int width, int height, FilterMode fmode)
    {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply(true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture(width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }

}