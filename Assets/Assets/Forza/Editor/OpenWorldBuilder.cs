using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor tool: turns Unity's Terrain URP Demo into a driving world.
// Run it from the menu while your SampleScene (with your car) is open:
//     Tools > Forza > Build Open World
//
// What it does:
//  1. Saves your car as a prefab (Forza/Prefabs/PlayerCar.prefab)
//  2. Makes a copy of the terrain demo scene called Assets/Scenes/ForzaWorld.unity and opens it
//  3. Turns off the demo's cinematic cameras
//  4. Builds a 3.2 km road loop around the lake, following the hills
//  5. Flattens and blends the terrain under the road, removes trees, grass and rocks from it
//  6. Adds AI waypoints along the road
//  7. Puts your car and an AI car on the start line
// Running it again rebuilds the road and cars.
public static class OpenWorldBuilder
{
    const string DemoScenePath = "Assets/TerrainDemoScene_URP/Scenes/TerrainDemoScene.unity";
    const string WorldScenePath = "Assets/Scenes/ForzaWorld.unity";
    const string PrefabFolder = "Assets/Assets/Forza/Prefabs";
    const string CarPrefabPath = PrefabFolder + "/PlayerCar.prefab";
    const string GeneratedFolder = "Assets/Assets/Forza/Generated";
    const string MaterialFolder = "Assets/Assets/Forza/Materials";

    const float RoadWidth = 10f;          // metres, edge to edge
    const float LineWidth = 0.3f;         // white edge lines
    const float CentreLineWidth = 0.25f;  // dashed centre line (half of it each side of the middle)
    const float SampleStep = 2f;          // one road segment every 2 m
    const float MaxGrade = 0.07f;         // steepest the road may climb: 7%
    const float FlatMargin = 1f;          // flat ground on each side of the road before blending into the hills
    const float TerrainBelowRoad = 0.06f; // terrain sits just under the road surface
    const float SkirtOut = 1.5f;          // hidden road edges that dip into the ground
    const float SkirtDown = 0.8f;
    const int WaypointEvery = 5;          // one AI waypoint every 5 samples = 10 m

    static float RoadHalf => RoadWidth * 0.5f;

    // Road shape seen from above (world x, z). Designed from the demo terrain's heightmap:
    // starts on the long straight south of the lake heading east, loops round the east side and back.
    static readonly Vector2[] ControlPoints =
    {
        new Vector2(-480.0f, -39.2f),
        new Vector2(-450.0f, -39.2f),
        new Vector2(-420.0f, -38.3f),
        new Vector2(-389.9f, -37.5f),
        new Vector2(-359.9f, -36.7f),
        new Vector2(-329.9f, -35.8f),
        new Vector2(-299.9f, -35.0f),
        new Vector2(-269.9f, -34.2f),
        new Vector2(-239.8f, -33.5f),
        new Vector2(-209.8f, -32.7f),
        new Vector2(-179.8f, -32.0f),
        new Vector2(-149.8f, -31.2f),
        new Vector2(-119.8f, -30.5f),
        new Vector2(-89.7f, -29.7f),
        new Vector2(-59.7f, -29.0f),
        new Vector2(-29.7f, -28.2f),
        new Vector2(0.3f, -27.5f),
        new Vector2(30.3f, -26.7f),
        new Vector2(60.4f, -26.0f),
        new Vector2(90.3f, -26.0f),
        new Vector2(120.2f, -27.9f),
        new Vector2(150.1f, -31.3f),
        new Vector2(179.9f, -35.0f),
        new Vector2(209.6f, -38.7f),
        new Vector2(239.2f, -43.3f),
        new Vector2(267.6f, -51.3f),
        new Vector2(294.6f, -63.7f),
        new Vector2(321.0f, -78.0f),
        new Vector2(347.4f, -92.3f),
        new Vector2(373.8f, -106.7f),
        new Vector2(400.1f, -121.2f),
        new Vector2(426.4f, -135.8f),
        new Vector2(451.9f, -147.0f),
        new Vector2(474.4f, -145.3f),
        new Vector2(493.3f, -127.3f),
        new Vector2(508.1f, -102.3f),
        new Vector2(514.5f, -75.4f),
        new Vector2(512.1f, -46.5f),
        new Vector2(506.7f, -16.9f),
        new Vector2(500.0f, 12.2f),
        new Vector2(490.2f, 40.4f),
        new Vector2(478.3f, 67.8f),
        new Vector2(465.8f, 95.2f),
        new Vector2(454.8f, 122.9f),
        new Vector2(446.9f, 151.7f),
        new Vector2(441.5f, 181.1f),
        new Vector2(436.5f, 210.8f),
        new Vector2(432.0f, 240.4f),
        new Vector2(428.7f, 270.2f),
        new Vector2(426.1f, 300.2f),
        new Vector2(423.8f, 330.1f),
        new Vector2(421.0f, 359.9f),
        new Vector2(416.3f, 389.4f),
        new Vector2(409.0f, 418.5f),
        new Vector2(400.8f, 447.4f),
        new Vector2(392.5f, 476.2f),
        new Vector2(383.0f, 504.5f),
        new Vector2(369.6f, 530.7f),
        new Vector2(352.1f, 554.8f),
        new Vector2(333.4f, 578.3f),
        new Vector2(314.1f, 600.9f),
        new Vector2(291.8f, 619.4f),
        new Vector2(265.2f, 631.6f),
        new Vector2(236.7f, 641.1f),
        new Vector2(208.3f, 650.5f),
        new Vector2(180.7f, 653.4f),
        new Vector2(154.8f, 643.2f),
        new Vector2(129.8f, 626.5f),
        new Vector2(107.2f, 608.5f),
        new Vector2(93.0f, 585.8f),
        new Vector2(88.2f, 557.8f),
        new Vector2(87.0f, 527.8f),
        new Vector2(86.1f, 497.8f),
        new Vector2(86.6f, 467.8f),
        new Vector2(88.5f, 437.9f),
        new Vector2(91.0f, 408.0f),
        new Vector2(92.3f, 378.2f),
        new Vector2(89.6f, 348.8f),
        new Vector2(82.3f, 320.0f),
        new Vector2(73.2f, 291.4f),
        new Vector2(61.8f, 264.6f),
        new Vector2(43.9f, 242.9f),
        new Vector2(20.0f, 226.0f),
        new Vector2(-5.7f, 210.6f),
        new Vector2(-32.2f, 197.2f),
        new Vector2(-60.3f, 188.9f),
        new Vector2(-89.9f, 184.7f),
        new Vector2(-119.7f, 181.5f),
        new Vector2(-149.6f, 178.3f),
        new Vector2(-179.5f, 176.1f),
        new Vector2(-209.4f, 176.0f),
        new Vector2(-239.5f, 177.0f),
        new Vector2(-269.5f, 178.0f),
        new Vector2(-299.5f, 179.0f),
        new Vector2(-329.3f, 178.2f),
        new Vector2(-358.9f, 173.8f),
        new Vector2(-388.2f, 167.5f),
        new Vector2(-417.6f, 161.2f),
        new Vector2(-446.7f, 154.4f),
        new Vector2(-474.5f, 144.4f),
        new Vector2(-500.4f, 129.8f),
        new Vector2(-525.4f, 113.1f),
        new Vector2(-547.4f, 94.5f),
        new Vector2(-561.5f, 70.6f),
        new Vector2(-568.9f, 42.4f),
        new Vector2(-569.1f, 14.2f),
        new Vector2(-557.8f, -11.2f),
        new Vector2(-537.0f, -29.7f),
        new Vector2(-509.9f, -37.5f)
    };

    [MenuItem("Tools/Forza/Build Open World")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldScenePath) == null)
        {
            Debug.LogError("Build Open World: can't find the Terrain URP Demo scene at " + DemoScenePath + ". Import 'Unity Terrain - URP Demo Scene' first.");
            return;
        }

        // 1. Save the player car as a prefab (only when we're not already in the world scene)
        PlayerCarInput player = Object.FindFirstObjectByType<PlayerCarInput>();
        if (player != null && SceneManager.GetActiveScene().path != WorldScenePath)
        {
            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(player.gameObject, CarPrefabPath, out bool saved);
            if (!saved)
            {
                Debug.LogError("Build Open World: couldn't save the car prefab.");
                return;
            }
        }

        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabPath);
        if (carPrefab == null)
        {
            Debug.LogError("Build Open World: no car prefab yet. Open the scene with your car (SampleScene) and run this again.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            // 2. Copy the demo scene (first time only) and open it
            Progress("Opening world scene", 0.05f);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldScenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(DemoScenePath, WorldScenePath))
                {
                    Debug.LogError("Build Open World: couldn't copy the demo scene.");
                    return;
                }
            }
            Scene scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            // 3. Turn off the demo's cinematic cameras, and remove anything from a previous build
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "Main Camera" || go.name == "VirtualCameras" || go.name == "Timelines" || go.name == "HDRP Compositor")
                    go.SetActive(false);
                if (go.name == "OpenWorldRoad" || go.name == "PlayerCar" || go.name == "AICar")
                    Object.DestroyImmediate(go);
            }

            List<Terrain> terrains = new List<Terrain>();
            foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                if (t.GetComponent<TerrainCollider>() != null) terrains.Add(t);
            if (terrains.Count == 0)
            {
                Debug.LogError("Build Open World: no terrain found in the world scene.");
                return;
            }

            // 4. Road centre line, ground height under it, and a smooth road height profile
            Progress("Planning the road", 0.15f);
            List<Vector3> centre = SampleCentreline(SampleStep);
            int n = centre.Count;
            Vector3[] forward = new Vector3[n];
            Vector3[] right = new Vector3[n];
            float[] ground = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 f = centre[(i + 1) % n] - centre[(i - 1 + n) % n];
                f.y = 0f;
                forward[i] = f.normalized;
                right[i] = Vector3.Cross(Vector3.up, forward[i]);
                ground[i] = SampleTerrain(terrains, centre[i], i > 0 ? ground[i - 1] : 0f);
            }
            float[] elev = BuildProfile(ground);

            // 5. Shape the terrain under the road (also clears trees and grass)
            for (int t = 0; t < terrains.Count; t++)
            {
                Progress("Shaping terrain " + (t + 1) + "/" + terrains.Count, 0.2f + 0.5f * t / terrains.Count);
                ShapeTerrain(terrains[t], centre, elev, ground);
            }

            Progress("Clearing rocks", 0.72f);
            HideRocksNearRoad(scene, centre);

            // 6. Road mesh, waypoints, cars
            Progress("Building the road mesh", 0.8f);
            GameObject root = new GameObject("OpenWorldRoad");
            SceneManager.MoveGameObjectToScene(root, scene);
            BuildRoadMesh(root.transform, centre, right, elev);

            GameObject waypoints = new GameObject("Waypoints");
            waypoints.transform.SetParent(root.transform, false);
            Texture2D icon = EditorGUIUtility.IconContent("sv_icon_dot3_pix16_gizmo").image as Texture2D;
            int index = 0;
            for (int i = 0; i < n; i += WaypointEvery)
            {
                GameObject wp = new GameObject("WP" + index);
                wp.transform.SetParent(waypoints.transform, false);
                wp.transform.position = centre[i] + Vector3.up * (elev[i] + 0.5f);
                if (icon != null) EditorGUIUtility.SetIconForObject(wp, icon);
                index++;
            }

            Progress("Placing cars", 0.9f);
            Vector3 start = centre[0] + Vector3.up * (elev[0] + 1f);
            Quaternion rotation = Quaternion.LookRotation(forward[0], Vector3.up);

            GameObject playerCar = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab, scene);
            playerCar.name = "PlayerCar";
            playerCar.transform.SetPositionAndRotation(start - right[0] * 2.5f, rotation);

            GameObject aiCar = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab, scene);
            PrefabUtility.UnpackPrefabInstance(aiCar, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            aiCar.name = "AICar";
            foreach (Camera cam in aiCar.GetComponentsInChildren<Camera>(true))
                Object.DestroyImmediate(cam.gameObject);
            PlayerCarInput aiKeyboard = aiCar.GetComponent<PlayerCarInput>();
            if (aiKeyboard != null) Object.DestroyImmediate(aiKeyboard);
            AICarInput brain = aiCar.AddComponent<AICarInput>();
            SerializedObject so = new SerializedObject(brain);
            so.FindProperty("waypoints").objectReferenceValue = waypoints.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            aiCar.transform.SetPositionAndRotation(start + right[0] * 2.5f, rotation);

            // 7. Save everything
            Progress("Saving", 0.97f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = playerCar;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log($"Open world built: {Mathf.RoundToInt(n * SampleStep)} m road, {index} waypoints. Scene saved as {WorldScenePath}. Press Play!");
        }
        finally
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }

    // ---------- Road path ----------

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
            for (int k = 0; k < 20; k++)
                dense.Add(CatmullRom(p0, p1, p2, p3, k / 20f));
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

    static float SampleTerrain(List<Terrain> terrains, Vector3 p, float fallback)
    {
        foreach (Terrain t in terrains)
        {
            Vector3 tp = t.GetPosition();
            Vector3 size = t.terrainData.size;
            if (p.x >= tp.x && p.x <= tp.x + size.x && p.z >= tp.z && p.z <= tp.z + size.z)
                return t.SampleHeight(new Vector3(p.x, 0f, p.z)) + tp.y;
        }
        return fallback;
    }

    // Smooth the ground heights along the road, then limit how steep it can climb.
    static float[] BuildProfile(float[] ground)
    {
        int n = ground.Length;
        float[] p = (float[])ground.Clone();
        for (int pass = 0; pass < 3; pass++) p = MovingAverage(p, 41);

        float maxStep = MaxGrade * SampleStep;
        for (int iter = 0; iter < 4; iter++)
        {
            for (int i = 1; i < 2 * n; i++)
            {
                float prev = p[(i - 1) % n];
                p[i % n] = Mathf.Clamp(p[i % n], prev - maxStep, prev + maxStep);
            }
            for (int i = 2 * n; i > 0; i--)
            {
                float next = p[(i + 1) % n];
                p[i % n] = Mathf.Clamp(p[i % n], next - maxStep, next + maxStep);
            }
        }
        return MovingAverage(p, 11);
    }

    static float[] MovingAverage(float[] a, int window)
    {
        int n = a.Length, half = window / 2;
        float[] r = new float[n];
        for (int i = 0; i < n; i++)
        {
            float sum = 0f;
            for (int k = -half; k <= half; k++) sum += a[((i + k) % n + n) % n];
            r[i] = sum / (2 * half + 1);
        }
        return r;
    }

    // ---------- Terrain ----------

    static void ShapeTerrain(Terrain terrain, List<Vector3> centre, float[] elev, float[] ground)
    {
        TerrainData td = terrain.terrainData;
        Vector3 tp = terrain.GetPosition();
        Vector3 size = td.size;
        int res = td.heightmapResolution;
        float cellX = size.x / (res - 1);
        float cellZ = size.z / (res - 1);
        int n = centre.Count;

        float[] dist = new float[res * res];
        float[] target = new float[res * res];
        float[] fall = new float[res * res];
        for (int k = 0; k < dist.Length; k++) dist[k] = float.MaxValue;
        bool touched = false;

        // For every heightmap point near the road: how far is it from the road, and how high is the road there?
        for (int i = 0; i < n; i++)
        {
            int i2 = (i + 1) % n;
            Vector2 a = new Vector2(centre[i].x, centre[i].z);
            Vector2 b = new Vector2(centre[i2].x, centre[i2].z);
            float diff = Mathf.Max(Mathf.Abs(elev[i] - ground[i]), Mathf.Abs(elev[i2] - ground[i2]));
            float falloff = Mathf.Clamp(8f + 2.5f * diff, 8f, 35f);   // bigger cuts/fills blend over a wider slope
            float reach = RoadHalf + FlatMargin + falloff;

            int x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - reach - tp.x) / cellX));
            int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + reach - tp.x) / cellX));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - reach - tp.z) / cellZ));
            int z1 = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + reach - tp.z) / cellZ));
            if (x0 > x1 || z0 > z1) continue;

            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 pt = new Vector2(tp.x + x * cellX, tp.z + z * cellZ);
                    float t01 = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(pt - a, ab) / len2) : 0f;
                    float d = Vector2.Distance(pt, a + ab * t01);
                    if (d > reach) continue;
                    int k = z * res + x;
                    if (d < dist[k])
                    {
                        dist[k] = d;
                        target[k] = Mathf.Lerp(elev[i], elev[i2], t01);
                        fall[k] = falloff;
                        touched = true;
                    }
                }
            }
        }
        if (!touched) return;

        // Heights: flat under the road, smoothly blending back into the original hills
        float[,] heights = td.GetHeights(0, 0, res, res);
        float core = RoadHalf + FlatMargin;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int k = z * res + x;
                if (dist[k] == float.MaxValue) continue;
                float roadH = (target[k] - TerrainBelowRoad - tp.y) / size.y;
                float h;
                if (dist[k] <= core) h = roadH;
                else
                {
                    float s = Mathf.Clamp01((dist[k] - core) / fall[k]);
                    s = s * s * (3f - 2f * s);
                    h = Mathf.Lerp(roadH, heights[z, x], s);
                }
                heights[z, x] = Mathf.Clamp01(h);
            }
        }
        td.SetHeights(0, 0, heights);

        // Trees: remove any standing on or right next to the road
        TreeInstance[] trees = td.treeInstances;
        List<TreeInstance> keep = new List<TreeInstance>(trees.Length);
        foreach (TreeInstance tree in trees)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(tree.position.x * (res - 1)), 0, res - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(tree.position.z * (res - 1)), 0, res - 1);
            if (dist[z * res + x] > RoadHalf + 4f) keep.Add(tree);
        }
        if (keep.Count != trees.Length) td.SetTreeInstances(keep.ToArray(), true);

        // Grass and small plants: clear them from the road
        int dres = td.detailResolution;
        int layers = td.detailPrototypes.Length;
        if (dres > 0 && layers > 0)
        {
            bool[] clear = new bool[dres * dres];
            bool any = false;
            for (int z = 0; z < dres; z++)
            {
                for (int x = 0; x < dres; x++)
                {
                    int hx = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / dres * (res - 1)), 0, res - 1);
                    int hz = Mathf.Clamp(Mathf.RoundToInt((z + 0.5f) / dres * (res - 1)), 0, res - 1);
                    if (dist[hz * res + hx] < RoadHalf + 2f) { clear[z * dres + x] = true; any = true; }
                }
            }
            if (any)
            {
                for (int layer = 0; layer < layers; layer++)
                {
                    int[,] map = td.GetDetailLayer(0, 0, dres, dres, layer);
                    for (int z = 0; z < dres; z++)
                        for (int x = 0; x < dres; x++)
                            if (clear[z * dres + x]) map[z, x] = 0;
                    td.SetDetailLayer(0, 0, layer, map);
                }
            }
        }

        UnityEditor.EditorUtility.SetDirty(td);
        terrain.Flush();
    }

    static void HideRocksNearRoad(Scene scene, List<Vector3> centre)
    {
        foreach (GameObject rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name != "Rocks") continue;
            foreach (Transform rock in rootGo.transform)
            {
                Renderer[] renderers = rock.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                Vector2 c = new Vector2(bounds.center.x, bounds.center.z);
                foreach (Vector3 p in centre)
                {
                    if (Vector2.Distance(c, new Vector2(p.x, p.z)) < RoadHalf + 3f + radius)
                    {
                        rock.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }
    }

    // ---------- Road mesh ----------

    static void BuildRoadMesh(Transform parent, List<Vector3> centre, Vector3[] right, float[] elev)
    {
        int n = centre.Count;
        float h = RoadHalf;
        float c = CentreLineWidth * 0.5f;
        // Across the road (left to right): skirt | line | asphalt | dashed centre | asphalt | line | skirt
        Vector2[] cols =
        {
            new Vector2(-h - SkirtOut, -SkirtDown),
            new Vector2(-h, 0f), new Vector2(-h + LineWidth, 0f),
            new Vector2(-c, 0f), new Vector2(c, 0f),
            new Vector2(h - LineWidth, 0f), new Vector2(h, 0f),
            new Vector2(h + SkirtOut, -SkirtDown)
        };
        int cc = cols.Length;

        List<Vector3> verts = new List<Vector3>(n * cc);
        List<Vector2> uvs = new List<Vector2>(n * cc);
        for (int i = 0; i < n; i++)
        {
            foreach (Vector2 col in cols)
            {
                verts.Add(centre[i] + right[i] * col.x + Vector3.up * (elev[i] + col.y));
                uvs.Add(new Vector2(col.x / RoadWidth + 0.5f, i * SampleStep / 8f));
            }
        }

        List<int> asphalt = new List<int>();
        List<int> lines = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int a = i * cc;
            int b = ((i + 1) % n) * cc;
            for (int s = 0; s < cc - 1; s++)
            {
                bool isLine = s == 1 || s == cc - 3 || (s == 3 && (i / 2) % 2 == 0);
                List<int> list = isLine ? lines : asphalt;
                int l0 = a + s, r0 = a + s + 1, l1 = b + s, r1 = b + s + 1;
                list.Add(l0); list.Add(l1); list.Add(r1);
                list.Add(l0); list.Add(r1); list.Add(r0);
            }
        }

        Mesh mesh = new Mesh { name = "OpenWorldRoad" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(asphalt, 0);
        mesh.SetTriangles(lines, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        EnsureFolder(GeneratedFolder);
        string meshPath = GeneratedFolder + "/OpenWorldRoad.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject road = new GameObject("Road", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        road.transform.SetParent(parent, false);
        road.GetComponent<MeshFilter>().sharedMesh = mesh;
        road.GetComponent<MeshCollider>().sharedMesh = mesh;
        road.GetComponent<MeshRenderer>().sharedMaterials = new[]
        {
            GetMaterial("Track_Road", new Color(0.18f, 0.18f, 0.19f), 0.25f),
            GetMaterial("Track_Line", new Color(0.92f, 0.92f, 0.92f), 0.3f)
        };
    }

    // ---------- Helpers ----------

    static Material GetMaterial(string name, Color color, float smoothness)
    {
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }

    static void Progress(string info, float amount)
    {
        UnityEditor.EditorUtility.DisplayProgressBar("Build Open World", info, amount);
    }
}
