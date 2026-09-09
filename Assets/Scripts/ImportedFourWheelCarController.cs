using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImportedFourWheelCarController : MonoBehaviour
{
    [Header("Source vehicle parameters")]
    [SerializeField, Range(1f, 100f)] private float motorTorqueScale = 10f;
    [SerializeField, Range(1f, 100f)] private float maxSteeringAngle = 45f;
    [SerializeField, Range(1f, 30f)] private float sidewaysFriction = 18f;
    [SerializeField, Range(1f, 10f)] private float forwardFriction = 7f;
    [SerializeField] private float keyboardMotorInput = 8f;
    [SerializeField] private bool useKeyboardInput = true;

    private Rigidbody carRigidbody;
    private WheelCollider frontLeftCollider;
    private WheelCollider frontRightCollider;
    private WheelCollider rearLeftCollider;
    private WheelCollider rearRightCollider;
    private Transform frontLeftVisual;
    private Transform frontRightVisual;
    private Transform rearLeftVisual;
    private Transform rearRightVisual;
    private Quaternion frontLeftBaseRotation;
    private Quaternion frontRightBaseRotation;
    private Quaternion rearLeftBaseRotation;
    private Quaternion rearRightBaseRotation;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float targetLeftMotorInput;
    private float targetRightMotorInput;
    private float targetSteeringInput;
    private float currentSteeringInput;

    private void Awake()
    {
        ResolveReferences();
        ConfigureVehicle();
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        HandleKeyboardInput();
        currentSteeringInput = Mathf.Lerp(
            currentSteeringInput,
            targetSteeringInput,
            Time.deltaTime * 30f
        );
    }

    private void FixedUpdate()
    {
        ApplyWheelForces();
        UpdateWheelVisuals();
    }

    public void SetThrottle(float leftMotorInput, float rightMotorInput)
    {
        targetLeftMotorInput = leftMotorInput;
        targetRightMotorInput = rightMotorInput;
    }

    public void SetSteering(float normalizedInput)
    {
        targetSteeringInput = Mathf.Clamp(normalizedInput, -1f, 1f);
    }

    public void ResetCar()
    {
        SetThrottle(0f, 0f);
        SetSteering(0f);
        currentSteeringInput = 0f;
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        carRigidbody.position = startPosition;
        carRigidbody.rotation = startRotation;
        Physics.SyncTransforms();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCar();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            motorTorqueScale = Mathf.Min(motorTorqueScale * 1.1f, 500f);
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            motorTorqueScale = Mathf.Max(motorTorqueScale * 0.9f, 0f);
        }

        if (!useKeyboardInput)
        {
            return;
        }

        float motorInput = 0f;
        if (Input.GetKey(KeyCode.W)) motorInput = keyboardMotorInput;
        if (Input.GetKey(KeyCode.S)) motorInput = -keyboardMotorInput;

        float steeringInput = 0f;
        if (Input.GetKey(KeyCode.A)) steeringInput = -1f;
        if (Input.GetKey(KeyCode.D)) steeringInput = 1f;

        if (Input.GetKey(KeyCode.Space))
        {
            motorInput = 0f;
        }

        SetThrottle(motorInput, motorInput);
        SetSteering(steeringInput);
    }

    private void ResolveReferences()
    {
        carRigidbody = GetComponent<Rigidbody>();
        frontLeftCollider = FindComponent<WheelCollider>("轮子/FrontLeft");
        frontRightCollider = FindComponent<WheelCollider>("轮子/FrontRight");
        rearLeftCollider = FindComponent<WheelCollider>("轮子/RearLeft");
        rearRightCollider = FindComponent<WheelCollider>("轮子/RearRight");

        const string visualRoot = "FourWheel_Model/Car_Blend_Visual/";
        frontLeftVisual = transform.Find(visualRoot + "Car_Wheel_FrontLeft");
        frontRightVisual = transform.Find(visualRoot + "Car_Wheel_FrontRight");
        rearLeftVisual = transform.Find(visualRoot + "Car_Wheel_RearLeft");
        rearRightVisual = transform.Find(visualRoot + "Car_Wheel_RearRight");

        frontLeftBaseRotation = GetRotationOffset(frontLeftCollider, frontLeftVisual);
        frontRightBaseRotation = GetRotationOffset(frontRightCollider, frontRightVisual);
        rearLeftBaseRotation = GetRotationOffset(rearLeftCollider, rearLeftVisual);
        rearRightBaseRotation = GetRotationOffset(rearRightCollider, rearRightVisual);
    }

    private void ConfigureVehicle()
    {
        carRigidbody.mass = 2f;
        carRigidbody.centerOfMass = new Vector3(0f, -0.01f, 0f);
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        carRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        carRigidbody.constraints = RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;

        ConfigureFriction(frontLeftCollider);
        ConfigureFriction(frontRightCollider);
        ConfigureFriction(rearLeftCollider);
        ConfigureFriction(rearRightCollider);
    }

    private void ApplyWheelForces()
    {
        float steeringAngle = maxSteeringAngle * currentSteeringInput;
        if (frontLeftCollider != null)
        {
            frontLeftCollider.steerAngle = steeringAngle - Mathf.Abs(steeringAngle) * 0.1f;
        }

        if (frontRightCollider != null)
        {
            frontRightCollider.steerAngle = steeringAngle + Mathf.Abs(steeringAngle) * 0.1f;
        }

        if (rearLeftCollider != null)
        {
            rearLeftCollider.motorTorque = motorTorqueScale * targetLeftMotorInput;
        }

        if (rearRightCollider != null)
        {
            rearRightCollider.motorTorque = motorTorqueScale * targetRightMotorInput;
        }
    }

    private void UpdateWheelVisuals()
    {
        UpdateWheelVisual(frontLeftCollider, frontLeftVisual, frontLeftBaseRotation);
        UpdateWheelVisual(frontRightCollider, frontRightVisual, frontRightBaseRotation);
        UpdateWheelVisual(rearLeftCollider, rearLeftVisual, rearLeftBaseRotation);
        UpdateWheelVisual(rearRightCollider, rearRightVisual, rearRightBaseRotation);
    }

    private void ConfigureFriction(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.extremumSlip = sidewaysFriction;
        sideways.extremumValue = sidewaysFriction * sidewaysFriction;
        wheel.sidewaysFriction = sideways;

        WheelFrictionCurve forward = wheel.forwardFriction;
        forward.extremumSlip = forwardFriction;
        forward.extremumValue = forwardFriction * forwardFriction;
        wheel.forwardFriction = forward;
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Quaternion GetRotationOffset(WheelCollider collider, Transform visual)
    {
        if (collider == null || visual == null)
        {
            return Quaternion.identity;
        }

        return Quaternion.Inverse(collider.transform.rotation) * visual.rotation;
    }

    private static void UpdateWheelVisual(
        WheelCollider collider,
        Transform visual,
        Quaternion baseRotation)
    {
        if (collider == null || visual == null)
        {
            return;
        }

        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        visual.position = position;
        visual.rotation = rotation * baseRotation;
    }
}
