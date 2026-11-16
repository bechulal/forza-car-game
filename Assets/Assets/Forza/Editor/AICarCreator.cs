using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: makes an AI copy of your player car and puts it on the start line.
// Run it from the menu: Tools > Forza > Create AI Car
// Running it again replaces the old "AICar" with a fresh copy of your current player car.
public static class AICarCreator
{
    [MenuItem("Tools/Forza/Create AI Car")]
    public static void Create()
    {
        PlayerCarInput player = Object.FindFirstObjectByType<PlayerCarInput>();
        if (player == null)
        {
            Debug.LogError("Create AI Car: no car with PlayerCarInput found in the scene.");
            return;
        }

        GameObject old = GameObject.Find("AICar");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // 1. Copy the whole player car (body, wheels, colliders, smoke, CarController...)
        GameObject ai = Object.Instantiate(player.gameObject);
        ai.name = "AICar";
        Undo.RegisterCreatedObjectUndo(ai, "Create AI Car");

        // 2. Remove the camera from the copy (only the player car should have one)
        foreach (Camera cam in ai.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(cam.gameObject);

        // 3. Swap the keyboard input for the AI brain
        Object.DestroyImmediate(ai.GetComponent<PlayerCarInput>());
        AICarInput brain = ai.AddComponent<AICarInput>();

        // 4. Give the AI the waypoints
        Transform waypoints = FindWaypoints();
        if (waypoints != null)
        {
            SerializedObject so = new SerializedObject(brain);
            so.FindProperty("waypoints").objectReferenceValue = waypoints;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("Create AI Car: no 'Waypoints' object found. Build the race track first (Tools > Forza > Build Race Track).");
        }

        // 5. Put it on the start line, right lane, facing the same way as the track
        if (waypoints != null && waypoints.childCount > 1)
        {
            Vector3 a = waypoints.GetChild(0).position;
            Vector3 b = waypoints.GetChild(1).position;
            Vector3 forward = b - a;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            ai.transform.position = new Vector3(a.x, 0f, a.z) + right * 3f + Vector3.up * 0.9f;
            ai.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        else
        {
            ai.transform.position = player.transform.position + player.transform.right * 6f;
            ai.transform.rotation = player.transform.rotation;
        }

        EditorSceneManager.MarkSceneDirty(ai.scene);
        Selection.activeGameObject = ai;
        Debug.Log("AI car created. Save the scene (Ctrl+S) and press Play.");
    }

    static Transform FindWaypoints()
    {
        GameObject go = GameObject.Find("RaceTrack/Waypoints");
        if (go == null) go = GameObject.Find("Waypoints");
        return go != null ? go.transform : null;
    }
}
