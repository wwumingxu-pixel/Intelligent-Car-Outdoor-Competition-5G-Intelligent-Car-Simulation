using UnityEngine;

public class ThirdPersonCarCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followHeight = 0.75f;
    [SerializeField] private float followDistance = 1.4f;
    [SerializeField] private float lookHeight = 0.15f;
    [SerializeField] private float lookAhead = 0.4f;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSharpness = 14f;

    private Vector3 velocity;
    private Transform headingReference;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        headingReference = FindHeadingReference(target);
        SnapToTarget();
    }

    private void OnEnable()
    {
        ResolveTarget();
        SnapToTarget();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        ResolveTarget();
        if (target == null)
        {
            return;
        }

        Vector3 heading = GetHeading();
        Vector3 desiredPosition = target.position - heading * followDistance + Vector3.up * followHeight;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            positionSmoothTime
        );

        Vector3 lookPoint = target.position + heading * lookAhead + Vector3.up * lookHeight;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 heading = GetHeading();
        transform.position = target.position - heading * followDistance + Vector3.up * followHeight;
        transform.LookAt(target.position + heading * lookAhead + Vector3.up * lookHeight, Vector3.up);
        velocity = Vector3.zero;
    }

    private Vector3 GetHeading()
    {
        if (headingReference == null && target != null)
        {
            headingReference = FindHeadingReference(target);
        }

        Vector3 heading = headingReference != null ? headingReference.forward : target.forward;
        heading.y = 0f;
        return heading.sqrMagnitude > 0.0001f ? heading.normalized : target.forward;
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            GameObject car = GameObject.Find("CarModel");
            target = car != null ? car.transform : null;
        }

        if (target != null && headingReference == null)
        {
            headingReference = FindHeadingReference(target);
        }
    }

    private static Transform FindHeadingReference(Transform car)
    {
        if (car == null)
        {
            return null;
        }

        Transform reference = car.Find("CarPole/CarCamera");
        if (reference == null)
        {
            reference = car.Find("摄像头");
        }

        if (reference == null)
        {
            Camera childCamera = car.GetComponentInChildren<Camera>(true);
            reference = childCamera != null ? childCamera.transform : null;
        }

        return reference;
    }
}
