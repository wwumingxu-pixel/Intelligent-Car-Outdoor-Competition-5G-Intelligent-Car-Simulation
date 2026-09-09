using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CarVisionPreview : MonoBehaviour
{
    [SerializeField] private int processingWidth = 160;
    [SerializeField] private int processingHeight = 120;
    [SerializeField, Range(1f, 30f)] private float processingFps = 15f;
    [SerializeField] private float panelGap = 0.015f;

    private Camera sourceCamera;
    private UserVisionAlgorithm algorithm;
    private RenderTexture scaledFrame;
    private Texture2D inputTexture;
    private Texture2D outputTexture;
    private Color32[] outputPixels;
    private RawImage processedImage;
    private RectTransform actualLabel;
    private RectTransform processedPanel;
    private float nextProcessingTime;

    private void Awake()
    {
        sourceCamera = GetComponent<Camera>();
        sourceCamera.targetTexture = null;
        algorithm = GetComponent<UserVisionAlgorithm>();
        if (algorithm == null)
        {
            algorithm = gameObject.AddComponent<UserVisionAlgorithm>();
        }

        CreateProcessingBuffers();
        BuildPreviewUi();
    }

    private void LateUpdate()
    {
        LayoutPreviewUi();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);

        if (Time.unscaledTime < nextProcessingTime || processedImage == null)
        {
            return;
        }

        nextProcessingTime = Time.unscaledTime + 1f / processingFps;
        Graphics.Blit(source, scaledFrame);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = scaledFrame;
        inputTexture.ReadPixels(
            new Rect(0f, 0f, processingWidth, processingHeight),
            0,
            0,
            false
        );
        inputTexture.Apply(false, false);
        RenderTexture.active = previous;

        Color32[] inputPixels = inputTexture.GetPixels32();
        algorithm.Process(inputPixels, outputPixels, processingWidth, processingHeight);
        outputTexture.SetPixels32(outputPixels);
        outputTexture.Apply(false, false);
    }

    private void CreateProcessingBuffers()
    {
        scaledFrame = new RenderTexture(processingWidth, processingHeight, 0)
        {
            name = "VisionInput160x120",
            filterMode = FilterMode.Bilinear
        };
        scaledFrame.Create();

        inputTexture = new Texture2D(
            processingWidth,
            processingHeight,
            TextureFormat.RGB24,
            false
        );
        outputTexture = new Texture2D(
            processingWidth,
            processingHeight,
            TextureFormat.RGB24,
            false
        )
        {
            filterMode = FilterMode.Point
        };
        outputPixels = new Color32[processingWidth * processingHeight];
    }

    private void BuildPreviewUi()
    {
        GameObject canvasObject = new GameObject(
            "VisionPreviewUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        actualLabel = CreateLabel(canvasObject.transform, "ActualLabel", "实际画面");

        GameObject panelObject = new GameObject(
            "ProcessedVisionPanel",
            typeof(RectTransform),
            typeof(Image)
        );
        panelObject.transform.SetParent(canvasObject.transform, false);
        processedPanel = panelObject.GetComponent<RectTransform>();
        panelObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 1f);

        GameObject imageObject = new GameObject(
            "ProcessedImage",
            typeof(RectTransform),
            typeof(RawImage)
        );
        imageObject.transform.SetParent(panelObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        processedImage = imageObject.GetComponent<RawImage>();
        processedImage.texture = outputTexture;
        processedImage.raycastTarget = false;

        CreateLabel(panelObject.transform, "ProcessedLabel", "处理结果");
        LayoutPreviewUi();
    }

    private void LayoutPreviewUi()
    {
        if (sourceCamera == null || actualLabel == null || processedPanel == null)
        {
            return;
        }

        Rect cameraRect = sourceCamera.rect;
        float processedTop = cameraRect.yMin - panelGap;
        float processedBottom = Mathf.Max(0f, processedTop - cameraRect.height);

        processedPanel.anchorMin = new Vector2(cameraRect.xMin, processedBottom);
        processedPanel.anchorMax = new Vector2(cameraRect.xMax, processedTop);
        processedPanel.offsetMin = Vector2.zero;
        processedPanel.offsetMax = Vector2.zero;

        actualLabel.anchorMin = new Vector2(cameraRect.xMin, cameraRect.yMax);
        actualLabel.anchorMax = new Vector2(cameraRect.xMin, cameraRect.yMax);
        actualLabel.pivot = new Vector2(0f, 1f);
        actualLabel.anchoredPosition = new Vector2(4f, -4f);
    }

    private static RectTransform CreateLabel(Transform parent, string name, string content)
    {
        GameObject labelObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(72f, 24f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        labelObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 0.82f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(labelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 0f);
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 13;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        return rect;
    }

    private void OnDestroy()
    {
        if (scaledFrame != null)
        {
            scaledFrame.Release();
            Destroy(scaledFrame);
        }

        Destroy(inputTexture);
        Destroy(outputTexture);
    }
}
