using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RaceCourseInteractionManager : MonoBehaviour
{
    [SerializeField] private string carObjectName = "CarModel";
    [SerializeField] private string startBarrierObjectName = "StartBarrier_80x50";
    [SerializeField] private string parkingBarrierObjectName = "ParkingBarrier_61x50";
    [SerializeField] private float barrierLiftHeight = 1.2f;
    [SerializeField] private float animationSpeed = 3f;
    [SerializeField] private float parkingSideOffset = 0.3f;

    private Transform car;
    private Transform startBarrier;
    private Transform parkingBarrier;
    private Vector3 carStartPosition;
    private Quaternion carStartRotation;
    private Vector3 startBarrierPosition;
    private Vector3 parkingBarrierPosition;
    private bool started;
    private bool parkingOnLeft = false;
    private float startBarrierTargetY;
    private ImportedFourWheelCarController carController;
    private Text startButtonLabel;
    private Text sideButtonLabel;
    private Coroutine barrierMoveRoutine;

    private void Awake()
    {
        ResolveReferences();
        NormalizeBarrierModel(startBarrier, "BlueBaffle");
        NormalizeBarrierModel(parkingBarrier, "BlueBaffle_61");
        ConfigureColliders();
        SaveInitialState();
        started = false;
        startBarrierTargetY = startBarrierPosition.y;
        SetParkingSide(false);
        BuildControlsUi();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCourse();
        }

        if (startBarrier != null)
        {
            if (barrierMoveRoutine == null && Mathf.Abs(startBarrier.position.y - startBarrierTargetY) > 0.001f)
            {
                barrierMoveRoutine = StartCoroutine(MoveStartBarrier());
            }
        }
    }

    public void StartCourse()
    {
        started = true;
        startBarrierTargetY = startBarrierPosition.y + barrierLiftHeight;
        RestartBarrierAnimation();
        if (startButtonLabel != null)
        {
            startButtonLabel.text = "挡板已打开";
        }
    }

    public void ResetCourse()
    {
        started = false;
        startBarrierTargetY = startBarrierPosition.y;
        if (barrierMoveRoutine != null)
        {
            StopCoroutine(barrierMoveRoutine);
            barrierMoveRoutine = null;
        }
        if (startButtonLabel != null)
        {
            startButtonLabel.text = "开始";
        }

        if (startBarrier != null)
        {
            startBarrier.position = startBarrierPosition;
        }

        SetParkingSide(false);

        if (carController != null)
        {
            carController.ResetCar();
        }
        else if (car != null)
        {
            Rigidbody body = car.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = carStartPosition;
                body.rotation = carStartRotation;
            }
            else
            {
                car.position = carStartPosition;
                car.rotation = carStartRotation;
            }
        }

        Physics.SyncTransforms();
    }

    private void RestartBarrierAnimation()
    {
        if (barrierMoveRoutine != null)
        {
            StopCoroutine(barrierMoveRoutine);
        }

        barrierMoveRoutine = startBarrier != null ? StartCoroutine(MoveStartBarrier()) : null;
    }

    private System.Collections.IEnumerator MoveStartBarrier()
    {
        while (startBarrier != null && Mathf.Abs(startBarrier.position.y - startBarrierTargetY) > 0.001f)
        {
            Vector3 position = startBarrier.position;
            position.y = Mathf.MoveTowards(position.y, startBarrierTargetY, animationSpeed * Time.deltaTime);
            startBarrier.position = position;
            yield return null;
        }

        if (startBarrier != null)
        {
            Vector3 position = startBarrier.position;
            position.y = startBarrierTargetY;
            startBarrier.position = position;
        }

        barrierMoveRoutine = null;
    }

    public void SetParkingSide(bool leftSide)
    {
        parkingOnLeft = leftSide;
        if (parkingBarrier == null)
        {
            return;
        }

        Vector3 position = parkingBarrierPosition;
        position.z = leftSide ? -parkingSideOffset : parkingSideOffset;
        parkingBarrier.position = position;
        if (sideButtonLabel != null)
        {
        sideButtonLabel.text = leftSide ? "停车挡板：左侧" : "停车挡板：右侧";
        }
    }

    private void ResolveReferences()
    {
        GameObject carObject = GameObject.Find(carObjectName);
        GameObject startObject = GameObject.Find(startBarrierObjectName);
        GameObject parkingObject = GameObject.Find(parkingBarrierObjectName);

        car = carObject != null ? carObject.transform : null;
        startBarrier = startObject != null ? startObject.transform : null;
        parkingBarrier = parkingObject != null ? parkingObject.transform : null;
        carController = carObject != null ? carObject.GetComponent<ImportedFourWheelCarController>() : null;
    }

    private void SaveInitialState()
    {
        if (car != null)
        {
            carStartPosition = car.position;
            carStartRotation = car.rotation;
        }

        if (startBarrier != null)
        {
            startBarrierPosition = startBarrier.position;
        }

        if (parkingBarrier != null)
        {
            parkingBarrierPosition = parkingBarrier.position;
        }
    }

    private void NormalizeBarrierModel(Transform barrier, string modelChildName)
    {
        if (barrier == null)
        {
            return;
        }

        Transform model = barrier.Find(modelChildName);
        if (model != null)
        {
            model.localPosition = new Vector3(0f, 0.25f, 0f);
        }

        DisableChildComponent<Camera>(barrier);
        DisableChildComponent<Light>(barrier);
    }

    private void ConfigureColliders()
    {
        ConfigureBarrierCollider(startBarrier, new Vector3(0.8f, 0.5f, 0.02f));
        ConfigureBarrierCollider(parkingBarrier, new Vector3(0.61f, 0.5f, 0.02f));

        if (car != null)
        {
            BoxCollider collider = car.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = car.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.11f, 0f);
                collider.size = new Vector3(0.34f, 0.23f, 0.21f);
            }
            collider.enabled = true;
        }
    }

    private static void ConfigureBarrierCollider(Transform barrier, Vector3 size)
    {
        if (barrier == null)
        {
            return;
        }

        BoxCollider collider = barrier.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = barrier.gameObject.AddComponent<BoxCollider>();
        }

        collider.center = new Vector3(0f, 0.25f, 0f);
        collider.size = size;
        collider.isTrigger = false;
        collider.enabled = true;
    }

    private static void DisableChildComponent<T>(Transform root) where T : Behaviour
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            component.enabled = false;
        }
    }

    private void BuildControlsUi()
    {
        GameObject canvasObject = new GameObject("RaceCourseControlsUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject panel = CreateUiObject("RaceCoursePanel", canvasObject.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-22f, 22f);
        panelRect.sizeDelta = new Vector2(220f, 122f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.04f, 0.05f, 0.9f);

        Text title = CreateUiText("Title", panel.transform, "赛道控制", 16, FontStyle.Bold);
        SetRect(title.gameObject, new Vector2(10f, -8f), new Vector2(200f, 24f), new Vector2(0f, 1f));

        Button startButton = CreateButton("StartButton", panel.transform, "开始");
        SetRect(startButton.gameObject, new Vector2(10f, -40f), new Vector2(200f, 32f), new Vector2(0f, 1f));
        startButtonLabel = startButton.GetComponentInChildren<Text>();
        startButton.onClick.AddListener(StartCourse);

        Button sideButton = CreateButton("SideButton", panel.transform, "停车挡板：右侧");
        SetRect(sideButton.gameObject, new Vector2(10f, -80f), new Vector2(200f, 32f), new Vector2(0f, 1f));
        sideButtonLabel = sideButton.GetComponentInChildren<Text>();
        sideButton.onClick.AddListener(() => SetParkingSide(!parkingOnLeft));
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static Text CreateUiText(string objectName, Transform parent, string content, int fontSize, FontStyle style)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.35f, 0.52f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateUiText("Label", buttonObject.transform, label, 14, FontStyle.Normal);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static void SetRect(GameObject target, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = pivot;
        rect.anchorMax = pivot;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
