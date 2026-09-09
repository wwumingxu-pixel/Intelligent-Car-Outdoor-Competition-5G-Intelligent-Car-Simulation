using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public class CarCameraControlPanel : MonoBehaviour
{
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private KeyCode toggleKey = KeyCode.H;

    private GameObject panelRoot;
    private GameObject shortcutPanel;
    private Text fovValue;
    private Text pitchValue;
    private Text heightValue;
    private Transform carRoot;
    private Transform cameraSupportPole;
    private float cameraYaw;
    private float cameraRoll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePanelExists()
    {
        if (FindObjectOfType<CarCameraControlPanel>() != null)
        {
            return;
        }

        new GameObject(
            "CameraControlUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CarCameraControlPanel)
        );
    }

    private void Awake()
    {
        ResolveCamera();
        ConfigureCanvas();
        EnsureEventSystem();
        BuildPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && panelRoot != null)
        {
            bool show = !panelRoot.activeSelf;
            panelRoot.SetActive(show);
            if (shortcutPanel != null)
            {
                shortcutPanel.SetActive(show);
            }
        }
    }

    private void ResolveCamera()
    {
        if (controlledCamera == null)
        {
            GameObject car = GameObject.Find("CarModel");
            carRoot = car != null ? car.transform : null;
            Transform cameraTransform = car != null ? car.transform.Find("CarPole/CarCamera") : null;
            if (cameraTransform == null && car != null)
            {
                cameraTransform = car.transform.Find("摄像头");
            }

            if (cameraTransform == null && car != null)
            {
                Camera childCamera = car.GetComponentInChildren<Camera>(true);
                cameraTransform = childCamera != null ? childCamera.transform : null;
            }

            controlledCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
        }

        if (controlledCamera != null && carRoot == null)
        {
            carRoot = controlledCamera.transform.root;
        }

        cameraSupportPole = carRoot != null ? carRoot.Find("碳杆") : null;

        if (controlledCamera != null)
        {
            Vector3 euler = controlledCamera.transform.localEulerAngles;
            cameraYaw = euler.y;
            cameraRoll = euler.z;
        }
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private void BuildPanel()
    {
        if (panelRoot != null)
        {
            return;
        }

        panelRoot = CreateUiObject("CameraPanel", transform);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -14f);
        panelRect.sizeDelta = new Vector2(270f, 158f);

        Image background = panelRoot.AddComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.075f, 0.9f);
        Outline outline = panelRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.32f, 0.38f, 0.43f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        CreateText("Title", panelRoot.transform, "车载摄像头", 16, TextAnchor.MiddleLeft,
            new Vector2(12f, -7f), new Vector2(246f, 24f), FontStyle.Bold);

        if (controlledCamera == null)
        {
            CreateText("MissingCamera", panelRoot.transform, "未找到车载摄像头", 13, TextAnchor.MiddleLeft,
                new Vector2(12f, -45f), new Vector2(246f, 24f), FontStyle.Normal);
            BuildShortcutPanel();
            return;
        }

        float pitch = NormalizeAngle(controlledCamera.transform.localEulerAngles.x);
        float height = GetCameraWorldHeight();

        CreateSliderRow("Fov", "视野", 36f, 30f, 100f, controlledCamera.fieldOfView, OnFovChanged, out fovValue);
        CreateSliderRow("Pitch", "角度", 74f, -30f, 45f, pitch, OnPitchChanged, out pitchValue);
        CreateSliderRow("Height", "高度", 112f, 0.15f, 0.8f, height, OnHeightChanged, out heightValue);

        OnFovChanged(controlledCamera.fieldOfView);
        OnPitchChanged(pitch);
        OnHeightChanged(height);
        BuildShortcutPanel();
    }

    private void BuildShortcutPanel()
    {
        shortcutPanel = CreateUiObject("ShortcutPanel", transform);
        RectTransform rect = shortcutPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(14f, -180f);
        rect.sizeDelta = new Vector2(270f, 164f);

        Image background = shortcutPanel.AddComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.075f, 0.9f);
        Outline outline = shortcutPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.32f, 0.38f, 0.43f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        CreateText("ShortcutTitle", shortcutPanel.transform, "按键控制", 16, TextAnchor.MiddleLeft,
            new Vector2(12f, -7f), new Vector2(246f, 24f), FontStyle.Bold);

        Text shortcuts = CreateText(
            "ShortcutText",
            shortcutPanel.transform,
            "W / S    前进 / 后退\nA / D    左转 / 右转\nSpace    停车\nR        复位车辆\nNum +/-  调整速度\nH        隐藏面板",
            13,
            TextAnchor.UpperLeft,
            new Vector2(12f, -36f),
            new Vector2(246f, 120f),
            FontStyle.Normal
        );
        shortcuts.lineSpacing = 1.05f;
    }

    private void CreateSliderRow(
        string rowName,
        string label,
        float top,
        float min,
        float max,
        float value,
        UnityEngine.Events.UnityAction<float> callback,
        out Text valueText)
    {
        CreateText(rowName + "Label", panelRoot.transform, label, 14, TextAnchor.MiddleLeft,
            new Vector2(12f, -top), new Vector2(40f, 24f), FontStyle.Normal);

        GameObject sliderObject = CreateUiObject(rowName + "Slider", panelRoot.transform);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(52f, -top - 3f);
        sliderRect.sizeDelta = new Vector2(150f, 20f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;

        GameObject backgroundObject = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 6f);
        Image sliderBackground = backgroundObject.AddComponent<Image>();
        sliderBackground.color = new Color(0.2f, 0.23f, 0.26f, 1f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(5f, -3f);
        fillAreaRect.offsetMax = new Vector2(-5f, 3f);

        GameObject fillObject = CreateUiObject("Fill", fillArea.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.13f, 0.62f, 0.78f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(7f, 0f);
        handleAreaRect.offsetMax = new Vector2(-7f, 0f);

        GameObject handleObject = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 18f);
        Image handle = handleObject.AddComponent<Image>();
        handle.color = new Color(0.9f, 0.94f, 0.96f, 1f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.value = Mathf.Clamp(value, min, max);
        slider.onValueChanged.AddListener(callback);

        valueText = CreateText(rowName + "Value", panelRoot.transform, string.Empty, 12, TextAnchor.MiddleRight,
            new Vector2(207f, -top), new Vector2(51f, 24f), FontStyle.Normal);
    }

    private void OnFovChanged(float value)
    {
        if (controlledCamera != null)
        {
            controlledCamera.fieldOfView = value;
        }

        if (fovValue != null)
        {
            fovValue.text = value.ToString("F0") + "°";
        }
    }

    private void OnPitchChanged(float value)
    {
        if (controlledCamera != null)
        {
            controlledCamera.transform.localRotation = Quaternion.Euler(value, cameraYaw, cameraRoll);
        }

        if (pitchValue != null)
        {
            pitchValue.text = value.ToString("F0") + "°";
        }
    }

    private void OnHeightChanged(float value)
    {
        if (controlledCamera != null)
        {
            Vector3 position = controlledCamera.transform.localPosition;
            float parentScale = Mathf.Abs(controlledCamera.transform.parent.lossyScale.y);
            position.y = parentScale > 0.0001f ? value / parentScale : value;
            controlledCamera.transform.localPosition = position;
            UpdateCameraSupport(position.y);
        }

        if (heightValue != null)
        {
            heightValue.text = value.ToString("F2") + " m";
        }
    }

    private float GetCameraWorldHeight()
    {
        if (controlledCamera == null || carRoot == null)
        {
            return controlledCamera != null ? controlledCamera.transform.localPosition.y : 0.2f;
        }

        return Mathf.Abs(controlledCamera.transform.position.y - carRoot.position.y);
    }

    private void UpdateCameraSupport(float cameraLocalHeight)
    {
        if (cameraSupportPole == null)
        {
            return;
        }

        const float bottomHeight = 0.01f;
        const float topMargin = 0.03f;
        float topHeight = Mathf.Max(cameraLocalHeight + topMargin, bottomHeight + 0.02f);
        float halfHeight = (topHeight - bottomHeight) * 0.5f;

        Vector3 scale = cameraSupportPole.localScale;
        scale.y = halfHeight;
        cameraSupportPole.localScale = scale;

        Vector3 position = cameraSupportPole.localPosition;
        position.y = bottomHeight + halfHeight;
        cameraSupportPole.localPosition = position;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        string content,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.9f, 0.93f, 0.95f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
