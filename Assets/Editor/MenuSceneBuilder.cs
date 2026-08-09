using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ana menü sahnesini tek tıkla kurar. Çıkan Canvas gerçek bir Unity UI
/// hiyerarşisidir; kurulduktan sonra Editor'den serbestçe biçimlendirilebilir.
/// </summary>
public static class MenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("GelistirJam/Ana Menü Sahnesini Kur")]
    public static void BuildMenuScene()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Ana menü sahnesini kur",
            "Yeni bir sahne oluşturulup " + ScenePath + " olarak kaydedilecek " +
            "ve Build Settings'te ilk sıraya alınacak.\n\n" +
            "Açık sahnede kaydedilmemiş değişiklik varsa önce kaydet.",
            "Kur", "Vazgeç");

        if (!proceed)
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var defaultCamera = GameObject.Find("Main Camera");
        if (defaultCamera != null)
            Object.DestroyImmediate(defaultCamera);

        ApplyDesertAtmosphere();

        Transform pivot = BuildCameraRig();
        BuildDiorama();

        var controllerObject = new GameObject("MainMenu");
        var controller = controllerObject.AddComponent<MainMenuController>();

        BuildCanvas(controller, pivot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        PutMenuSceneFirst();

        Debug.Log("Ana menü sahnesi kuruldu: " + ScenePath);
    }

    /// <summary>Oyunla aynı sis ve ışık hissi; menü ile oyun kopuk durmasın.</summary>
    private static void ApplyDesertAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.79f, 0.71f, 0.54f);
        RenderSettings.fogStartDistance = 15f;
        RenderSettings.fogEndDistance = 70f;

        RenderSettings.ambientLight = new Color(0.55f, 0.5f, 0.42f);

        var light = Object.FindFirstObjectByType<Light>();
        if (light != null)
        {
            light.color = new Color(1f, 0.95f, 0.82f);
            light.transform.rotation = Quaternion.Euler(28f, 40f, 0f);
        }
    }

    /// <summary>
    /// Kamera bir pivotun child'ı olarak duruyor; MainMenuController pivotu
    /// döndürünce kamera manzarayı yavaşça tarar.
    /// </summary>
    private static Transform BuildCameraRig()
    {
        var pivotObject = new GameObject("CameraPivot");
        pivotObject.transform.position = new Vector3(0f, 1.5f, 0f);

        var cameraObject = new GameObject("MenuCamera");
        cameraObject.transform.SetParent(pivotObject.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.2f, -12f);
        cameraObject.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 55f;
        camera.farClipPlane = 120f;
        camera.backgroundColor = new Color(0.79f, 0.71f, 0.54f);
        cameraObject.AddComponent<AudioListener>();

        return pivotObject.transform;
    }

    /// <summary>
    /// Küçük bir diorama: birkaç kum tepesi ve kaya. Asıl haritayı kopyalamak
    /// WebGL açılışını gereksiz yere yavaşlatırdı.
    /// </summary>
    private static void BuildDiorama()
    {
        Material sand = CreateMaterial("MenuSand", new Color(0.76f, 0.68f, 0.5f));
        Material rock = CreateMaterial("MenuRock", new Color(0.45f, 0.41f, 0.35f));

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        ground.GetComponent<Renderer>().sharedMaterial = sand;

        // Sabit tohum: sahne her kurulduğunda aynı görünsün.
        Random.InitState(4242);

        for (int i = 0; i < 7; i++)
        {
            var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dune.name = "Dune" + i;
            dune.transform.position = new Vector3(
                Random.Range(-28f, 28f), Random.Range(-2.5f, -1.2f), Random.Range(-28f, 28f));
            dune.transform.localScale = new Vector3(
                Random.Range(12f, 22f), Random.Range(4f, 7f), Random.Range(12f, 22f));
            dune.GetComponent<Renderer>().sharedMaterial = sand;
            Object.DestroyImmediate(dune.GetComponent<Collider>());
        }

        for (int i = 0; i < 9; i++)
        {
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Rock" + i;
            stone.transform.position = new Vector3(
                Random.Range(-20f, 20f), Random.Range(0f, 0.4f), Random.Range(-20f, 20f));
            stone.transform.rotation = Random.rotation;
            stone.transform.localScale = Vector3.one * Random.Range(0.6f, 2.2f);
            stone.GetComponent<Renderer>().sharedMaterial = rock;
            Object.DestroyImmediate(stone.GetComponent<Collider>());
        }
    }

    private static void BuildCanvas(MainMenuController controller, Transform pivot)
    {
        var canvasObject = new GameObject("MenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Butonların tıklanabilmesi için sahnede bir EventSystem şart.
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        CreateText(canvasObject.transform, "Title", "TIGER", 110, new Vector2(0f, 250f),
            new Color(0.93f, 0.88f, 0.72f));

        CreateText(canvasObject.transform, "Subtitle", "Kuzey Afrika, 1942", 34,
            new Vector2(0f, 170f), new Color(0.78f, 0.73f, 0.6f));

        Button start = CreateButton(canvasObject.transform, "StartButton", "BAŞLA", new Vector2(0f, -20f));
        Button quit = CreateButton(canvasObject.transform, "QuitButton", "ÇIKIŞ", new Vector2(0f, -110f));

        CreateText(canvasObject.transform, "Hint",
            "W A S D sür · M harita · R yeniden başla", 26,
            new Vector2(0f, -260f), new Color(0.7f, 0.66f, 0.56f));

        SetField(controller, "startButton", start);
        SetField(controller, "quitButton", quit);
        SetField(controller, "cameraPivot", pivot);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 70f);
        rect.anchoredPosition = position;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.15f, 0.12f, 0.85f);

        var text = CreateText(buttonObject.transform, "Label", label, 34, Vector2.zero,
            new Color(0.93f, 0.88f, 0.72f));
        text.rectTransform.sizeDelta = rect.sizeDelta;

        return buttonObject.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string name, string content, int size,
                                   Vector2 position, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1200f, 140f);
        rect.anchoredPosition = position;

        var text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;

        return text;
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

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SetField(Object target, string fieldName, Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(fieldName);

        if (property == null)
        {
            Debug.LogWarning($"MenuSceneBuilder: '{fieldName}' alanı bulunamadı.");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Menü ilk sırada olmalı; build açıldığında bu sahne yüklenir.</summary>
    private static void PutMenuSceneFirst()
    {
        var existing = EditorBuildSettings.scenes;
        var ordered = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        foreach (var entry in existing)
        {
            if (entry.path != ScenePath)
                ordered.Add(entry);
        }

        bool hasGameScene = ordered.Exists(s => s.path == GameScenePath);
        if (!hasGameScene && System.IO.File.Exists(GameScenePath))
            ordered.Add(new EditorBuildSettingsScene(GameScenePath, true));

        EditorBuildSettings.scenes = ordered.ToArray();
    }
}
