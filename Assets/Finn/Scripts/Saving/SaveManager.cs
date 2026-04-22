using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public SolarSystemManager systemManager;
    public CameraMovement cameraMovement;
    public SaveManager saveManager;
    public RVOManager AIManager;
    bool loadSave = false;
    int saveToLoad = 0;

    private void Awake()
    {

        if (SceneManager.GetActiveScene().name == "SolarSystemTest")
        {
            systemManager = FindFirstObjectByType<SolarSystemManager>();
            cameraMovement = FindFirstObjectByType<CameraMovement>();
            AIManager = FindFirstObjectByType<RVOManager>();
        }
        var managers = Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None);

        if (managers.Length > 1)
        {

            foreach (var m in managers)
            {
                if (m != this)
                {
                    Destroy(m.gameObject);
                }
            }
        }
        DontDestroyOnLoad(this);
    }
    public void Save(Save saveData, int Save)
    {
        string jsonData = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(Application.persistentDataPath + $"/Save{Save}.json", jsonData);
        Debug.Log($"Saved data to {Application.persistentDataPath + $"/Save{Save}.json"}");
    }
    public void LoadFromTitle()
    {
        loadSave = true;
        saveToLoad = 0;
        SceneManager.LoadScene("SolarSystemTest");

    }
    public void NewGame()
    {
        loadSave = false;
        saveToLoad = 0;
        SceneManager.LoadScene("SolarSystemTest");
    }
    public void Load(int Save)
    {
        string data = File.ReadAllText(Application.persistentDataPath + $"/Save{Save}.json");
        Save readableSave = JsonUtility.FromJson<Save>(data);
        SaveableSolarSystem solarSystem = readableSave.solarSystem;
        Debug.Log(readableSave.AIs.AIs.Count);
        AIManager = FindFirstObjectByType<RVOManager>();
        AIManager.LoadAIs(readableSave.AIs);
        systemManager.Load(solarSystem);
        cameraMovement.transform.position = readableSave.lastCamPos;
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        systemManager = FindFirstObjectByType<SolarSystemManager>();
        cameraMovement = FindFirstObjectByType<CameraMovement>();
        AIManager = FindFirstObjectByType<RVOManager>();
        if (scene.name == "SolarSystemTest" && loadSave)
        {
            Load(saveToLoad);
            Debug.Log("Loading Scene");
            //loadSave = false;
        }
        else if (scene.name == "SolarSystemTest" && !loadSave)
        {
            systemManager.Generate(3500, 10, Vector2.zero);
            AIManager.SpawnAIs();
        }
    }
}
