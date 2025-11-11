using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPathController : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints = new List<Transform>();
    public bool loop = false;

    [Header("Motion")]
    [Tooltip("Max forward speed on straight segments (m/s)")]
    public float maxForwardSpeed = 10f;
    [Tooltip("Acceleration (m/s^2)")]
    public float accel = 3f;
    [Tooltip("Deceleration (m/s^2) used for braking")]
    public float decel = 6f;
    [Tooltip("Rotate multiplier for body rotation while turning")]
    public float turnSpeed = 2f;
    [Tooltip("Distance considered as reached for a waypoint (m)")]
    public float arriveDistance = 0.25f;
    [Tooltip("Rotation alignment speed while final parking")]
    public float parkingAlignSpeed = 2f;
    [Tooltip("Threshold to consider rigidbody stopped (m/s)")]
    public float stopTolerance = 0.05f;

    [Header("Corner / Braking tuning")]
    public float minBrakingDist = 1.0f;
    public float maxBrakingDist = 6.0f;
    public float minCornerSpeed = 1.5f;
    public float parkingSpeed = 0.6f;

    [Header("Parking")]
    [Tooltip("If true, Rigidbody.isKinematic will be set true after final parking (locks car in place)")]
    public bool kinematicOnPark = true;

    [Header("Steering (for external visuals)")]
    [Range(0f, 45f)] public float maxSteerAngle = 30f;

    [Header("Headlights")]
    public Light[] headlights;

    [Header("Audio SFX")]
    [Tooltip("Optional AudioSource used to play car SFX")]
    public AudioSource audioSource;
    [Tooltip("Sound played when engine starts")]
    public AudioClip engineStartClip;
    [Tooltip("Sound played when engine stops")]
    public AudioClip engineStopClip;
    [Tooltip("Sound played when a car door opens")]
    public AudioClip doorOpenClip;
    [Tooltip("Sound played when a car door closes")]
    public AudioClip doorCloseClip;
    [Tooltip("Looped driving sound (assign an AudioClip and set audioSource.loop = true to play continuously)")]
    public AudioClip drivingLoopClip;

    [Header("Driver spawn (scene object)")]
    [Tooltip("Assign existing driver GameObject in the scene (will be activated and moved)")]
    public GameObject driverObject;
    [Tooltip("Transform where driver will be placed. If null the driver will be placed to the right of the car.")]
    public Transform driverSpawnPoint;
    [Tooltip("Delay after door close before enabling/moving the driver (seconds)")]
    public float driverSpawnDelayAfterDoorClose = 0.0f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    public float gizmoSphereRadius = 0.15f;

    // runtime
    Rigidbody rb;
    int currentIndex = 0;
    float currentSpeed = 0f;
    bool isFinished = false;

    // external state
    public bool IsParked { get; private set; } = false;

    public float CurrentSteerAngle { get; private set; } = 0f;
    public float CurrentSpeed => currentSpeed;

    const float DIR_EPSILON = 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("CarPathController requires a Rigidbody on the same GameObject.");
        else rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            isFinished = true;
            return;
        }

        currentIndex = 0;
        currentSpeed = 0f;
        IsParked = false;

        // play driving loop if assigned
        if (audioSource != null && drivingLoopClip != null)
        {
            audioSource.clip = drivingLoopClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();

            // optional: play engine start one-shot as well if provided
            if (engineStartClip != null)
                audioSource.PlayOneShot(engineStartClip);
        }
        else
        {
            // if no loop clip but engineStartClip assigned, play it as one-shot
            if (audioSource != null && engineStartClip != null)
                audioSource.PlayOneShot(engineStartClip);
        }
    }

    void FixedUpdate()
    {
        if (isFinished) return;
        if (waypoints == null || waypoints.Count == 0) return;
        if (rb == null) return;

        Transform target = waypoints[Mathf.Clamp(currentIndex, 0, waypoints.Count - 1)];
        Vector3 toTarget = target.position - transform.position;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float dist = flatToTarget.magnitude;
        bool isLast = currentIndex == waypoints.Count - 1;

        // Compute desired direction with protection from near-zero vectors
        Vector3 desiredDir;
        if (flatToTarget.sqrMagnitude > DIR_EPSILON * DIR_EPSILON)
        {
            desiredDir = flatToTarget.normalized;
        }
        else
        {
            if (isLast)
            {
                Vector3 wf = Vector3.ProjectOnPlane(target.forward, Vector3.up);
                desiredDir = (wf.sqrMagnitude > 1e-5f) ? wf.normalized : transform.forward;
            }
            else
            {
                desiredDir = transform.forward;
            }
        }

        // Steering angle for visuals
        float signedAngle = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up);
        CurrentSteerAngle = Mathf.Clamp(signedAngle, -maxSteerAngle, maxSteerAngle);
        float absAngle = Mathf.Abs(signedAngle);

        // braking distance grows with required steering angle
        float brakingDist = Mathf.Max(minBrakingDist, Mathf.Lerp(minBrakingDist, maxBrakingDist, Mathf.InverseLerp(0f, 90f, absAngle)));

        // compute target speed
        float targetSpeed = maxForwardSpeed;
        if (isLast)
        {
            if (dist < brakingDist)
            {
                // smoothly go to parkingSpeed then to zero as we approach
                targetSpeed = Mathf.Lerp(0f, parkingSpeed, Mathf.Clamp01(dist / brakingDist));
            }
        }
        else
        {
            float angleFactor = Mathf.InverseLerp(15f, 90f, absAngle);
            float angleSpeedFactor = Mathf.Lerp(1f, 0.35f, angleFactor);

            if (dist < brakingDist)
            {
                float speedByDist = Mathf.Lerp(minCornerSpeed, maxForwardSpeed, Mathf.Clamp01(dist / brakingDist));
                targetSpeed = speedByDist * angleSpeedFactor;
            }
            else
            {
                targetSpeed = maxForwardSpeed * angleSpeedFactor;
            }

            float minAllowed = Mathf.Max(0.1f, minCornerSpeed * (1f - angleFactor) + minCornerSpeed * angleFactor * 0.35f);
            targetSpeed = Mathf.Max(targetSpeed, minAllowed);
        }

        // accelerate/decelerate smoothly
        if (currentSpeed < targetSpeed)
            currentSpeed = Mathf.Min(targetSpeed, currentSpeed + accel * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.Max(targetSpeed, currentSpeed - decel * Time.fixedDeltaTime);

        // move forward
        Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // rotate predictably using RotateTowards to avoid sudden flips
        Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        float maxAngleThisStep = turnSpeed * 60f * Time.fixedDeltaTime * (1f + Mathf.Abs(CurrentSteerAngle) / 10f);
        Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, maxAngleThisStep);
        rb.MoveRotation(newRot);

        // arrival handling
        if (dist <= arriveDistance)
        {
            if (!isLast)
            {
                currentIndex++;
                if (loop && currentIndex >= waypoints.Count) currentIndex = 0;
            }
            else
            {
                StartCoroutine(FinishAndPark(target));
                isFinished = true;
            }
        }
    }

    IEnumerator FinishAndPark(Transform finalTarget)
    {
        // ensure we are not kinematic while approaching
        if (rb.isKinematic) rb.isKinematic = false;

        while (true)
        {
            // small positional steps toward target
            rb.MovePosition(Vector3.MoveTowards(transform.position, finalTarget.position, parkingSpeed * Time.deltaTime));

            // stable rotation towards final rotation (avoids flips)
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, finalTarget.rotation, parkingAlignSpeed * 100f * Time.deltaTime));

            // gently bring velocities to zero
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, decel * Time.deltaTime);
            rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, decel * Time.deltaTime);

            float posDelta = Vector3.Distance(transform.position, finalTarget.position);
            float angleDelta = Quaternion.Angle(transform.rotation, finalTarget.rotation);
            float speed = rb.linearVelocity.magnitude;
            if (posDelta <= 1f && angleDelta <= 0.5f && speed <= stopTolerance)
                break;

            yield return null;
        }

        // turn off headlights
        SetHeadlights(false);

        // snap exactly to final transform
        rb.MovePosition(finalTarget.position);
        rb.MoveRotation(finalTarget.rotation);

        // zero velocities and speed
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentSpeed = 0f;

        // set parked state AFTER we've zeroed velocities and currentSpeed
        if (kinematicOnPark)
            rb.isKinematic = true;
        else
            rb.constraints = RigidbodyConstraints.FreezeAll;

        IsParked = true;

        // Audio sequence: stop driving loop, play engine stop, then door open/close with delays
        StartCoroutine(PlayParkAudioSequence());
    }

    IEnumerator PlayParkAudioSequence()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Stop driving loop if it was playing
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == drivingLoopClip)
            audioSource.Stop();

        // Play engine stop sound
        if (audioSource != null && engineStopClip != null)
            audioSource.PlayOneShot(engineStopClip);

        // wait 2 seconds
        yield return new WaitForSeconds(2f);

        // play door open sound
        if (audioSource != null && doorOpenClip != null)
            audioSource.PlayOneShot(doorOpenClip);

        // wait another 2 seconds
        yield return new WaitForSeconds(2f);

        // play door close sound
        if (audioSource != null && doorCloseClip != null)
            audioSource.PlayOneShot(doorCloseClip);

        // optional small delay before enabling/moving the driver
        if (driverSpawnDelayAfterDoorClose > 0f)
            yield return new WaitForSeconds(driverSpawnDelayAfterDoorClose);

        // Activate and move existing driver object (if assigned)
        if (driverObject != null)
        {
            // place at spawn point or default position beside car
            Vector3 spawnPos = transform.position + transform.right * 1.2f + Vector3.up * 0.0f;
            Quaternion spawnRot = transform.rotation;
            if (driverSpawnPoint != null)
            {
                spawnPos = driverSpawnPoint.position;
                spawnRot = driverSpawnPoint.rotation;
            }

            // If driver has a CharacterController / Rigidbody, try to move using transform to avoid physics jumps
            driverObject.transform.position = spawnPos;
            driverObject.transform.rotation = spawnRot;

            // enable if disabled
            if (!driverObject.activeInHierarchy) driverObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("CarPathController: driverObject not assigned — cannot enable/move driver after parking.");
        }
    }


    // Force immediate stop and optionally lock position
    public void ForceStop(bool lockPosition = false)
    {
        currentSpeed = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (lockPosition)
            {
                rb.isKinematic = true;
                IsParked = true;
            }
        }
    }

    public void SetHeadlights(bool value)
    {
        foreach (var item in headlights)
        {
            if (item == null) continue;
            item.gameObject.SetActive(value);
        }
    }

    // Restart path
    public void RestartPath()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        if (rb == null) rb = GetComponent<Rigidbody>();
        isFinished = false;
        IsParked = false;
        currentIndex = 0;
        currentSpeed = 0f;
        if (rb.isKinematic) rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // restart driving loop if available
        if (audioSource != null && drivingLoopClip != null)
        {
            audioSource.clip = drivingLoopClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // Gizmos drawing: OnDrawGizmos so path visible without selection.
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
#if UNITY_EDITOR
        if (!UnityEditor.Handles.ShouldRenderGizmos()) return;
#endif
        Gizmos.color = gizmoColor;
        if (waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var w = waypoints[i];
            if (w == null) continue;
            Gizmos.DrawSphere(w.position, gizmoSphereRadius);

            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(w.position, waypoints[i + 1].position);
        }

        // draw arrow for last waypoint forward
        var last = waypoints[waypoints.Count - 1];
        if (last != null)
        {
            Vector3 p = last.position;
            Vector3 f = Vector3.ProjectOnPlane(last.forward, Vector3.up).normalized;
            if (f.sqrMagnitude > 0.0001f)
            {
                float len = 0.6f;
                Gizmos.DrawLine(p, p + f * len);
                Vector3 right = Quaternion.Euler(0, 150f, 0) * f * (len * 0.4f);
                Vector3 left = Quaternion.Euler(0, -150f, 0) * f * (len * 0.4f);
                Gizmos.DrawLine(p + f * len, p + f * len + right);
                Gizmos.DrawLine(p + f * len, p + f * len + left);
            }
        }
    }
}
