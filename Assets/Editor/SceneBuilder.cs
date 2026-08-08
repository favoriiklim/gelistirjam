using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Test sahnesini tek tıkla kurar. Elle kurulum uzun sürüyor ve referans
/// bağlamayı unutmak sessiz hatalara yol açıyor; sahne kaybolursa da
/// buradan saniyeler içinde geri gelir.
/// </summary>
public static class SceneBuilder
{
    private const string ObstacleLayerName = "Obstacle";
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("GelistirJam/Test Sahnesini Kur")]
    public static void BuildTestScene()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Test sahnesini kur",
            "Yeni bir sahne oluşturulup " + ScenePath + " olarak kaydedilecek.\n\n" +
            "Açık sahnede kaydedilmemiş değişiklik varsa önce kaydet.",
            "Kur", "Vazgeç");

        if (!proceed)
            return;

        int obstacleLayer = EnsureLayer(ObstacleLayerName);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Varsayılan kamerayı siliyoruz; vizör kamerası onun yerini alacak.
        var defaultCamera = GameObject.Find("Main Camera");
        if (defaultCamera != null)
            Object.DestroyImmediate(defaultCamera);

        Material sandMaterial = CreateMaterial("Sand", new Color(0.76f, 0.68f, 0.5f));
        Material tankMaterial = CreateMaterial("TankTemp", new Color(0.42f, 0.40f, 0.32f));
        Material enemyMaterial = CreateMaterial("EnemyTemp", new Color(0.55f, 0.22f, 0.18f));

        BuildGround(obstacleLayer, sandMaterial);
        GameObject tank = BuildTank(tankMaterial);
        BuildEnemy(obstacleLayer, enemyMaterial);
        GameObject goal = BuildGoalZone();
        BuildGameManager();
        BuildUI(goal);

        Selection.activeGameObject = tank;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        Debug.Log("Test sahnesi kuruldu: " + ScenePath);
    }

    private static void BuildGround(int obstacleLayer, Material material)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);

        // Zemin ve engeller Obstacle katmanında olmalı, yoksa görüş hattı kesilmez.
        ground.layer = obstacleLayer;
        ground.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject BuildTank(Material material)
    {
        // Kök ölçeksiz kalmalı: ölçeklenmiş bir parent, child'ların
        // localPosition değerlerini de çarpar ve kamera metrelerce kayar.
        var tank = new GameObject("Tank");
        tank.transform.position = new Vector3(0f, 0.75f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(tank.transform, false);
        body.transform.localScale = new Vector3(3f, 1.5f, 6f);
        body.GetComponent<Renderer>().sharedMaterial = material;

        // Çarpışma kökteki collider ile çözülüyor; ikinci collider gereksiz.
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());

        var collider = tank.AddComponent<BoxCollider>();
        collider.size = new Vector3(3f, 1.5f, 6f);

        var body_rb = tank.AddComponent<Rigidbody>();
        body_rb.mass = 1000f;

        var aimPoint = new GameObject("AimPoint");
        aimPoint.transform.SetParent(tank.transform, false);
        aimPoint.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        var cameraObject = new GameObject("VisorCamera");
        cameraObject.transform.SetParent(tank.transform, false);
        cameraObject.transform.localPosition = new Vector3(-0.7f, 0.4f, 3.2f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        // Sisin arkasını çizmenin anlamı yok; WebGL'de bedava performans.
        camera.farClipPlane = 120f;
        cameraObject.AddComponent<AudioListener>();

        var tankController = tank.AddComponent<TankController>();
        tank.AddComponent<StationManager>();
        var inputRouter = tank.AddComponent<InputRouter>();
        var playerTarget = tank.AddComponent<PlayerTarget>();
        var visorCamera = cameraObject.AddComponent<VisorCamera>();

        SetObjectField(inputRouter, "stationManager", tank.GetComponent<StationManager>());
        SetObjectField(inputRouter, "tankController", tankController);
        SetObjectField(playerTarget, "aimPoint", aimPoint.transform);
        SetObjectField(playerTarget, "tankController", tankController);
        SetObjectField(visorCamera, "tankController", tankController);

        return tank;
    }

    private static void BuildEnemy(int obstacleLayer, Material material)
    {
        var enemy = new GameObject("EnemyTank");
        enemy.transform.position = new Vector3(0f, 0.7f, 40f);
        // Oyuncunun başlangıç noktasına baksın.
        enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(enemy.transform, false);
        body.transform.localScale = new Vector3(2.5f, 1.4f, 5f);
        body.GetComponent<Renderer>().sharedMaterial = material;

        var eye = new GameObject("Eye");
        eye.transform.SetParent(enemy.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.9f, 2.5f);

        var spotter = enemy.AddComponent<EnemySpotter>();
        SetObjectField(spotter, "eye", eye.transform);
        SetIntField(spotter, "obstacleMask", 1 << obstacleLayer);
    }

    private static GameObject BuildGoalZone()
    {
        var goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "GoalZone";
        goal.transform.position = new Vector3(0f, 2f, 90f);
        goal.transform.localScale = new Vector3(20f, 6f, 20f);

        // Bölge görünmesin; Scene view'da gizmo olarak zaten çiziliyor.
        goal.GetComponent<Renderer>().enabled = false;
        goal.GetComponent<BoxCollider>().isTrigger = true;
        goal.AddComponent<GoalZone>();

        return goal;
    }

    private static void BuildGameManager()
    {
        var manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
    }

    private static void BuildUI(GameObject goal)
    {
        var ui = new GameObject("VisorUI");
        ui.AddComponent<VisorMask>();
        ui.AddComponent<SpottingIndicator>();
        ui.AddComponent<GameOverUI>();

        var map = ui.AddComponent<MapUI>();
        SetObjectField(map, "goal", goal.transform);

        // Zemin 200x200 ve merkezde; harita sınırları buna göre.
        SetVector2Field(map, "worldOrigin", new Vector2(-100f, -100f));
        SetVector2Field(map, "worldSize", new Vector2(200f, 200f));
    }

    // --- Yardımcılar -------------------------------------------------------

    /// <summary>
    /// private [SerializeField] alanlar dışarıdan atanamaz; SerializedObject
    /// üzerinden yazmak Editor tarafında bunun doğru yoludur.
    /// </summary>
    private static SerializedProperty FindField(Object target, string fieldName, out SerializedObject serialized)
    {
        serialized = new SerializedObject(target);
        var property = serialized.FindProperty(fieldName);

        if (property == null)
            Debug.LogWarning($"SceneBuilder: '{target.GetType().Name}' içinde '{fieldName}' alanı bulunamadı.");

        return property;
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        var property = FindField(target, fieldName, out var serialized);
        if (property == null)
            return;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetIntField(Object target, string fieldName, int value)
    {
        var property = FindField(target, fieldName, out var serialized);
        if (property == null)
            return;

        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2Field(Object target, string fieldName, Vector2 value)
    {
        var property = FindField(target, fieldName, out var serialized);
        if (property == null)
            return;

        property.vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        const string folder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = $"{folder}/{materialName}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("SceneBuilder: URP Lit shader bulunamadı, varsayılan shader kullanılıyor.");
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        material.color = color;

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>Katman yoksa ilk boş yuvaya ekler ve indeksini döndürür.</summary>
    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1)
            return existing;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        var layers = tagManager.FindProperty("layers");

        // 0-7 arası Unity'nin yerleşik katmanları; kullanıcı katmanları 8'den başlar.
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue))
                continue;

            slot.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return i;
        }

        Debug.LogError($"SceneBuilder: boş katman yuvası kalmadı, '{layerName}' eklenemedi.");
        return 0;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var entry in scenes)
        {
            if (entry.path == ScenePath)
                return;
        }

        var updated = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updated, 0);
        updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
