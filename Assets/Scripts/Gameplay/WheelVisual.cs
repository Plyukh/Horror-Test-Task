using UnityEngine;

/// <summary>
/// Visual controller for wheel rotation based on car speed
/// </summary>
public class WheelVisual : MonoBehaviour
{
    private const float MIN_WHEEL_RADIUS = 0.0001f;
    private const float DEGREES_PER_RADIAN = 360f;
    private const float TWO_PI = 2f * Mathf.PI;

    [Header("References")]
    [SerializeField] private CarPathController carPathController;

    [Header("Wheels")]
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform rearLeft;
    [SerializeField] private Transform rearRight;

    [Header("Settings")]
    [Tooltip("Wheel radius in meters (used to compute rotation speed)")]
    [SerializeField] private float wheelRadius = 0.33f;

    [Tooltip("If speed <= stopSpeedEpsilon wheels won't spin")]
    [SerializeField] private float stopSpeedEpsilon = 0.02f;

    private float accumulatedDeg = 0f;

    private void Awake()
    {
        if (carPathController == null)
        {
            carPathController = GetComponentInParent<CarPathController>();
        }
    }

    private void Update()
    {
        if (carPathController == null) return;

        if (carPathController.IsParked)
        {
            accumulatedDeg = 0f;
            ResetWheelRotations();
            return;
        }

        float forwardSpeed = carPathController.CurrentSpeed;

        if (forwardSpeed <= stopSpeedEpsilon)
        {
            accumulatedDeg = 0f;
            ResetWheelRotations();
        }
        else
        {
            UpdateWheelRotation(forwardSpeed);
        }
    }

    private void UpdateWheelRotation(float forwardSpeed)
    {
        float radius = Mathf.Max(MIN_WHEEL_RADIUS, wheelRadius);
        float angularVelocityRadPerSec = forwardSpeed / radius;
        float degPerSec = angularVelocityRadPerSec * DEGREES_PER_RADIAN;
        accumulatedDeg += degPerSec * Time.deltaTime;

        ApplyRotationToWheel(frontLeft);
        ApplyRotationToWheel(frontRight);
        ApplyRotationToWheel(rearLeft);
        ApplyRotationToWheel(rearRight);
    }

    private void ApplyRotationToWheel(Transform wheel)
    {
        if (wheel == null) return;

        Vector3 euler = wheel.localRotation.eulerAngles;
        wheel.localRotation = Quaternion.Euler(accumulatedDeg, euler.y, euler.z);
    }

    private void ResetWheelRotations()
    {
        ResetWheelRotation(frontLeft);
        ResetWheelRotation(frontRight);
        ResetWheelRotation(rearLeft);
        ResetWheelRotation(rearRight);
    }

    private void ResetWheelRotation(Transform wheel)
    {
        if (wheel == null) return;

        Vector3 euler = wheel.localRotation.eulerAngles;
        wheel.localRotation = Quaternion.Euler(0f, euler.y, euler.z);
    }

    public void StopAndReset()
    {
        accumulatedDeg = 0f;
        ResetWheelRotations();
    }
}
