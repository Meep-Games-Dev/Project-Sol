using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;


public class PauseManager : MonoBehaviour
{
    public PlayerInput PlayerInput;
    private InputAction pause;
    public GameObject pauseMenu;
    public SolarSystemManager solarSystemManager;
    public CameraMovement cameraMovement;
    public SaveManager saveManager;
    public RVOManager AIManager;
    public Volume postProcessing;
    public GameObject confirmationDialogueQuit;
    public GameObject confirmationDialogueQuitToTitle;
    public Inspector inspector;
    bool paused = false;

    private void Awake()
    {
        PlayerInput = new PlayerInput();
        pause = PlayerInput.Main.Pause;
    }
    private void Start()
    {
        saveManager = FindFirstObjectByType<SaveManager>();
        AIManager = FindFirstObjectByType<RVOManager>();
    }

    public void OnEnable()
    {

        pause.Enable();
    }
    public void OnDisable()
    {
        pause.Disable();
    }

    private void Update()
    {
        if (pause.WasPressedThisFrame())
        {
            if (paused)
            {
                UnPause();
            }
            else
            {
                Pause();
            }
            //paused = !paused;
        }
    }
    public void Pause()
    {
        Time.timeScale = 0;
        pauseMenu.SetActive(true);
        paused = true;
        DepthOfField depthComp;
        if (postProcessing.profile.TryGet<DepthOfField>(out depthComp))
        {
            depthComp.active = true;
        }
        inspector.HideInspector();
    }
    public void UnPause()
    {
        Time.timeScale = 1;
        pauseMenu.SetActive(false);
        paused = false;
        DepthOfField depthComp;
        if (postProcessing.profile.TryGet<DepthOfField>(out depthComp))
        {
            depthComp.active = false;
        }
        inspector.ShowInspector();
    }
    public void ReturnToTitle()
    {
        SceneManager.LoadScene("TitleScreen");
    }
    public void AttemptExit()
    {
        confirmationDialogueQuit.SetActive(true);
    }
    public void CancelExit()
    {
        confirmationDialogueQuit.SetActive(false);
    }
    public void AttemptExitToTitle()
    {
        confirmationDialogueQuitToTitle.SetActive(true);
    }
    public void CancelExitToTitle()
    {
        confirmationDialogueQuitToTitle.SetActive(false);
    }
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SaveAll()
    {
        Save save = new Save();
        SaveableSolarSystem solarSystem = new SaveableSolarSystem();
        solarSystem.planets = new List<SaveablePlanet>();
        for (int i = 0; i < solarSystemManager.planetComponentList.Count; i++)
        {
            SaveablePlanet planet = new SaveablePlanet
            {
                speed = solarSystemManager.planetComponentList[i].rotationalSpeed,
                description = solarSystemManager.planetComponentList[i].planetDescription,
                lastPos = solarSystemManager.planetComponentList[i].gameObject.transform.position,
                name = solarSystemManager.planetComponentList[i].planetName,
                resourceAbundance = solarSystemManager.planetComponentList[i].planetResourceAbundance,
                resources = solarSystemManager.planetComponentList[i].planetResources,
                type = solarSystemManager.planetComponentList[i].planetType,
                atmosphereColor = solarSystemManager.planetComponentList[i].planetColor,
                size = solarSystemManager.planetComponentList[i].size,
                surfaceOffset = solarSystemManager.planetComponentList[i].surfaceOffset,
                cloudColor = solarSystemManager.planetComponentList[i].cloudColor,
                cloudCover = solarSystemManager.planetComponentList[i].cloudCover,
                max = solarSystemManager.planetComponentList[i].max,
                min = solarSystemManager.planetComponentList[i].min,
                planetColor = solarSystemManager.planetComponentList[i].planetColor
            };
            solarSystem.planets.Add(planet);
        }
        save.AlliedAIs = AIManager.SaveAllied();
        save.solarSystem = solarSystem;
        save.lastCamPos = cameraMovement.transform.position;
        saveManager.Save(save, 0);
    }
}
