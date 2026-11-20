using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click scene setup for the escape room plan. Run "Tools > Escape Room > Apply Full Setup"
/// with Assets/Scenes/EscapeRoom.unity open, then review and save the scene.
/// A backup copy of the scene is written first (Assets/Scenes/Backups).
/// Every step can be re-run; generated objects are rebuilt rather than duplicated.
/// </summary>
public static class EscapeRoomSetup
{
    private const string Root = "Assets/EscapeRoom";
    private const string GeneratedDir = Root + "/Generated";

    private const int WallLayer = 3;
    private const int OutdoorLayer = 7;
    private const int LampLayer = 8;
    private const int RobotLayer = 9;

    private const float TileSize = 0.5f;
    private const float TileThickness = 0.15f;

    // c7 on the board (verified from the piece layout; see the plan doc).
    private static readonly Vector3 C7 = new Vector3(3.5625f, 1f, 3.3375f);
    private static readonly Vector3 RoomStartPosition = new Vector3(0.04f, 0f, -4.15f);
    private static readonly Vector3 LobbyCenter = new Vector3(0f, 0f, 200f);

    private static Scene _scene;

    // ------------------------------------------------------------------ entry points

    [MenuItem("Tools/Escape Room/Apply Full Setup")]
    public static void ApplyAll()
    {
        if (!Prepare()) return;

        EnsureLayers();
        FixRigAndLocomotion();
        FixRoomGeometry();
        SetupLightPuzzle();
        SetupWand();
        SetupSafe();
        SetupKnight();
        SetupRobotsAndBaton();
        SetupLightsaber();
        WallBreachTracker tracker = BuildWalls();
        GameObject outsideTeleport = SetupOutdoor();
        SetupTeleportSurfaces(tracker, outsideTeleport);
        SetupMenusAndFlow(tracker);
        ProjectSettingsFixes();

        Finish("Escape room setup applied. Review the scene, then save it (Ctrl+S).");
    }

    /// <summary>Batch-mode entry point: -executeMethod EscapeRoomSetup.ApplyAllBatch</summary>
    public static void ApplyAllBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/EscapeRoom.unity", OpenSceneMode.Single);
        ApplyAll();
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("[EscapeRoomSetup] Scene saved to " + _scene.path);
    }

    /// <summary>
    /// Adds only the time limit HUD, pointer ray lines and end-screen achievements to the open scene,
    /// leaving everything else (room sizes, walls, puzzles) untouched.
    /// </summary>
    [MenuItem("Tools/Escape Room/Steps/Add Timer, Ray Lines, Achievements")]
    public static void AddTimerRayAchievements()
    {
        if (!Prepare()) return;

        GameFlow flow = Object.FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);
        if (flow == null) { Debug.LogWarning("[EscapeRoomSetup] GameFlow not found; run Apply Full Setup instead."); return; }
        GameObject flowGo = flow.gameObject;
        ExitMonitor exit = flowGo.GetComponent<ExitMonitor>();
        GameTimer timer = GetOrAdd<GameTimer>(flowGo);
        AchievementTracker achievements = GetOrAdd<AchievementTracker>(flowGo);

        SetRef(flow, "exitMonitor", exit);
        SetRef(flow, "achievements", achievements);
        SetRef(achievements, "robotPuzzle", FindAll<VerifySong>().FirstOrDefault());
        SetRef(timer, "gameFlow", flow);
        BuildTimerHud(FindInScene("[BuildingBlock] Camera Rig"), timer);

        // End menu: grow to 1600 x 1100 and add the orange achievements list.
        MenuPanel end = new SerializedObject(flow).FindProperty("endMenu").objectReferenceValue as MenuPanel;
        if (end != null)
        {
            var rt = (RectTransform)end.transform;
            rt.sizeDelta = new Vector2(1600f, 1100f);
            float h = 1100f;
            void Place(string child, float y)
            {
                var c = end.transform.Find(child) as RectTransform;
                if (c != null) c.anchoredPosition = new Vector2(c.anchoredPosition.x, y);
            }
            Place("Title", h * 0.5f - 120f);
            Place("Subtitle", h * 0.5f - 240f);
            Place("PrimaryButton", -h * 0.5f + 150f);
            Place("SecondaryButton", -h * 0.5f + 150f);

            Transform oldList = end.transform.Find("Achievements");
            if (oldList != null) Undo.DestroyObjectImmediate(oldList.gameObject);
            TMP_Text list = MakeText(end.transform, "Achievements", "", 52f, Vector2.zero,
                new Vector2(1400f, 380f), new Color32(255, 165, 0, 255));
            list.gameObject.SetActive(false);
            end.BindAchievements(list);
            EditorUtility.SetDirty(end);
        }
        else Debug.LogWarning("[EscapeRoomSetup] GameFlow.endMenu not set; achievements list skipped.");

        foreach (PointerTool tool in FindAll<PointerTool>())
        {
            if (new SerializedObject(tool).FindProperty("mode").enumValueIndex == (int)PointerTool.Mode.Raycast)
                AddRayFlash(tool.gameObject, tool);
        }

        Finish("Timer, ray lines and achievements added. Review the scene, then save it (Ctrl+S).");
    }

    /// <summary>Batch-mode entry point: -executeMethod EscapeRoomSetup.AddTimerRayAchievementsBatch</summary>
    public static void AddTimerRayAchievementsBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/EscapeRoom.unity", OpenSceneMode.Single);
        AddTimerRayAchievements();
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("[EscapeRoomSetup] Scene saved to " + _scene.path);
    }

    [MenuItem("Tools/Escape Room/Steps/Rebuild Cuttable Walls")]
    public static void RebuildWallsOnly()
    {
        if (!Prepare()) return;
        EnsureLayers();
        WallBreachTracker tracker = BuildWalls();
        GameObject outsideTeleport = FindInScene("OutsideTeleport");
        SetupTeleportSurfaces(tracker, outsideTeleport);
        GameFlow flow = Object.FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);
        if (flow != null)
        {
            SetRef(flow, "walls", tracker);
            ExitMonitor exit = flow.GetComponent<ExitMonitor>();
            if (exit != null) SetRef(exit, "walls", tracker);
        }
        Finish("Cuttable walls rebuilt.");
    }

    // ------------------------------------------------------------------ helpers

    private static bool Prepare()
    {
        _scene = SceneManager.GetActiveScene();
        if (_scene.name != "EscapeRoom" &&
            !EditorUtility.DisplayDialog("Escape Room Setup",
                $"The active scene is '{_scene.name}', not 'EscapeRoom'. Continue anyway?", "Continue", "Cancel"))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(_scene.path))
        {
            if (_scene.isDirty) EditorSceneManager.SaveScene(_scene);
            string backupDir = "Assets/Scenes/Backups";
            Directory.CreateDirectory(backupDir);
            string backup = $"{backupDir}/{_scene.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.unity";
            AssetDatabase.CopyAsset(_scene.path, backup);
            Debug.Log($"[EscapeRoomSetup] Backup written to {backup}");
        }

        Directory.CreateDirectory(GeneratedDir);
        return true;
    }

    private static void Finish(string message)
    {
        EditorSceneManager.MarkSceneDirty(_scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[EscapeRoomSetup] " + message);
    }

    private static IEnumerable<Transform> AllTransforms()
    {
        foreach (GameObject root in _scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) yield return t;
        }
    }

    private static GameObject FindInScene(string name)
    {
        Transform t = AllTransforms().FirstOrDefault(x => x.name == name);
        return t != null ? t.gameObject : null;
    }

    private static T[] FindAll<T>() where T : Component
    {
        return AllTransforms().SelectMany(t => t.GetComponents<T>()).ToArray();
    }

    private static GameObject Require(string name)
    {
        GameObject go = FindInScene(name);
        if (go == null) Debug.LogWarning($"[EscapeRoomSetup] '{name}' not found in the scene; step skipped.");
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static Transform GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name).transform;
            Undo.RegisterCreatedObjectUndo(child.gameObject, name);
            child.SetParent(parent, false);
        }
        return child;
    }

    private static void SetRef(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning($"[EscapeRoomSetup] {target.GetType().Name}.{property} not found"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string property, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning($"[EscapeRoomSetup] {target.GetType().Name}.{property} not found"); return; }
        p.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string property, int value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning($"[EscapeRoomSetup] {target.GetType().Name}.{property} not found"); return; }
        if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
        else p.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string property, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning($"[EscapeRoomSetup] {target.GetType().Name}.{property} not found"); return; }
        p.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void OrLayerMask(Object target, string property, int layer)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning($"[EscapeRoomSetup] {target.GetType().Name}.{property} not found"); return; }
        p.intValue |= 1 << layer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool HasPersistent(UnityEventBase evt, Object target, string method)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
        {
            if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == method) return true;
        }
        return false;
    }

    private static void AddVoid(Object owner, UnityEvent evt, UnityAction action)
    {
        if (HasPersistent(evt, action.Target as Object, action.Method.Name)) return;
        UnityEventTools.AddVoidPersistentListener(evt, action);
        EditorUtility.SetDirty(owner);
    }

    private static void AddBool(Object owner, UnityEvent evt, UnityAction<bool> action, bool argument)
    {
        if (HasPersistent(evt, action.Target as Object, action.Method.Name)) return;
        UnityEventTools.AddBoolPersistentListener(evt, action, argument);
        EditorUtility.SetDirty(owner);
    }

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>Box collider on <paramref name="go"/> that fits all child renderers.</summary>
    private static BoxCollider FitBoxCollider(GameObject go)
    {
        Bounds world = WorldBounds(go);
        BoxCollider box = GetOrAdd<BoxCollider>(go);
        Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
        Vector3 c = world.center, e = world.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 local = go.transform.InverseTransformPoint(corner);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }
        box.center = (min + max) * 0.5f;
        box.size = max - min;
        return box;
    }

    /// <summary>Interaction SDK grab (controllers + hands), same setup as the Building Block cube.</summary>
    private static Rigidbody AddGrab(GameObject go)
    {
        Rigidbody rb = GetOrAdd<Rigidbody>(go);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Grabbable grabbable = GetOrAdd<Grabbable>(go);
        SetRef(grabbable, "_rigidbody", rb);

        GameObject child = GetOrCreateChild(go.transform, "GrabInteraction").gameObject;
        GrabInteractable grab = GetOrAdd<GrabInteractable>(child);
        SetRef(grab, "_rigidbody", rb);
        SetRef(grab, "_pointableElement", grabbable);

        HandGrabInteractable handGrab = GetOrAdd<HandGrabInteractable>(child);
        SetRef(handGrab, "_rigidbody", rb);
        SetRef(handGrab, "_pointableElement", grabbable);
        return rb;
    }

    /// <summary>Adds a PointerTool and a "Tip" child at the positive end of the mesh's longest axis.</summary>
    private static PointerTool AddPointerTool(GameObject go, PointerTool.Mode mode, int targetLayer, bool createTip)
    {
        PointerTool tool = GetOrAdd<PointerTool>(go);
        SetInt(tool, "mode", (int)mode);

        if (createTip)
        {
            MeshFilter mf = go.GetComponentInChildren<MeshFilter>(true);
            Transform tip = GetOrCreateChild(go.transform, "Tip");
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                Vector3 size = Vector3.Scale(b.size, mf.transform.lossyScale);
                Vector3 axis = size.x >= size.y && size.x >= size.z ? Vector3.right
                    : size.y >= size.z ? Vector3.up : Vector3.forward;
                Vector3 meshLocalTip = b.center + Vector3.Scale(axis, b.extents);
                tip.position = mf.transform.TransformPoint(meshLocalTip);
                tip.rotation = Quaternion.LookRotation(mf.transform.TransformDirection(axis), Vector3.up);
            }
            SetRef(tool, "tip", tip);
        }

        if (targetLayer >= 0) SetInt(tool, "targetMask", 1 << targetLayer);
        SetInt(tool, "occluderMask", (1 << 0) | (1 << WallLayer));

        if (mode == PointerTool.Mode.Raycast) AddRayFlash(go, tool);
        return tool;
    }

    /// <summary>0.2 s white line shown each time the tool's ray is cast.</summary>
    private static void AddRayFlash(GameObject go, PointerTool tool)
    {
        {
            Transform flashT = GetOrCreateChild(go.transform, "RayFlash");
            LineRenderer line = GetOrAdd<LineRenderer>(flashT.gameObject);
            line.sharedMaterial = CreateUnlitMaterial("RayFlash", Color.white);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.003f;
            line.startColor = line.endColor = Color.white;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            RayFlash flash = GetOrAdd<RayFlash>(flashT.gameObject);
            SetRef(tool, "rayFlash", flash);
        }
    }

    private static Material CreateLitMaterial(string name, Color color, Texture texture = null, Vector2? tiling = null)
    {
        string path = $"{GeneratedDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.1f);
        if (texture != null)
        {
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        string path = $"{GeneratedDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Copy of the default TMP font material that draws on top of everything (for the HUD).</summary>
    private static Material CreateOverlayFontMaterial(string name)
    {
        string path = $"{GeneratedDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        TMP_FontAsset font = Font();
        if (mat == null && font != null)
        {
            mat = new Material(font.material);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat == null) return null;
        Shader overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (overlay != null) mat.shader = overlay;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------ steps

    private static void EnsureLayers()
    {
        Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (tagManager == null) return;
        var so = new SerializedObject(tagManager);
        SerializedProperty layers = so.FindProperty("layers");
        void Ensure(int index, string name)
        {
            SerializedProperty p = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = name;
            else if (p.stringValue != name)
                Debug.LogWarning($"[EscapeRoomSetup] Layer {index} is '{p.stringValue}', expected '{name}'. Scripts use index {index}.");
        }
        Ensure(OutdoorLayer, "Outdoor");
        Ensure(LampLayer, "Lamp");
        Ensure(RobotLayer, "Robot");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FixRigAndLocomotion()
    {
        GameObject rig = Require("[BuildingBlock] Camera Rig");
        if (rig != null)
        {
            // Duplicate of the SDK capsule on Locomotor > PlayerController.
            UnityEngine.CharacterController stray = rig.GetComponent<UnityEngine.CharacterController>();
            if (stray != null) Undo.DestroyObjectImmediate(stray);

            foreach (Camera cam in rig.GetComponentsInChildren<Camera>(true))
            {
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 150f;
                EditorUtility.SetDirty(cam);
            }
        }

        foreach (FirstPersonLocomotor locomotor in FindAll<FirstPersonLocomotor>())
            SetFloat(locomotor, "_jumpForce", 0f); // no jumping

        foreach (Oculus.Interaction.Locomotion.CharacterController cc in FindAll<Oculus.Interaction.Locomotion.CharacterController>())
        {
            OrLayerMask(cc, "_layerMask", WallLayer);
            OrLayerMask(cc, "_layerMask", OutdoorLayer); // the outdoor ground
        }

        foreach (WallPenetrationTunneling wpt in FindAll<WallPenetrationTunneling>())
            OrLayerMask(wpt, "_layerMask", WallLayer);

        // Teleport only (also forced at runtime by LocomotionDefaults) and comfort vignettes off.
        foreach (MovingSetting moving in FindAll<MovingSetting>())
            SetInt(moving, "_controllerMovement._value", (int)MovingSetting.MovementStyle.Teleport);
        foreach (ComfortVignetteSetting comfort in FindAll<ComfortVignetteSetting>())
            SetInt(comfort, "_comfortLevel._value", (int)ComfortVignetteSetting.ComfortAssistance.Off);
    }

    private static void ReplacePlaneColliderWithBox(GameObject plane, bool topAtSurface)
    {
        foreach (MeshCollider mc in plane.GetComponents<MeshCollider>()) Undo.DestroyObjectImmediate(mc);
        BoxCollider box = GetOrAdd<BoxCollider>(plane);
        float sy = Mathf.Max(0.0001f, Mathf.Abs(plane.transform.lossyScale.y));
        float thickness = 0.2f / sy;
        box.size = new Vector3(10f, thickness, 10f); // Unity Plane mesh is 10 x 10
        box.center = new Vector3(0f, topAtSurface ? -thickness * 0.5f : thickness * 0.5f, 0f);
    }

    private static void FixRoomGeometry()
    {
        GameObject floor = Require("Floor");
        if (floor != null)
        {
            Undo.RecordObject(floor.transform, "Floor");
            floor.transform.localScale = new Vector3(1.5f, floor.transform.localScale.y, 1.5f); // 15 x 15 m, inside only
            ReplacePlaneColliderWithBox(floor, true);
        }

        GameObject roof = Require("Roof");
        if (roof != null) ReplacePlaneColliderWithBox(roof, true); // rotated 180 deg: local -y is world up

        GameObject table = Require("Chess Table");
        if (table != null)
        {
            Undo.RecordObject(table.transform, "Chess Table");
            Vector3 p = table.transform.position;
            table.transform.position = new Vector3(p.x, 0.458f, p.z); // top at the board frame's underside, no z-fighting
        }

        GameObject board = Require("Board");
        if (board != null)
        {
            MeshCollider mc = board.GetComponent<MeshCollider>();
            if (mc != null) { mc.convex = false; EditorUtility.SetDirty(mc); } // pieces rest on the squares
        }
    }

    private static void SetupLightPuzzle()
    {
        GameObject lights = Require("Lights");
        GameObject numbers = Require("Numbers");
        if (lights == null) return;

        RevealNumbersWhenDark puzzle = lights.GetComponent<RevealNumbersWhenDark>();
        foreach (Light light in lights.GetComponentsInChildren<Light>(true))
        {
            GameObject lamp = light.gameObject;
            lamp.layer = LampLayer;
            FitBoxCollider(lamp);
            ToggleableLight toggle = GetOrAdd<ToggleableLight>(lamp);
            SetRef(toggle, "targetLight", light);
            if (puzzle != null) SetRef(toggle, "puzzle", puzzle);
        }

        if (numbers != null)
        {
            NumberGlow glow = numbers.GetComponent<NumberGlow>();
            if (glow != null)
            {
                Renderer[] renderers = numbers.GetComponentsInChildren<Renderer>(true);
                var so = new SerializedObject(glow);
                SerializedProperty arr = so.FindProperty("numberRenderers");
                arr.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                if (puzzle != null) SetRef(puzzle, "numberGlow", glow);
            }
        }
    }

    private static void SetupWand()
    {
        GameObject wand = Require("Wand");
        if (wand == null) return;
        AddGrab(wand);
        AddPointerTool(wand, PointerTool.Mode.Raycast, LampLayer, true);
    }

    private static void SetupSafe()
    {
        GameObject safe = Require("Cyber_Box");
        GameObject keypadGo = Require("Keypad");
        if (safe == null || keypadGo == null) return;

        // Keypad on the safe's side that faces the room centre.
        Bounds box = WorldBounds(safe);
        Vector3 toCenter = Vector3.zero - box.center;
        toCenter.y = 0f;
        Vector3 normal = Mathf.Abs(toCenter.x) > Mathf.Abs(toCenter.z)
            ? new Vector3(Mathf.Sign(toCenter.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(toCenter.z));

        Transform keypad = keypadGo.transform;
        Undo.RecordObject(keypad, "Move keypad");
        // The keypad's button face is its local -Z (it was at yaw 180 facing +Z on Wall3).
        keypad.rotation = Quaternion.LookRotation(-normal, Vector3.up);

        Bounds kb = WorldBounds(keypadGo);
        float faceHeight = box.size.y;
        float faceWidth = Mathf.Abs(normal.x) > 0 ? box.size.z : box.size.x;
        float fit = Mathf.Min(1f, 0.8f * faceHeight / Mathf.Max(0.001f, kb.size.y), 0.8f * faceWidth / Mathf.Max(0.001f, Mathf.Max(kb.size.x, kb.size.z)));
        if (fit < 1f) keypad.localScale *= fit;

        kb = WorldBounds(keypadGo);
        float depthAlongNormal = Mathf.Abs(Vector3.Dot(kb.size, normal));
        Vector3 faceCenter = box.center + Vector3.Scale(box.extents, normal);
        Vector3 desiredCenter = faceCenter + normal * (depthAlongNormal * 0.5f + 0.005f);
        keypad.position += desiredCenter - kb.center;

        // Poke input on every key.
        foreach (NavKeypad.KeypadButton button in keypadGo.GetComponentsInChildren<NavKeypad.KeypadButton>(true))
        {
            ProximityPokeButton poke = GetOrAdd<ProximityPokeButton>(button.gameObject);
            BoxCollider col = button.GetComponent<BoxCollider>();
            Vector3 center = col != null ? col.center : Vector3.zero;
            float radius = 0.012f;
            if (col != null)
            {
                Vector3 e = Vector3.Scale(col.size * 0.5f, button.transform.lossyScale);
                radius = Mathf.Clamp(Mathf.Max(Mathf.Abs(e.x), Mathf.Abs(e.y)), 0.008f, 0.03f);
            }
            poke.Configure(center, radius, radius * 2f);
            AddVoid(poke, poke.OnPressed, button.PressButton);
            EditorUtility.SetDirty(poke);
        }

        // The safe's top disappears when the code is accepted. In Cyber_Box.fbx the top mesh is named "Cap".
        Transform top = safe.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t != safe.transform && t.name.ToLowerInvariant() == "top")
            ?? safe.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t != safe.transform && t.name.ToLowerInvariant() == "cap");
        NavKeypad.Keypad keypadLogic = keypadGo.GetComponentInChildren<NavKeypad.Keypad>(true);
        if (top == null)
            Debug.LogWarning("[EscapeRoomSetup] Cyber_Box child 'top' (or 'Cap') not found; the safe will not open visually.");
        else if (keypadLogic == null)
            Debug.LogWarning("[EscapeRoomSetup] Keypad component not found under the keypad; the safe will not open visually.");
        else
        {
            AddBool(keypadLogic, keypadLogic.OnAccessGranted, top.gameObject.SetActive, false);
            Debug.Log("[EscapeRoomSetup] Safe top '" + top.name + "' hides on the correct code.");
        }

        // Knight waits on top of the safe's body.
        GameObject knight = FindKnight();
        if (knight != null)
        {
            Undo.RecordObject(knight.transform, "Knight spawn");
            knight.transform.position = new Vector3(box.center.x, box.max.y + 0.02f, box.center.z);
        }
    }

    private static GameObject FindKnight()
    {
        WhiteKnight k = FindAll<WhiteKnight>().FirstOrDefault();
        return k != null ? k.gameObject : null;
    }

    private static void SetupKnight()
    {
        GameObject knight = FindKnight();
        if (knight == null) { Debug.LogWarning("[EscapeRoomSetup] WhiteKnight not found."); return; }
        AddGrab(knight);
        WhiteKnight script = knight.GetComponent<WhiteKnight>();
        SetFloat(script, "targetX", C7.x);
        SetFloat(script, "targetY", C7.y);
        SetFloat(script, "targetZ", C7.z);
    }

    private static void SetupRobotsAndBaton()
    {
        foreach (RobotSing robot in FindAll<RobotSing>())
        {
            robot.gameObject.layer = RobotLayer;
            FitBoxCollider(robot.gameObject);
            AudioSource audio = robot.GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                EditorUtility.SetDirty(audio);
            }
        }

        GameObject baton = Require("Baton");
        if (baton == null) return;
        AddGrab(baton);
        AddPointerTool(baton, PointerTool.Mode.Raycast, RobotLayer, true);
    }

    private static void SetupLightsaber()
    {
        BladeCollapse blade = FindAll<BladeCollapse>().FirstOrDefault();
        if (blade == null) { Debug.LogWarning("[EscapeRoomSetup] Lightsaber not found."); return; }

        Rigidbody existing = blade.GetComponentInParent<Rigidbody>(true);
        GameObject saber = existing != null ? existing.gameObject : blade.transform.root.gameObject;
        AddGrab(saber);

        if (blade.blade != null)
        {
            foreach (Collider c in blade.blade.GetComponentsInChildren<Collider>(true))
            {
                c.isTrigger = true; // cuts walls, never shoves objects
                EditorUtility.SetDirty(c);
            }
        }

        PointerTool tool = AddPointerTool(saber, PointerTool.Mode.Activate, -1, false);
        AddVoid(tool, tool.OnFired, blade.Toggle);

        // Hand tracking: a hilt button at the pommel end, pressed with the free hand.
        CapsuleCollider hilt = saber.GetComponentsInChildren<CapsuleCollider>(true)
            .FirstOrDefault(c => blade.blade == null || !c.transform.IsChildOf(blade.blade.transform));
        if (hilt != null)
        {
            Vector3 axis = hilt.direction == 0 ? Vector3.right : hilt.direction == 1 ? Vector3.up : Vector3.forward;
            float half = Mathf.Max(0f, hilt.height * 0.5f - hilt.radius);
            Vector3 a = hilt.transform.TransformPoint(hilt.center + axis * half);
            Vector3 b = hilt.transform.TransformPoint(hilt.center - axis * half);
            Vector3 bladeBase = blade.transform.position;
            Vector3 pommel = Vector3.Distance(a, bladeBase) > Vector3.Distance(b, bladeBase) ? a : b;

            Transform buttonT = GetOrCreateChild(saber.transform, "HiltButton");
            buttonT.position = pommel;
            ProximityPokeButton button = GetOrAdd<ProximityPokeButton>(buttonT.gameObject);
            button.Configure(Vector3.zero, 0.02f, 0.04f);
            SetRef(button, "requireHeld", tool);
            AddVoid(button, button.OnPressed, blade.Toggle);
        }
    }

    private static WallBreachTracker BuildWalls()
    {
        GameObject old = FindInScene("CuttableWalls");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var rootGo = new GameObject("CuttableWalls");
        Undo.RegisterCreatedObjectUndo(rootGo, "CuttableWalls");
        WallBreachTracker tracker = rootGo.AddComponent<WallBreachTracker>();

        // Things mounted on walls keep uncuttable tiles behind them.
        var mounted = new List<Bounds>();
        GameObject lights = FindInScene("Lights");
        if (lights != null)
            foreach (Light l in lights.GetComponentsInChildren<Light>(true)) mounted.Add(WorldBounds(l.gameObject));
        GameObject numbers = FindInScene("Numbers");
        if (numbers != null)
            foreach (Transform digit in numbers.transform) mounted.Add(WorldBounds(digit.gameObject));

        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        for (int w = 1; w <= 4; w++)
        {
            GameObject wall = FindInScene("Wall" + w);
            if (wall == null) continue;
            Transform t = wall.transform;
            Material material = wall.GetComponent<MeshRenderer>() != null ? wall.GetComponent<MeshRenderer>().sharedMaterial : null;

            Vector3 axisA = t.right * (10f * t.lossyScale.x);
            Vector3 axisB = t.forward * (10f * t.lossyScale.z);
            Vector3 vertical = Mathf.Abs(axisA.normalized.y) > Mathf.Abs(axisB.normalized.y) ? axisA : axisB;
            Vector3 horizontal = vertical == axisA ? axisB : axisA;
            float height = vertical.magnitude;
            float length = horizontal.magnitude;
            Vector3 up = Vector3.up;
            Vector3 inward = t.up; // the plane's visible side faces into the room
            Vector3 along = Vector3.Cross(up, inward).normalized;

            var wallGo = new GameObject("CuttableWall" + w);
            wallGo.transform.SetParent(rootGo.transform, false);
            WallTilesSurface surface = wallGo.AddComponent<WallTilesSurface>();
            TeleportInteractable blocker = wallGo.AddComponent<TeleportInteractable>();
            SetBool(blocker, "_allowTeleport", false);
            SetRef(blocker, "_surface", surface);

            int cols = Mathf.RoundToInt(length / TileSize);
            int rows = Mathf.RoundToInt(height / TileSize);
            Vector3 center = t.position;
            Quaternion rotation = Quaternion.LookRotation(inward, up);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = center
                        + along * (-length * 0.5f + (c + 0.5f) * TileSize)
                        + up * (-height * 0.5f + (r + 0.5f) * TileSize)
                        - inward * (TileThickness * 0.5f);

                    var tile = new GameObject($"Tile_{r:00}_{c:00}");
                    tile.layer = WallLayer;
                    tile.transform.SetParent(wallGo.transform, false);
                    tile.transform.SetPositionAndRotation(pos, rotation);
                    tile.transform.localScale = new Vector3(TileSize, TileSize, TileThickness);
                    tile.AddComponent<MeshFilter>().sharedMesh = cube;
                    MeshRenderer mr = tile.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = material;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    tile.AddComponent<BoxCollider>();

                    var tileBounds = new Bounds(pos, new Vector3(TileSize, TileSize, TileSize) + Vector3.one * 0.1f);
                    bool holdsSomething = mounted.Any(m => m.Intersects(tileBounds));
                    tile.AddComponent<WallTile>().Configure(tracker, w, !holdsSomething);
                }
            }

            Undo.RecordObject(wall, "Hide plane wall");
            wall.SetActive(false); // replaced by tiles
        }

        return tracker;
    }

    private static Texture2D CreateGrassTexture()
    {
        string path = $"{GeneratedDir}/Grass.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(256, 256, TextureFormat.RGB24, true);
            var rng = new System.Random(1977);
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float n = (float)rng.NextDouble();
                float blades = Mathf.PerlinNoise(x * 0.08f, y * 0.35f);
                Color c = Color.Lerp(new Color(0.20f, 0.36f, 0.12f), new Color(0.38f, 0.55f, 0.20f), 0.6f * blades + 0.4f * n);
                tex.SetPixel(x, y, c);
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>Grass ground, horizon hills, static sunrise sky. Returns the (inactive) outdoor teleport surface.</summary>
    private static GameObject SetupOutdoor()
    {
        GameObject old = FindInScene("Outdoor");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var outdoor = new GameObject("Outdoor");
        Undo.RegisterCreatedObjectUndo(outdoor, "Outdoor");
        outdoor.layer = OutdoorLayer;

        // Ground: 100 x 100 m, top 2 cm under the room floor so the two never z-fight.
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "OutsideGround";
        ground.layer = OutdoorLayer;
        ground.transform.SetParent(outdoor.transform, false);
        ground.transform.position = new Vector3(0f, -0.02f, 0f);
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        Object.DestroyImmediate(ground.GetComponent<MeshCollider>());
        BoxCollider groundBox = ground.AddComponent<BoxCollider>();
        groundBox.size = new Vector3(10f, 0.2f, 10f);
        groundBox.center = new Vector3(0f, -0.1f, 0f);
        ground.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial("Grass", Color.white, CreateGrassTexture(), new Vector2(40f, 40f));

        var outsideTeleport = new GameObject("OutsideTeleport");
        outsideTeleport.transform.SetParent(outdoor.transform, false);
        ColliderSurface groundSurface = outsideTeleport.AddComponent<ColliderSurface>();
        SetRef(groundSurface, "_collider", groundBox);
        TeleportInteractable groundTeleport = outsideTeleport.AddComponent<TeleportInteractable>();
        SetRef(groundTeleport, "_surface", groundSurface);
        outsideTeleport.SetActive(false); // enabled by WallBreachTracker on the first cut tile

        // Horizon hills (no colliders).
        Material hillMat = CreateLitMaterial("Hills", new Color(0.18f, 0.30f, 0.14f));
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = "Hill" + i;
            hill.layer = OutdoorLayer;
            hill.transform.SetParent(outdoor.transform, false);
            float radius = 52f + (i % 3) * 4f;
            hill.transform.position = new Vector3(Mathf.Cos(angle) * radius, -2f, Mathf.Sin(angle) * radius);
            hill.transform.localScale = new Vector3(26f, 8f + (i % 4) * 2f, 26f);
            Object.DestroyImmediate(hill.GetComponent<Collider>());
            MeshRenderer hr = hill.GetComponent<MeshRenderer>();
            hr.sharedMaterial = hillMat;
            hr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Sunrise, visible from the start: low warm sun beyond Wall4 (+Z), lighting only the outdoor layer.
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(outdoor.transform, false);
        sunGo.transform.rotation = Quaternion.Euler(8f, 180f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.6f, 0.35f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.None;
        sun.cullingMask = 1 << OutdoorLayer;

        string skyPath = $"{GeneratedDir}/SunriseSky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, skyPath);
        }
        sky.SetFloat("_SunSize", 0.08f);
        sky.SetFloat("_SunSizeConvergence", 4f);
        sky.SetFloat("_AtmosphereThickness", 1.6f);
        sky.SetColor("_SkyTint", new Color(0.75f, 0.55f, 0.5f));
        sky.SetColor("_GroundColor", new Color(0.3f, 0.32f, 0.25f));
        sky.SetFloat("_Exposure", 1.1f);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        // Minimum light for the dark-room puzzle: flat ambient, not the sky.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.11f, 0.11f, 0.11f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.85f, 0.70f, 0.60f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 90f;

        return outsideTeleport;
    }

    private static void SetupTeleportSurfaces(WallBreachTracker tracker, GameObject outsideTeleport)
    {
        // Inside floor: collider surface instead of the NavMesh baked from the old 30 x 30 m plane.
        GameObject floor = FindInScene("Floor");
        TeleportInteractable insideTeleport = FindAll<TeleportInteractable>()
            .FirstOrDefault(t => t.gameObject.name == "ISDK_TeleportInteraction");
        if (floor != null && insideTeleport != null)
        {
            ColliderSurface floorSurface = GetOrAdd<ColliderSurface>(floor);
            SetRef(floorSurface, "_collider", floor.GetComponent<BoxCollider>());
            SetRef(insideTeleport, "_surface", floorSurface);
            foreach (Oculus.Interaction.Surfaces.NavMeshSurface nav in insideTeleport.GetComponents<Oculus.Interaction.Surfaces.NavMeshSurface>())
            {
                nav.enabled = false;
                EditorUtility.SetDirty(nav);
            }
        }

        // Roof blocks arcs over the walls.
        GameObject roof = FindInScene("Roof");
        if (roof != null)
        {
            ColliderSurface roofSurface = GetOrAdd<ColliderSurface>(roof);
            SetRef(roofSurface, "_collider", roof.GetComponent<BoxCollider>());
            TeleportInteractable roofBlocker = GetOrAdd<TeleportInteractable>(roof);
            SetBool(roofBlocker, "_allowTeleport", false);
            SetRef(roofBlocker, "_surface", roofSurface);
        }

        if (tracker != null && outsideTeleport != null)
            AddBool(tracker, tracker.OnFirstBreach, outsideTeleport.SetActive, true);
    }

    private static TMP_FontAsset Font()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    private static TMP_Text MakeText(Transform parent, string name, string text, float size, Vector2 anchoredPos, Vector2 box, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = anchoredPos;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = Font();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text MakeButton(Transform parent, string name, string label, Vector2 pos, MenuPanel panel, bool primary)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(440f, 130f);
        rt.anchoredPosition = pos;
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);

        TMP_Text text = MakeText(go.transform, "Label", label, 60f, Vector2.zero, rt.sizeDelta, new Color(0.1f, 0.1f, 0.1f));

        // Ray-selectable with the rig's controller and hand rays.
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(440f, 130f, 10f);
        ColliderSurface surface = go.AddComponent<ColliderSurface>();
        SetRef(surface, "_collider", col);
        RayInteractable ray = go.AddComponent<RayInteractable>();
        SetRef(ray, "_surface", surface);
        InteractableUnityEventWrapper events = go.AddComponent<InteractableUnityEventWrapper>();
        SetRef(events, "_interactableView", ray);
        if (primary) AddVoid(events, events.WhenSelect, panel.PressPrimary);
        else AddVoid(events, events.WhenSelect, panel.PressSecondary);
        return text;
    }

    private static MenuPanel BuildPanel(Transform parent, string name, string title, string primary, string secondary,
        Vector2 size, RenderTexture feed, MenuOrbitCamera orbit, Vector3 position, Quaternion rotation)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = Vector3.one * 0.001f; // 1 px = 1 mm
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)go.transform).sizeDelta = size;
        MenuPanel panel = go.AddComponent<MenuPanel>();

        if (feed != null)
        {
            var feedGo = new GameObject("CameraFeed", typeof(RectTransform));
            feedGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)feedGo.transform);
            feedGo.AddComponent<RawImage>().texture = feed;
        }

        var filter = new GameObject("GrayFilter", typeof(RectTransform));
        filter.transform.SetParent(go.transform, false);
        Stretch((RectTransform)filter.transform);
        filter.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, feed != null ? 0.5f : 0.85f);

        float h = size.y;
        TMP_Text titleText = MakeText(go.transform, "Title", title, 110f, new Vector2(0f, h * 0.5f - 120f), new Vector2(size.x - 80f, 160f), Color.white);
        TMP_Text subtitle = MakeText(go.transform, "Subtitle", "", 52f, new Vector2(0f, h * 0.5f - 240f), new Vector2(size.x - 80f, 90f), Color.white);
        subtitle.gameObject.SetActive(false);
        float buttonY = -h * 0.5f + 150f;
        TMP_Text p = MakeButton(go.transform, "PrimaryButton", primary, new Vector2(-260f, buttonY), panel, true);
        TMP_Text s = MakeButton(go.transform, "SecondaryButton", secondary, new Vector2(260f, buttonY), panel, false);

        panel.Bind(titleText, subtitle, p, s, orbit);
        EditorUtility.SetDirty(panel);
        return panel;
    }

    private static MenuOrbitCamera MakeOrbitCamera(Transform parent, string name, RenderTexture rt, Vector3 center, float radius, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Camera cam = go.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 150f;
        cam.enabled = false;
        MenuOrbitCamera orbit = go.AddComponent<MenuOrbitCamera>();
        orbit.Configure(center, radius, height);
        go.transform.position = center + new Vector3(radius, height, 0f);
        go.transform.LookAt(center);
        EditorUtility.SetDirty(orbit);
        return orbit;
    }

    private static void BuildLobby(Transform parent)
    {
        Material dark = CreateLitMaterial("Lobby", new Color(0.12f, 0.12f, 0.13f));
        var lobby = new GameObject("Lobby");
        lobby.transform.SetParent(parent, false);
        lobby.transform.position = LobbyCenter;

        void Box(string name, Vector3 localPos, Vector3 scale)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(lobby.transform, false);
            b.transform.localPosition = localPos;
            b.transform.localScale = scale;
            b.GetComponent<MeshRenderer>().sharedMaterial = dark;
        }
        Box("LobbyFloor", new Vector3(0f, -0.1f, 0f), new Vector3(6f, 0.2f, 6f));
        Box("LobbyRoof", new Vector3(0f, 3.1f, 0f), new Vector3(6f, 0.2f, 6f));
        Box("LobbyWallN", new Vector3(0f, 1.5f, 3.1f), new Vector3(6f, 3f, 0.2f));
        Box("LobbyWallS", new Vector3(0f, 1.5f, -3.1f), new Vector3(6f, 3f, 0.2f));
        Box("LobbyWallE", new Vector3(3.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 6f));
        Box("LobbyWallW", new Vector3(-3.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 6f));

        var lightGo = new GameObject("LobbyLight");
        lightGo.transform.SetParent(lobby.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 8f;
        l.intensity = 1.5f;
    }

    private static void SetupMenusAndFlow(WallBreachTracker tracker)
    {
        GameObject old = FindInScene("GameFlow");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var flowGo = new GameObject("GameFlow");
        Undo.RegisterCreatedObjectUndo(flowGo, "GameFlow");
        flowGo.AddComponent<LocomotionDefaults>();
        GameFlow flow = flowGo.AddComponent<GameFlow>();
        ExitMonitor exit = flowGo.AddComponent<ExitMonitor>();
        GameTimer timer = flowGo.AddComponent<GameTimer>();
        AchievementTracker achievements = flowGo.AddComponent<AchievementTracker>();

        BuildLobby(flowGo.transform);

        string rtPath = $"{GeneratedDir}/MenuOrbit.renderTexture";
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(1280, 720, 24);
            AssetDatabase.CreateAsset(rt, rtPath);
        }

        MenuOrbitCamera startOrbit = MakeOrbitCamera(flowGo.transform, "StartOrbitCamera", rt, new Vector3(0f, 1f, 0f), 5f, 3f);
        MenuOrbitCamera endOrbit = MakeOrbitCamera(flowGo.transform, "EndOrbitCamera", rt, new Vector3(0f, 1f, 0f), 18f, 4f);

        // Player waits in the lobby facing a fixed panel.
        Vector3 spawn = LobbyCenter + new Vector3(0f, 0f, -1.5f);
        GameObject rig = FindInScene("[BuildingBlock] Camera Rig");
        if (rig != null)
        {
            Undo.RecordObject(rig.transform, "Rig to lobby");
            rig.transform.SetPositionAndRotation(new Vector3(spawn.x, rig.transform.position.y, spawn.z), Quaternion.identity);
        }

        MenuPanel start = BuildPanel(flowGo.transform, "StartMenu", "Title pending", "Play", "Quit",
            new Vector2(1600f, 900f), rt, startOrbit, spawn + new Vector3(0f, 1.5f, 2f), Quaternion.identity);
        MenuPanel end = BuildPanel(flowGo.transform, "EndMenu", "You Win", "Play Again", "Quit",
            new Vector2(1600f, 1100f), rt, endOrbit, Vector3.zero, Quaternion.identity);
        TMP_Text achievementText = MakeText(end.transform, "Achievements", "", 52f, new Vector2(0f, 0f),
            new Vector2(1400f, 380f), new Color32(255, 165, 0, 255));
        achievementText.gameObject.SetActive(false);
        end.BindAchievements(achievementText);
        EditorUtility.SetDirty(end);
        MenuPanel leave = BuildPanel(flowGo.transform, "LeavePrompt", "Leave the room?", "Leave", "Keep exploring",
            new Vector2(1100f, 520f), null, null, Vector3.zero, Quaternion.identity);
        end.gameObject.SetActive(false);
        leave.gameObject.SetActive(false);

        var roomStart = new GameObject("RoomStart");
        roomStart.transform.SetParent(flowGo.transform, false);
        roomStart.transform.SetPositionAndRotation(RoomStartPosition, Quaternion.identity);

        SetRef(flow, "startMenu", start);
        SetRef(flow, "endMenu", end);
        SetRef(flow, "roomStart", roomStart.transform);
        SetRef(flow, "walls", tracker);
        SetRef(flow, "exitMonitor", exit);
        SetRef(flow, "achievements", achievements);
        SetRef(achievements, "robotPuzzle", FindAll<VerifySong>().FirstOrDefault());
        SetRef(timer, "gameFlow", flow);
        BuildTimerHud(rig, timer);
        SetRef(exit, "walls", tracker);
        SetRef(exit, "leavePrompt", leave);
        SetRef(exit, "gameFlow", flow);

        AddVoid(start, start.OnPrimary, flow.Play);
        AddVoid(start, start.OnSecondary, flow.Quit);
        AddVoid(end, end.OnPrimary, flow.PlayAgain);
        AddVoid(end, end.OnSecondary, flow.Quit);
        AddVoid(leave, leave.OnPrimary, exit.Leave);
        AddVoid(leave, leave.OnSecondary, exit.KeepExploring);
    }

    /// <summary>Head-locked countdown: world-space canvas under the CenterEyeAnchor, upper right of the view.</summary>
    private static void BuildTimerHud(GameObject rig, GameTimer timer)
    {
        Transform eye = rig != null ? rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CenterEyeAnchor") : null;
        if (eye == null) { Debug.LogWarning("[EscapeRoomSetup] CenterEyeAnchor not found; timer HUD skipped."); return; }

        Transform old = eye.Find("TimerHUD");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject("TimerHUD", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "TimerHUD");
        go.transform.SetParent(eye, false);
        Vector3 offset = new Vector3(0.30f, 0.20f, 1.0f); // ~17 deg right, ~11 deg up, 1 m out
        go.transform.localPosition = offset;
        go.transform.localRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
        go.transform.localScale = Vector3.one * 0.001f; // 1 px = 1 mm
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        ((RectTransform)go.transform).sizeDelta = new Vector2(300f, 100f);

        TMP_Text label = MakeText(go.transform, "Countdown", "10:00", 60f, Vector2.zero, new Vector2(300f, 100f), new Color32(255, 0, 0, 255));
        label.raycastTarget = false;
        Material overlay = CreateOverlayFontMaterial("TimerOverlay");
        if (overlay != null) label.fontSharedMaterial = overlay;

        SetRef(timer, "hud", go);
        SetRef(timer, "label", label);
        go.SetActive(false);
    }

    private static void ProjectSettingsFixes()
    {
        // Quest 3: 4x MSAA, no HDR.
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (urp != null)
        {
            urp.msaaSampleCount = 4;
            urp.supportsHDR = false;
            EditorUtility.SetDirty(urp);
        }

        // Build list: the old entry points at a SampleScene that does not exist.
        if (!string.IsNullOrEmpty(_scene.path))
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(_scene.path, true) };
        }
    }
}
