using UnityEngine;

// The AI "brain". It gives CarController the same three values the player does
// (Throttle, Steer, Handbrake), but decides them by following the waypoints.
public class AICarInput : CarInputBase
{
    [SerializeField] private Transform waypoints;          // the parent object holding WP0, WP1, WP2...
    [SerializeField] private float reachDistance = 5f;     // how close counts as "arrived"
    [SerializeField] private float cornerSlowdown = 0.5f;  // how much to lift off the gas in turns

    private int currentWaypoint = 0;

    void Awake()
    {
        // If nothing was dragged in (e.g. the track was rebuilt), find the waypoints by name
        if (waypoints == null)
        {
            GameObject found = GameObject.Find("Waypoints");
            if (found != null) waypoints = found.transform;
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.childCount == 0) return;

        // 1. Which waypoint are we heading to?
        Transform target = waypoints.GetChild(currentWaypoint);

        // 2. Where is it, from the car's point of view? (x = left/right, z = ahead)
        Vector3 local = transform.InverseTransformPoint(target.position);
        local.y = 0f;

        // 3. Steer toward it (-1 = hard left, 1 = hard right)
        Steer = local.normalized.x;

        // 4. Full gas when straight, less gas in turns
        Throttle = 1f - Mathf.Abs(Steer) * cornerSlowdown;
        Handbrake = false;

        // 5. Close enough? Go to the next waypoint (and loop back to the first after the last)
        if (local.magnitude < reachDistance)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.childCount;
        }
    }

    // Draws a yellow line to the waypoint the AI is heading for (select the AI car in the Scene view to see it)
    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.childCount == 0) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, waypoints.GetChild(currentWaypoint).position);
    }
}
