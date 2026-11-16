using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

// Editor tool: builds a ProBuilder race track (road + grass ground + AI waypoints)
// and puts the cars on the start line.
// Run it from the menu: Tools > Forza > Build Race Track
// Running it again deletes the old "RaceTrack" object and builds a fresh one.
public static class RaceTrackBuilder
{
    const float RoadWidth = 12f;        // metres, edge to edge
    const float LineWidth = 0.35f;      // white edge lines
    const float RoadHeight = 0.03f;     // road sits slightly above the ground (no flickering)
    const float SampleStep = 2f;        // one road segment every 2 m
    const float WaypointSpacing = 8f;   // one AI waypoint every 8 m
    const float GroundSize = 260f;

    // The track's shape, seen from above (x, z). A smooth curve is drawn through these points, in order, as a loop.
    static readonly Vector2[] ControlPoints =
    {
        new Vector2(-46f, -40f),   // start line, main straight going forward (+Z)
        new Vector2(-46f, 0f),
        new Vector2(-46f, 40f),
        new Vector2(-36f, 62f),    // turn 1
        new Vector2(-11f, 68f),
        new Vector2(11f, 52f),
        new Vector2(14f, 28f),
        new Vector2(2f, 8f),       // S-bend
        new Vector2(9f, -14f),
        new Vector2(34f, -26f),
        new Vector2(46f, -50f),    // long sweeper
        new Vector2(32f, -72f),
        new Vector2(2f, -76f),
        new Vector2(-26f, -68f),
        new Vector2(-41f, -56f)    // last corner back onto the straight
    };

    [MenuItem("Tools/Forza/Build Race Track")]
    public static void Build()
    {
        GameObject old = GameObject.Find("RaceTrack");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject root = new GameObject("RaceTrack");
        Undo.RegisterCreatedObjectUndo(root, "Build Race Track");

        // 1. Points along the middle of the road, every 2 m
        List<Vector3> centre = SampleCentreline(SampleStep);
        int n = centre.Count;
        Vector3[] forward = new Vector3[n];
        Vector3[] right = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 f = centre[(i + 1) % n] - centre[(i - 1 + n) % n];
            f.y = 0f;
            forward[i] = f.normalized;
            right[i] = Vector3.Cross(Vector3.up, forward[i]);
        }

        Material roadMat = GetMaterial("Track_Road", new Color(0.18f, 0.18f, 0.19f), 0.25f);
        Material lineMat = GetMaterial("Track_Line", new Color(0.92f, 0.92f, 0.92f), 0.3f);
        Material grassMat = GetMaterial("Track_Grass", new Color(0.33f, 0.5f, 0.22f), 0.1f);

        // 2. Road mesh: across the road we have 3 strips -> white line | asphalt | white line
        float h = RoadWidth * 0.5f;
        float[] offsets = { -h, -h + LineWidth, h - LineWidth, h };
        int cols = offsets.Length;

        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < n; i++)
            foreach (float o in offsets)
                positions.Add(centre[i] + right[i] * o + Vector3.up * RoadHeight);

        List<Face> asphaltFaces = new List<Face>();
        List<Face> lineFaces = new List<Face>();
        for (int i = 0; i < n; i++)
        {
            int a = i * cols;
            int b = ((i + 1) % n) * cols;
            for (int c = 0; c < cols - 1; c++)
            {
                int l0 = a + c, r0 = a + c + 1, l1 = b + c, r1 = b + c + 1;
                Face face = new Face(new int[] { l0, l1, r1, l0, r1, r0 });
                if (c == 1) asphaltFaces.Add(face);
                else lineFaces.Add(face);
            }
        }

        List<Face> allFaces = new List<Face>(asphaltFaces);
        allFaces.AddRange(lineFaces);

        ProBuilderMesh road = ProBuilderMesh.Create(positions, allFaces);
        road.gameObject.name = "Road";
        road.transform.SetParent(root.transform, false);

        // Faces were added asphalt-first, so split the mesh's own faces the same way
        List<Face> meshAsphalt = new List<Face>();
        List<Face> meshLines = new List<Face>();
        IList<Face> meshFaces = road.faces;
        for (int k = 0; k < meshFaces.Count; k++)
        {
            if (k < asphaltFaces.Count) meshAsphalt.Add(meshFaces[k]);
            else meshLines.Add(meshFaces[k]);
        }
        road.SetMaterial(meshAsphalt, roadMat);
        road.SetMaterial(meshLines, lineMat);
        FinishMesh(road);

        // 3. Grass ground under everything
        float g = GroundSize * 0.5f;
        Vector3[] groundCorners =
        {
            new Vector3(-g, 0f, -g), new Vector3(-g, 0f, g),
            new Vector3(g, 0f, g),   new Vector3(g, 0f, -g)
        };
        ProBuilderMesh ground = ProBuilderMesh.Create(groundCorners, new[] { new Face(new int[] { 0, 1, 2, 0, 2, 3 }) });
        ground.gameObject.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localPosition = new Vector3(0f, -0.01f, -4f);
        ground.SetMaterial(ground.faces, grassMat);
        FinishMesh(ground);

        // 4. AI waypoints along the middle of the road, in driving order
        GameObject wpRoot = new GameObject("Waypoints");
        wpRoot.transform.SetParent(root.transform, false);
        int every = Mathf.Max(1, Mathf.RoundToInt(WaypointSpacing / SampleStep));
        Texture2D icon = EditorGUIUtility.IconContent("sv_icon_dot3_pix16_gizmo").image as Texture2D;
        int index = 0;
        for (int i = 0; i < n; i += every)
        {
            GameObject wp = new GameObject("WP" + index);
            wp.transform.SetParent(wpRoot.transform, false);
            wp.transform.position = centre[i] + Vector3.up * 0.5f;
            if (icon != null) EditorGUIUtility.SetIconForObject(wp, icon);
            index++;
        }

        // 5. Put the cars on the start line, facing down the main straight
        PlaceCars(centre[0], forward[0], right[0]);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Race track built: {Mathf.RoundToInt(n * SampleStep)} m long, {index} waypoints. Save the scene (Ctrl+S).");
    }

    static void FinishMesh(ProBuilderMesh mesh)
    {
        mesh.ToMesh();
        mesh.Refresh();
        UnityEditor.ProBuilder.EditorMeshUtility.Optimize(mesh);

        MeshCollider col = mesh.GetComponent<MeshCollider>();
        if (col == null) col = mesh.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
    }

    static void PlaceCars(Vector3 start, Vector3 forward, Vector3 right)
    {
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        CarInputBase[] cars = Object.FindObjectsByType<CarInputBase>(FindObjectsSortMode.None);

        int aiCount = 0;
        foreach (CarInputBase car in cars)
        {
            Transform t = car.transform;
            Undo.RecordObject(t, "Place car on start line");

            Vector3 position;
            if (car is PlayerCarInput)
            {
                position = start - right * 3f;                                   // left lane
            }
            else
            {
                position = start + right * 3f - forward * (8f * aiCount);        // right lane, lined up behind each other
                aiCount++;
            }

            t.position = position + Vector3.up * 0.9f;
            t.rotation = rotation;
        }
    }

    // Draws a smooth closed curve (Catmull-Rom) through the control points,
    // then picks points on it that are evenly 'step' metres apart.
    static List<Vector3> SampleCentreline(float step)
    {
        int count = ControlPoints.Length;
        List<Vector2> dense = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            Vector2 p0 = ControlPoints[(i - 1 + count) % count];
            Vector2 p1 = ControlPoints[i];
            Vector2 p2 = ControlPoints[(i + 1) % count];
            Vector2 p3 = ControlPoints[(i + 2) % count];
            for (int k = 0; k < 40; k++)
                dense.Add(CatmullRom(p0, p1, p2, p3, k / 40f));
        }

        int d = dense.Count;
        float[] length = new float[d + 1];
        for (int i = 1; i <= d; i++)
            length[i] = length[i - 1] + Vector2.Distance(dense[i - 1], dense[i % d]);

        float total = length[d];
        int samples = Mathf.FloorToInt(total / step);
        List<Vector3> result = new List<Vector3>(samples);
        int j = 0;
        for (int s = 0; s < samples; s++)
        {
            float target = s * total / samples;
            while (length[j + 1] < target) j++;
            float f = (target - length[j]) / (length[j + 1] - length[j]);
            Vector2 p = Vector2.Lerp(dense[j], dense[(j + 1) % d], f);
            result.Add(new Vector3(p.x, 0f, p.y));
        }
        return result;
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1)
                     + (-p0 + p2) * t
                     + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                     + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static Material GetMaterial(string name, Color color, float smoothness)
    {
        const string folder = "Assets/Assets/Forza/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Assets/Forza", "Materials");

        string path = folder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        UnityEditor.EditorUtility.SetDirty(mat);
        return mat;
    }
}
