using System;
using UnityEngine;
using Unity.Barracuda;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


// Executes backbone inference using Barracuda and implements the YOLO head
// code for YOLO head is based and adapted from 
// https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx
public class Yolov4_tiny : MonoBehaviour, Detector
{
    // Fixed parameters of YOLOv4-tiny
    public int IMG_WIDTH { get; } = 416;
    public int IMG_HEIGHT { get; } = 416;
    public readonly static Dictionary<string, int> PARAMS_L = new Dictionary<string, int>() { { "ROWS", 13 }, { "COLS", 13 }, { "STRIDE_X", 32 }, { "STRIDE_Y", 32 } };
    private static Dictionary<string, int> PARAMS_M = new Dictionary<string, int>() { { "ROWS", 26 }, { "COLS", 26 }, { "STRIDE_X", 16 }, { "STRIDE_Y", 16 } };
    private static int BOX_INFO_FEATURE_COUNT = 5;

    // Parameters of YOLOv4-tiny, that depend on the model
    private int BOXES_PER_CELL = 3;
    [SerializeField] private int CLASS_COUNT = 1;
    private float[] anchors = new float[]
    {
        // Anchors in the config used for training on Stolpersteine
        10f,14f,  23f,27f,  37f,58f,  81f,82f,  135f,169f,  344f,319f
    };

    [SerializeField] private NNModel _onnxModel;
    [SerializeField] private string _inputLayer;
    [SerializeField] private string _outputLayerL;
    [SerializeField] private string _outputLayerM;
    [SerializeField] private float _thresholdScore;
    [SerializeField] private float _thresholdIOU;
    [SerializeField] private int _bboxLimit;
    [SerializeField] private int _syncEveryNthLayer;
    [SerializeField] private TextAsset labelsFile = null;

    private IWorker worker;
    private string[] labels;
    private bool drawBoxes;

    private void OnEnable()
    {
        var model = ModelLoader.Load(_onnxModel);
        worker = GraphicsWorker.GetWorker(model);

        if (labelsFile == null && CLASS_COUNT == 1)
        {
            labels = new string[] { "object" };
        }
        else
        {
            labels = Regex.Split(labelsFile.text, "\n|\r|\r\n")
                .Where(s => !String.IsNullOrEmpty(s)).ToArray();
        }
    }

    void OnDisable()
    {
        worker?.Dispose();
    }

    /// <summary>
    ///     Performs object detection on the image
    /// </summary>
    /// <param name="img">input image</param>
    /// <param name="callback">Callback function to process results</param>
    public IEnumerator Detect(Color32[] img, System.Action<IList<BoundingBox>> callback)
    {
        var t_0 = DateTime.Now;
        using (var tensor = ImgToTensor(img, IMG_WIDTH, IMG_HEIGHT))
        {
            // Backbone execution
            var executor = worker.StartManualSchedule(tensor);
            var it = 0;
            bool hasMoreWork;
            do
            {
                hasMoreWork = executor.MoveNext();
                if (++it % _syncEveryNthLayer == 0)
                    worker.FlushSchedule();
            } while (hasMoreWork);

            var t_1 = DateTime.Now;
            Debug.Log("Model execution took: " + (t_1 - t_0).TotalSeconds + "s");

            // Output handling (YOLO Head) mixed non-maximum-supression
            var outputL = worker.PeekOutput(_outputLayerL);
            var outputM = worker.PeekOutput(_outputLayerM);
            var boxesL = DecodeWithThreshold(outputL, _thresholdScore, PARAMS_L);
            var boxesM = DecodeWithThreshold(outputM, _thresholdScore, PARAMS_M);
            var boxesCat = boxesL.Concat(boxesM).ToList();
            var boxes = FilterBoundingBoxes(boxesCat, _bboxLimit, _thresholdIOU);
            Debug.Log("Output processing took: " + (DateTime.Now - t_1).TotalSeconds + "s");
            Debug.Log("Detected " + boxes.Count + " objects:\n" + string.Join("\n", boxes));

            callback(boxes);
        }
        yield return null;
    }

    /// <summary>
    ///     Converts an image to a Tensor
    /// </summary>
    /// <param name="img">image as Color32 array</param>
    /// <param name="width">image widht</param>
    /// <param name="height">image height</param>
    public static Tensor ImgToTensor(Color32[] img, int width, int height)
    {
        float[] floatValues = new float[width * height * 3];

        for (int i = 0; i < img.Length; ++i)
        {
            var color = img[i];

            floatValues[i * 3 + 0] = (float)color.r / 255;
            floatValues[i * 3 + 1] = (float)color.g / 255;
            floatValues[i * 3 + 2] = (float)color.b / 255;
        }
        return new Tensor(1, height, width, 3, floatValues);
    }


    // Model output handling adapted from https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx

    /// <summary>
    ///     Decode the YOLO feature map and map to bounding box coordinates
    ///     Already filters out predictions < threshold for efficiency reasons
    /// </summary>
    /// <param name="modelOutput">output tensor of YOLO model</param>
    /// <param name="thresholdScore">minimum prediction score</param>
    /// <param name="parameters">dictionary of parameters</param>
    private IList<BoundingBox> DecodeWithThreshold(Tensor modelOutput, float thresholdScore, Dictionary<string, int> parameters)
    {
        var boxes = new List<BoundingBox>();
        for (int cy = 0; cy < parameters["COLS"]; cy++)
        {
            for (int cx = 0; cx < parameters["ROWS"]; cx++)
            {
                for (int box = 0; box < BOXES_PER_CELL; box++)
                {
                    var channel = (box * (BOX_INFO_FEATURE_COUNT + CLASS_COUNT));

                    // Filter for confidence & confidence x probability early, to save resources
                    float confidence = GetConfidence(modelOutput, cx, cy, channel);
                    if (confidence < thresholdScore)
                    {
                        continue;
                    }
                    float[] predictedClasses = ExtractClasses(modelOutput, cx, cy, channel);
                    var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                    var topScore = topResultScore * confidence;
                    if (topScore < thresholdScore)
                    {
                        continue;
                    }

                    var bbd = ExtractBoundingBoxDimensions(modelOutput, cx, cy, channel);
                    // Get bounding boxes in image space, X,Y correspond to center coordinates
                    var mappedBoundingBox = MapBoundingBoxToCell(cx, cy, box, bbd, parameters);
                    boxes.Add(new BoundingBox
                    {
                        Dimensions = mappedBoundingBox,
                        Confidence = topScore,
                        Label = labels[topResultIndex],
                    });
                }
            }
        }
        return boxes;
    }


    private BoundingBoxDimensions ExtractBoundingBoxDimensions(Tensor modelOutput, int x, int y, int channel)
    {
        return new BoundingBoxDimensions
        {
            X = modelOutput[0, x, y, channel],
            Y = modelOutput[0, x, y, channel + 1],
            Width = modelOutput[0, x, y, channel + 2],
            Height = modelOutput[0, x, y, channel + 3]
        };
    }

    private float GetConfidence(Tensor modelOutput, int x, int y, int channel)
    {
        return Sigmoid(modelOutput[0, x, y, channel + 4]);
    }

    private float Sigmoid(float value)
    {
        var k = (float)Math.Exp(value);

        return k / (1.0f + k);
    }

    public float[] ExtractClasses(Tensor modelOutput, int x, int y, int channel)
    {
        float[] predictedClasses = new float[CLASS_COUNT];
        int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;

        for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
        {
            predictedClasses[predictedClass] = modelOutput[0, x, y, predictedClass + predictedClassOffset];
        }

        return Softmax(predictedClasses);
    }

    private ValueTuple<int, float> GetTopResult(float[] predictedClasses)
    {
        return predictedClasses
            .Select((predictedClass, index) => (Index: index, Value: predictedClass))
            .OrderByDescending(result => result.Value)
            .First();
    }

    private BoundingBoxDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions, Dictionary<string, int> parameters)
    {
        var anchor_offs = (parameters["STRIDE_X"] == 16) ? 2 * box : 2 * box + 6;

        return new BoundingBoxDimensions
        {
            X = ((float)y + Sigmoid(boxDimensions.X)) * parameters["STRIDE_X"],
            Y = ((float)x + Sigmoid(boxDimensions.Y)) * parameters["STRIDE_Y"],
            Width = (float)Math.Exp(boxDimensions.Width) * anchors[anchor_offs],
            Height = (float)Math.Exp(boxDimensions.Height) * anchors[anchor_offs + 1],
        };
    }

    private float[] Softmax(float[] values)
    {
        var maxVal = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxVal));
        var sumExp = exp.Sum();

        return exp.Select(v => (float)(v / sumExp)).ToArray();
    }

    /// <summary>
    ///     Non-maximum-suppression to filter out the overlapping bounding boxes
    /// </summary>
    /// <param name="boxes">bounding boxes to be filtered</param>
    /// <param name="limit">maximum number of accepted predictions</param>
    /// <param name="thresholdIOU">threshold value of IoU determining if overlapping boxes are to be removed</param>
    private IList<BoundingBox> FilterBoundingBoxes(IList<BoundingBox> boxes, int limit, float thresholdIOU)
    {
        var t_0 = DateTime.Now;
        var activeCount = boxes.Count;
        var isActiveBoxes = new bool[boxes.Count];

        for (int i = 0; i < isActiveBoxes.Length; i++)
        {
            isActiveBoxes[i] = true;
        }

        var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                .OrderByDescending(b => b.Box.Confidence)
                .ToList();

        var results = new List<BoundingBox>();

        for (int i = 0; i < boxes.Count; i++)
        {
            if (isActiveBoxes[i])
            {
                var boxA = sortedBoxes[i].Box;
                results.Add(boxA);

                if (results.Count >= limit)
                    break;

                for (var j = i + 1; j < boxes.Count; j++)
                {
                    if (isActiveBoxes[j])
                    {
                        var boxB = sortedBoxes[j].Box;

                        if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > thresholdIOU)
                        {
                            isActiveBoxes[j] = false;
                            activeCount--;

                            if (activeCount <= 0)
                                break;
                        }
                    }
                }

                if (activeCount <= 0)
                    break;
            }
        }

        return results;
    }

    private float IntersectionOverUnion(Rect boundingBoxA, Rect boundingBoxB)
    {
        var areaA = boundingBoxA.width * boundingBoxA.height;

        if (areaA <= 0)
            return 0;

        var areaB = boundingBoxB.width * boundingBoxB.height;

        if (areaB <= 0)
            return 0;

        var minX = Math.Max(boundingBoxA.xMin, boundingBoxB.xMin);
        var minY = Math.Max(boundingBoxA.yMin, boundingBoxB.yMin);
        var maxX = Math.Min(boundingBoxA.xMax, boundingBoxB.xMax);
        var maxY = Math.Min(boundingBoxA.yMax, boundingBoxB.yMax);

        var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

        return intersectionArea / (areaA + areaB - intersectionArea);
    }
}