using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCameraInputDetection : MonoBehaviour
{
    [SerializeField] private ARCameraManager _ARCameraManager;
    [SerializeField] Yolov4_tiny _detector; // Can not use Detector interface, since it is not assignable in inspector
    [SerializeField] private bool _drawBoxes;
    private PreProcessor _preprocessor;
    private DateTime _lastDetection;
    private IList<BoundingBox> _detections;


    void OnEnable()
    {
        _preprocessor = new PreProcessor(_detector.IMG_WIDTH, _detector.IMG_HEIGHT);
    }

    public IList<BoundingBox> DetectOnLatestFrame()
    {
        var t_0 = DateTime.Now;

        Texture2D camTexture = AcquireLatestCpuImageAsTexture();
        if (camTexture == null)
        {
            Debug.Log("Getting ar cam image failed");
            return null;
        }

        TimeMeasurements.StartingMeasurement("Machine Learning Part");
        Color32[] imgPreprocessed = _preprocessor.Preprocess(camTexture);

        StartCoroutine(_detector.Detect(imgPreprocessed, boxes =>
    {
        foreach (BoundingBox box in boxes)
        {
            var bbDimensionsScreenSpace = MapToScreen(box.Dimensions, camTexture.width, camTexture.height, _detector.IMG_WIDTH, _detector.IMG_HEIGHT);
            box.Dimensions = bbDimensionsScreenSpace;
        }
        _detections = boxes;
        TimeMeasurements.StoppingMeasurement();
    }));
        Debug.Log("whole detection took " + (DateTime.Now - t_0).TotalSeconds + "s");
        return _detections;
    }

    private Texture2D _texture;
    /// <summary>
    ///     Gets the latest image from the ar camera and returns it as Texture2D
    /// </summary>
    /// <returns> The latest ar cam image as texture</returns>
    unsafe private Texture2D AcquireLatestCpuImageAsTexture()
    {
        XRCpuImage image;
        if (!_ARCameraManager.TryAcquireLatestCpuImage(out image))
        {
            return null;
        }

        var format = TextureFormat.RGBA32;
        if (_texture == null || _texture.width != image.width || _texture.height != image.height)
        {
            _texture = new Texture2D(image.width, image.height, format, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.None);

        var rawTextureData = _texture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // Dispose of the XRCameraImage  to avoid leaking native resources.
            image.Dispose();
        }

        // Account for possibly rotated device
        var rotatedTexture = FitDeviceOrientation(_texture);
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    private Texture2D _rotatedTexture;
    /// <summary>
    ///     Rotates the texture, such that it matches the current device orientation
    /// </summary>
    /// <param name="texture">The texture to rotate</param>
    /// <param name="width">Width of the corresponding image</param>
    /// <param name="height">Height of the corresponding image</param>
    /// <returns> The rotated texture</returns>
    private Texture2D FitDeviceOrientation(Texture2D texture)
    {
        var screenOrientation = Screen.orientation;

        // For landscape left, the image is not rotated
        if (screenOrientation == ScreenOrientation.LandscapeLeft)
        {
            return texture;
        }

        Color32[] rotatedMatrix;
        Color32[] matrix = texture.GetPixels32();
        switch (screenOrientation)
        {
            case ScreenOrientation.PortraitUpsideDown:
                rotatedMatrix = RotateLeft(matrix, texture.width, texture.height);
                if (_rotatedTexture == null || _rotatedTexture.width != texture.height || _rotatedTexture.height != texture.width)
                {
                    Destroy(_rotatedTexture);
                    _rotatedTexture = new Texture2D(texture.height, texture.width);
                }
                break;
            case ScreenOrientation.LandscapeRight:
                rotatedMatrix = Rotate180(matrix, texture.width, texture.height);
                if (_rotatedTexture == null || _rotatedTexture.width != texture.width || _rotatedTexture.height != texture.height)
                {
                    Destroy(_rotatedTexture);
                    _rotatedTexture = new Texture2D(texture.width, texture.height);
                }
                break;
            case ScreenOrientation.Portrait:
                rotatedMatrix = RotateRight(matrix, texture.width, texture.height);
                if (_rotatedTexture == null || _rotatedTexture.width != texture.height || _rotatedTexture.height != texture.width)
                {
                    Destroy(_rotatedTexture);
                    _rotatedTexture = new Texture2D(texture.height, texture.width);
                }
                break;
            default:
                throw new Exception("DeviceOrientation " + screenOrientation + " not supported");
        }
        _rotatedTexture.SetPixels32(rotatedMatrix);
        return _rotatedTexture;
    }

    private static Color32[] RotateRight(Color32[] matrix, int width, int height)
    {
        Color32[] result = new Color32[matrix.Length];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                result[i * height + j] = matrix[(height - 1 - j) * width + i];
            }
        }
        return result;
    }
    private static Color32[] Rotate180(Color32[] matrix, int width, int height)
    {
        Color32[] result = new Color32[matrix.Length];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                result[j * width + i] = matrix[(height - 1 - j) * width + (width - 1 - i)];
            }
        }
        return result;
    }
    private static Color32[] RotateLeft(Color32[] matrix, int width, int height)
    {
        Color32[] result = new Color32[matrix.Length];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                result[i * height + j] = matrix[j * width + (width - 1 - i)];
            }
        }
        return result;
    }


    /// <summary>
    ///     Translates the bounding box coordinates from preprocessed image input space into screen space
    /// </summary>
    /// <param name="dims">The bounding box dimensions to translage</param>
    /// <param name="camWidth">Width of the camera image</param>
    /// <param name="camHeight">Height of the camera image</param>
    /// <param name="processedWidth">Width of the preprocessed image that is input for the NN</param>
    /// <param name="processedHeight">Height of the preprocessed image that is input for the NN</param>
    /// <returns> The rotated texture</returns>
    public BoundingBoxDimensions MapToScreen(BoundingBoxDimensions dims,
        float camWidth, float camHeight, float processedWidth, float processedHeight)
    {
        //factors for mapping the bounding box from the processsed image space to  ARCamera Image space
        float scale2cam = Mathf.Min(camWidth / processedWidth, camHeight / processedHeight);
        float shift2camX = (camWidth - processedWidth * scale2cam) / 2;
        float shift2camY = (camHeight - processedHeight * scale2cam) / 2;

        //factors for mapping the bounding box from ARCamera Image space to Screen Space
        float scale2screen = Mathf.Max(Screen.width / camWidth, Screen.height / camHeight);
        float shift2screenX = (Screen.width - camWidth * scale2screen) / 2;
        float shift2screenY = (Screen.height - camHeight * scale2screen) / 2;

        //perform mapping from processsed image space to Screen space
        var widthScreen = dims.Width * scale2cam * scale2screen;
        var heightScreen = dims.Height * scale2cam * scale2screen;
        var xCam = dims.X * scale2cam + shift2camX;
        var xScreen = xCam * scale2screen + shift2screenX;
        var yCam = dims.Y * scale2cam + shift2camY;
        var yScreen = yCam * scale2screen + shift2screenY;

        return new BoundingBoxDimensions
        {
            X = xScreen,
            Y = yScreen,
            Width = widthScreen,
            Height = heightScreen
        };
    }

    public void OnGUI()
    {
        if (!_drawBoxes)
        {
            return;
        }

        if (_detections == null)
        {
            return;
        }

        foreach (BoundingBox bbox in _detections)
        {
            var width = bbox.Dimensions.Width;
            var height = bbox.Dimensions.Height;
            var xRect = bbox.Dimensions.X - width / 2;
            var yRect = bbox.Dimensions.Y - height / 2;

            LabelGUI(xRect, yRect, bbox);
            BBoxGUI(xRect, yRect, width, height, 10);
        }
    }

    private Texture2D _rectTexture;
    private GUIStyle _rectStyle;
    public void BBoxGUI(float x, float y, float width, float height, int thickness)
    {
        if (_rectTexture == null || _rectStyle == null)
        {
            _rectTexture = new Texture2D(1, 1);
            _rectStyle = new GUIStyle();
            _rectTexture.SetPixel(0, 0, new Color(1f, 0, 0f, 0.5f));
            _rectTexture.Apply();
            _rectStyle.normal.background = _rectTexture;
        }

        GUI.Box(new Rect(x, y, width + thickness, -thickness), GUIContent.none, _rectStyle);
        GUI.Box(new Rect(x, y - thickness, -thickness, height + thickness), GUIContent.none, _rectStyle);
        GUI.Box(new Rect(x - thickness, y + height, width + thickness, thickness), GUIContent.none, _rectStyle);
        GUI.Box(new Rect(x + width, y, thickness, height + thickness), GUIContent.none, _rectStyle);
    }

    private GUIStyle _labelStyle;
    public void LabelGUI(float x, float y, BoundingBox bbox)
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle();
            _labelStyle.fontSize = 20;
            _labelStyle.normal.textColor = new Color(1f, 0f, 0f, 0.8f);
        }

        GUI.Label(new Rect(x, y - 30, 200, 20), $"{bbox.Label}: {(int)(bbox.Confidence * 100)}%", _labelStyle);
    }
}