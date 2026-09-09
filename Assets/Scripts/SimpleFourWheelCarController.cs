using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class SimpleFourWheelCarController : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] private float motorForce = 18f;
    [SerializeField] private float steerSpeed = 85f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float coastDrag = 0.5f;
    [SerializeField] private float brakeDrag = 3f;

    [Header("Colors")]
    [SerializeField] private Color bodyColor = new Color(0.15f, 0.55f, 0.95f);
    [SerializeField] private Color wheelColor = new Color(0.12f, 0.12f, 0.12f);
    [SerializeField] private Color poleColor = new Color(0.18f, 0.18f, 0.18f);

    private Rigidbody rb;
    private Transform driveReference;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        CacheRigidbody();
        CacheDriveReference();
        ApplySetup();
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        CacheRigidbody();
        CacheDriveReference();
        ApplySetup();
    }

    private void OnValidate()
    {
        CacheRigidbody();
        CacheDriveReference();
        ApplySetup();
    }

    private void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.R))
        {
            ResetVehicle();
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying || rb == null)
        {
            return;
        }

        float throttle = Input.GetAxisRaw("Vertical");
        float steer = Input.GetAxisRaw("Horizontal");
        Vector3 driveForward = GetDriveForward();

        rb.drag = Mathf.Abs(throttle) > 0.01f ? coastDrag : brakeDrag;

        if (Mathf.Abs(throttle) > 0.01f)
        {
            float forwardSpeed = Vector3.Dot(rb.velocity, driveForward);
            if (Mathf.Abs(forwardSpeed) < maxSpeed || forwardSpeed * throttle < 0f)
            {
                rb.AddForce(driveForward * throttle * motorForce, ForceMode.Acceleration);
            }
        }

        if (Mathf.Abs(steer) > 0.01f)
        {
            float turn = steer * steerSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }
    }

    public void ResetVehicle()
    {
        if (rb == null)
        {
            CacheRigidbody();
        }

        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = startPosition;
        rb.rotation = startRotation;
        rb.Sleep();
    }

    private void CacheDriveReference()
    {
        driveReference = transform.Find("CarPole/CarCamera");
        if (driveReference == null)
        {
            driveReference = transform.Find("摄像头");
        }

        if (driveReference == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);
            driveReference = childCamera != null ? childCamera.transform : null;
        }
    }

    private Vector3 GetDriveForward()
    {
        Vector3 forward = driveReference != null ? driveReference.forward : transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
    }

    private void CacheRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void ApplySetup()
    {
        ApplyColorToChild("Car_Chassis", bodyColor);
        ApplyColorToChild("Car_Wheel_FrontLeft", wheelColor);
        ApplyColorToChild("Car_Wheel_FrontRight", wheelColor);
        ApplyColorToChild("Car_Wheel_RearLeft", wheelColor);
        ApplyColorToChild("Car_Wheel_RearRight", wheelColor);
        ApplyColorToChild("CarPole", poleColor);
    }

    private void ApplyColorToChild(string childName, Color color)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return;
        }

        Renderer renderer = child.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        if (renderer.sharedMaterial != null)
        {
            if (Application.isPlaying)
            {
                renderer.material.color = color;
            }
            else
            {
                renderer.sharedMaterial = EnsureEditableMaterial(renderer.sharedMaterial, color, childName);
            }
        }
        else
        {
            Material fallback = new Material(Shader.Find("Standard"));
            fallback.color = color;
            renderer.sharedMaterial = fallback;
        }
    }

    private Material EnsureEditableMaterial(Material source, Color color, string childName)
    {
        string materialName = name + "_" + childName + "_Mat";
        Material material = new Material(source)
        {
            name = materialName,
            color = color
        };
        return material;
    }
}
