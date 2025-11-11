using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NPCPathController : MonoBehaviour
{
    [Header("Main")]
    public CharacterController cc;
    public Rigidbody rb;
    public NPCAnimatorController animatorController;

    [Header("Paths")]
    [Tooltip("List of named paths. Each path is a list of Transforms (waypoints).")]
    public List<PathData> paths = new List<PathData>();

    [Header("Runtime")]
    [Tooltip("Index of the selected path in Paths list (-1 = none)")]
    public int selectedPathIndex = -1;

    [Header("Motion")]
    public float moveSpeed = 2.0f;
    public float rotateSpeed = 360f;
    public float arriveDistance = 0.25f;
    public bool alignOnStop = true;

    [Header("Grounding")]
    public float groundSnapMaxDistance = 2f;
    public float groundedStick = -1f;

    [Header("Door detection")]
    [Tooltip("Distance in front of NPC to raycast for doors")]
    public float doorCheckDistance = 1.5f;
    [Tooltip("Half angle for directional cone when checking doors (degrees). Use small value like 15.")]
    public float doorCheckAngle = 30f;
    public LayerMask doorRaycastMask = ~0;

    // internal
    int currentWaypointIndex = 0;
    bool hasArrived = false;
    float verticalVelocity = 0f;

    // opened doors per active path (to avoid spamming Open)
    HashSet<DoorController> openedDoors = new HashSet<DoorController>();

    void Update()
    {
        if (selectedPathIndex < 0 || selectedPathIndex >= paths.Count) return;
        var path = paths[selectedPathIndex];
        if (path == null || path.waypoints == null || path.waypoints.Count == 0) return;

        Transform target = path.waypoints[Mathf.Clamp(currentWaypointIndex, 0, path.waypoints.Count - 1)];
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float dist = flatToTarget.magnitude;

        // If animator controller present, set walking state
        if (animatorController != null) animatorController.SetWalk(true);

        // Arrival detection
        if (dist <= arriveDistance)
        {
            // advance to next waypoint or finish path
            if (currentWaypointIndex < path.waypoints.Count - 1)
            {
                currentWaypointIndex++;
                hasArrived = false;
            }
            else
            {
                // final waypoint reached
                HandlePathFinished(path);
                return;
            }

            // face last direction if needed
            if (alignOnStop)
                FaceDirection(flatToTarget.normalized);

            return;
        }

        // movement toward current waypoint
        hasArrived = false;
        Vector3 moveDir = (flatToTarget.sqrMagnitude > 1e-6f) ? flatToTarget.normalized : transform.forward;

        // --- DOOR CHECK: raycast forward and try to open doors from path.doorsToOpen ---
        CheckAndOpenDoorAhead(path, moveDir);

        // gravity accumulation
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = groundedStick;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 horizontal = moveDir * moveSpeed;
        Vector3 motion = horizontal + Vector3.up * verticalVelocity;
        cc.Move(motion * Time.deltaTime);

        FaceDirection(moveDir);
    }

    void CheckAndOpenDoorAhead(PathData path, Vector3 moveDir)
    {
        if (path.doorsToOpen == null || path.doorsToOpen.Count == 0) return;
        if (moveDir.sqrMagnitude < 1e-6f) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Ray ray = new Ray(origin, moveDir);

        // simple raycast forward
        if (Physics.Raycast(ray, out RaycastHit hit, doorCheckDistance, doorRaycastMask, QueryTriggerInteraction.Ignore))
        {
            var hitDoor = hit.collider.GetComponentInParent<DoorController>();
            if (hitDoor != null && path.doorsToOpen.Contains(hitDoor))
            {
                Vector3 toDoor = (hitDoor.transform.position - transform.position);
                float ang = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(toDoor, Vector3.up));
                if (ang <= doorCheckAngle)
                {
                    if (!openedDoors.Contains(hitDoor))
                    {
                        // open door and schedule close
                        hitDoor.Open();
                        openedDoors.Add(hitDoor);
                        StartCoroutine(CloseDoorAfter(hitDoor, 1f));
                    }
                }
            }
        }
        else
        {
            float sphereRadius = 0.3f;
            if (Physics.SphereCast(origin, sphereRadius, moveDir, out RaycastHit sh, doorCheckDistance, doorRaycastMask, QueryTriggerInteraction.Ignore))
            {
                var hitDoor = sh.collider.GetComponentInParent<DoorController>();
                if (hitDoor != null && path.doorsToOpen.Contains(hitDoor) && !openedDoors.Contains(hitDoor))
                {
                    hitDoor.Open();
                    openedDoors.Add(hitDoor);
                    StartCoroutine(CloseDoorAfter(hitDoor, 1f));
                }
            }
        }
    }

    // Coroutine to close door after delay and allow it to be re-opened later
    IEnumerator CloseDoorAfter(DoorController door, float delay)
    {
        if (door == null) yield break;
        yield return new WaitForSeconds(delay);
        // Safety: only close if door still exists
        try
        {
            door.Close();
        }
        catch { }
        openedDoors.Remove(door);
    }

    void HandlePathFinished(PathData path)
    {
        // snap to ground & stop vertical movement
        verticalVelocity = 0f;
        SnapToGround();
        if (animatorController != null) animatorController.SetWalk(false);

        // mark arrived and call events
        if (!hasArrived)
        {
            hasArrived = true;
            path.onPathFinished?.Invoke();
        }

        // if looping, restart; otherwise clear selected path (or keep stopped)
        if (path.loop)
        {
            currentWaypointIndex = 0;
            hasArrived = false;
            // reset opened doors when looping
            openedDoors.Clear();
        }
        else
        {
            // keep at final waypoint; do not move further unless SetTargetPath called again
        }
    }

    void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundSnapMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            float dy = hit.point.y - transform.position.y;
            if (Mathf.Abs(dy) > 0.001f)
                cc.Move(new Vector3(0f, dy, 0f));
        }
    }

    void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 1e-6f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    // Public API

    // Select path by index and start from its first waypoint
    public void SetTargetPath(int pathIndex, bool restart = true)
    {
        if (pathIndex < 0 || pathIndex >= paths.Count)
        {
            selectedPathIndex = -1;
            return;
        }

        selectedPathIndex = pathIndex;
        if (restart) currentWaypointIndex = 0;
        hasArrived = false;
        openedDoors.Clear();
    }

    // Select path by name (first match)
    public bool SetTargetPath(string pathName, bool restart = true)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i] != null && paths[i].name == pathName)
            {
                SetTargetPath(i, restart);
                return true;
            }
        }
        return false;
    }

    // Jump to a specific waypoint index within current path
    public void SkipToWaypoint(int waypointIndex)
    {
        if (selectedPathIndex < 0 || selectedPathIndex >= paths.Count) return;
        var path = paths[selectedPathIndex];
        if (path == null || path.waypoints == null || path.waypoints.Count == 0) return;
        currentWaypointIndex = Mathf.Clamp(waypointIndex, 0, path.waypoints.Count - 1);
        hasArrived = false;
    }

    // Stop following path
    public void ClearPath()
    {
        selectedPathIndex = -1;
        hasArrived = false;
        openedDoors.Clear();
    }

    // Helper: check if NPC currently moving along a path
    public bool IsMoving()
    {
        return selectedPathIndex >= 0 && !hasArrived;
    }
}
