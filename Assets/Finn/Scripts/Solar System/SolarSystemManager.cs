using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public class SolarSystemManager : MonoBehaviour
{
    public int size;
    public int planets;
    public Vector2 position;

    //public float planet2atmos = 3;

    public GameObject planetPrefab;


    public List<GameObject> planetsList = new List<GameObject>();
    public List<GameObject> planetPivots = new List<GameObject>();
    public List<Planet> planetComponentList = new List<Planet>();

    public List<GameObject> asteroidList = new List<GameObject>();

    private List<Matrix4x4[]> asteroidPositions = new List<Matrix4x4[]>();
    private MaterialPropertyBlock[] propBlocks;

    public Mesh asteroidMesh;
    public Material asteroidMaterial;
    public int totalAsteroids = 100000;
    public int asteroidBeltSize = 1;
    public int asteroidBeltDistance = 100000;
    public Material orbitMat;
    public int resolution;
    public Camera cam;
    public AlliedManager alliedManager;
    public EnemyManager enemyManager;
    public CameraMovement camMove;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Generate(size, 5, new Vector2(0, 0));

    }

    // Update is called once per frame
    void Update()
    {


        for (int i = 0; i < asteroidPositions.Count; i++)
        {
            Graphics.DrawMeshInstanced(asteroidMesh, 0, asteroidMaterial, asteroidPositions[i], asteroidPositions[i].Length, propBlocks[i]);
        }

    }
    private void LateUpdate()
    {
        //for (int i = 0; i < planetsList.Count; i++)
        //{
        //    Planet planet = planetComponentList[i];
        //    planet.currentAngle += planet.rotationalSpeed * Time.deltaTime;
        //    float x = Mathf.Cos(planet.currentAngle) * planet.orbitRadius;
        //    float y = Mathf.Sin(planet.currentAngle) * planet.orbitRadius;
        //    planetsList[i].transform.position = new Vector3(x, y, 0);
        //}
        if (LoadingState.AIFinishedLoading && LoadingState.planetsFinishedLoading)
        {
            for (int i = 0; i < planetsList.Count; i++)
            {
                Planet planet = planetComponentList[i];
                planetPivots[i].transform.Rotate(Vector3.forward, planet.rotationalSpeed * Time.deltaTime);
            }

        }

    }

    public void Generate(int size, int planets, Vector2 centerPosition)
    {
        Debug.Log("Generating solar system");
        alliedManager = FindFirstObjectByType<AlliedManager>();
        enemyManager = FindFirstObjectByType<EnemyManager>();
        camMove = FindFirstObjectByType<CameraMovement>();
        Random.InitState((int)System.DateTime.Now.Ticks);
        int enemyHome = Random.Range(0, planets);
        int alliedHome = Random.Range(0, planets);
        while (enemyHome == alliedHome)
        {
            alliedHome = Random.Range(0, planets);
        }

        for (int i = 0; i < planets; i++)
        {

            Color planetColor = Random.ColorHSV();
            Color atmosphereColor = Random.ColorHSV();
            Color cloudColor = Random.ColorHSV();

            float distanceFromSun = Random.Range(500, size);
            float planetSize = Random.Range(2f, 10f);

            float degrees = Random.Range(0, 360);
            Vector3 position = new Vector3(centerPosition.x + distanceFromSun * Mathf.Cos(degrees * Mathf.Deg2Rad), centerPosition.y + distanceFromSun * Mathf.Sin(degrees * Mathf.Deg2Rad), 0);
            GameObject planetPivot = Instantiate(new GameObject(), new Vector3(0, 0, 0), Quaternion.identity);
            planetPivots.Add(planetPivot);
            GameObject instantiatedPlanet = Instantiate(planetPrefab, planetPivot.transform);
            instantiatedPlanet.transform.localPosition = position;
            LineRenderer ln = instantiatedPlanet.AddComponent<LineRenderer>();

            ln.positionCount = Mathf.CeilToInt(distanceFromSun * resolution);

            for (int j = 0; j < ln.positionCount; j++)
            {
                float progress = (float)j / (ln.positionCount - 1);

                float radians = progress * 2 * Mathf.PI;

                Vector3 lineRendPos = new Vector3(
                    centerPosition.x + distanceFromSun * Mathf.Cos(radians),
                    centerPosition.y + distanceFromSun * Mathf.Sin(radians),
                    0
                );

                ln.SetPosition(j, lineRendPos);
            }
            ln.widthMultiplier = 0.5f;
            ln.material = orbitMat;
            ln.alignment = LineAlignment.TransformZ;

            instantiatedPlanet.transform.localScale = new Vector3(planetSize, planetSize, planetSize);
            Material planetMat = instantiatedPlanet.GetComponent<MeshRenderer>().material;

            float min = Random.Range(-0.1f, 0.5f);
            float max = Random.Range(1f, 1.3f);
            Vector3 surfaceOffset = new Vector3(Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f));

            planetMat.SetColor("_PlanetSurfaceMain", planetColor);
            planetMat.SetFloat("_Min", min);
            planetMat.SetFloat("_Max", max);
            planetMat.SetVector("_SurfaceOffset", surfaceOffset);

            GameObject instantiatedAtmos = instantiatedPlanet.transform.GetChild(0).gameObject;
            //instantiatedAtmos.transform.localScale = new Vector3(planetSize * planet2atmos, planetSize * planet2atmos, planetSize * planet2atmos);
            Material atmosMat = instantiatedAtmos.GetComponent<MeshRenderer>().material;
            float cloudCover = Random.Range(0f, 0.8f);
            atmosMat.SetColor("_AtmosphereColor", atmosphereColor);
            atmosMat.SetColor("_CloudColor", cloudColor);
            atmosMat.SetFloat("_CloudCover", cloudCover);
            instantiatedPlanet.AddComponent<Planet>();
            Planet planet = instantiatedPlanet.GetComponent<Planet>();

            planet.planetColor = planetColor;
            planet.atmosphereColor = atmosphereColor;
            planet.cloudColor = cloudColor;
            planet.min = min;
            planet.max = max;
            planet.surfaceOffset = surfaceOffset;
            planet.size = planetSize;
            planet.cloudCover = cloudCover;
            if (i == enemyHome)
            {
                enemyManager.homePlanet = planet;
                planet.homePlanetOf = Faction.Enemy;
            }
            else if (i == alliedHome)
            {
                alliedManager.homePlanet = planet;
                planet.homePlanetOf = Faction.Freindly;
                camMove.transform.position = planet.gameObject.transform.position;
            }
            else
            {
                planet.homePlanetOf = Faction.None;
            }

            planetComponentList.Add(instantiatedPlanet.GetComponent<Planet>());
            planetsList.Add(instantiatedPlanet);

        }
        camMove.planets = planetComponentList;
        //Each batch is a max of 1023 asteroids
        int totalFullBatches = totalAsteroids / 1023;

        int others = totalAsteroids % 1023;
        propBlocks = new MaterialPropertyBlock[totalFullBatches + 1];
        for (int i = 0; i <= totalFullBatches; i++)
        {
            int amount = (i == totalFullBatches) ? others : 1023;
            if (amount <= 0) break;

            propBlocks[i] = new MaterialPropertyBlock();
            Matrix4x4[] batchData = new Matrix4x4[amount];
            List<Vector4> asteroidSpinSpeeds = new List<Vector4>();
            List<Vector4> meshOffsets = new List<Vector4>();
            for (int j = 0; j < amount; j++)
            {
                Vector2 edgePoint = Random.onUnitCircle * asteroidBeltDistance;
                Vector3 position = new Vector3(edgePoint.x + centerPosition.x + Random.Range(-asteroidBeltSize, asteroidBeltSize), edgePoint.y + centerPosition.y + Random.Range(-asteroidBeltSize, asteroidBeltSize), Random.Range(-10, 10));
                Matrix4x4 asteroidPos = Matrix4x4.Translate(position);
                Vector3 asteroidSeedOffset = new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000));
                Vector3 asteroidSpinSpeed = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
                Matrix4x4 asteroidLocalPos = asteroidPos;
                batchData[j] = asteroidLocalPos;
                asteroidSpinSpeeds.Add(asteroidSpinSpeed);
                meshOffsets.Add(asteroidSeedOffset);
            }
            propBlocks[i].SetVectorArray("_SurfaceOffset", meshOffsets);
            propBlocks[i].SetVectorArray("_SpinOffset", asteroidSpinSpeeds);

            asteroidPositions.Add(batchData);
        }
        LoadingState.planetsFinishedLoading = true;
    }

    public void Load(SaveableSolarSystem system)
    {
        List<SaveablePlanet> importedPlanets = system.planets;
        Debug.Log(importedPlanets.Count);
        for (int i = 0; i < importedPlanets.Count; i++)
        {
            Debug.Log("Created planet");
            Color planetColor = importedPlanets[i].planetColor;
            Color atmosphereColor = importedPlanets[i].atmosphereColor;
            Color cloudColor = importedPlanets[i].cloudColor;

            float distanceFromSun = Vector2.Distance(importedPlanets[i].lastPos, new Vector2(0, 0));
            float planetSize = importedPlanets[i].size;

            Vector3 position = new Vector3(importedPlanets[i].lastPos.x, importedPlanets[i].lastPos.y, 0);
            GameObject planetPivot = Instantiate(new GameObject(), new Vector3(0, 0, 0), Quaternion.identity);
            planetPivots.Add(planetPivot);
            GameObject instantiatedPlanet = Instantiate(planetPrefab, planetPivot.transform);
            instantiatedPlanet.transform.localPosition = position;
            LineRenderer ln = instantiatedPlanet.AddComponent<LineRenderer>();

            ln.positionCount = Mathf.CeilToInt(distanceFromSun * resolution);

            for (int j = 0; j < ln.positionCount; j++)
            {
                float progress = (float)j / (ln.positionCount - 1);

                float radians = progress * 2 * Mathf.PI;

                Vector3 lineRendPos = new Vector3(
                    distanceFromSun * Mathf.Cos(radians),
                    distanceFromSun * Mathf.Sin(radians),
                    0
                );

                ln.SetPosition(j, lineRendPos);
            }
            ln.widthMultiplier = 0.5f;
            ln.material = orbitMat;
            ln.alignment = LineAlignment.TransformZ;

            instantiatedPlanet.transform.localScale = new Vector3(planetSize, planetSize, planetSize);
            Material planetMat = instantiatedPlanet.GetComponent<MeshRenderer>().material;

            float min = importedPlanets[i].min;
            float max = importedPlanets[i].max;
            Vector3 surfaceOffset = importedPlanets[i].surfaceOffset;

            planetMat.SetColor("_PlanetSurfaceMain", planetColor);
            planetMat.SetFloat("_Min", min);
            planetMat.SetFloat("_Max", max);
            planetMat.SetVector("_SurfaceOffset", surfaceOffset);

            GameObject instantiatedAtmos = instantiatedPlanet.transform.GetChild(0).gameObject;

            Material atmosMat = instantiatedAtmos.GetComponent<MeshRenderer>().material;
            float cloudCover = importedPlanets[i].cloudCover;
            atmosMat.SetColor("_AtmosphereColor", atmosphereColor);
            atmosMat.SetColor("_CloudColor", cloudColor);
            atmosMat.SetFloat("_CloudCover", cloudCover);
            instantiatedPlanet.AddComponent<Planet>();
            Planet planet = instantiatedPlanet.GetComponent<Planet>();

            planet.planetColor = planetColor;
            planet.atmosphereColor = atmosphereColor;
            planet.cloudColor = cloudColor;
            planet.min = min;
            planet.max = max;
            planet.surfaceOffset = surfaceOffset;
            planet.size = planetSize;
            planet.cloudCover = cloudCover;

            planetComponentList.Add(instantiatedPlanet.GetComponent<Planet>());
            planetsList.Add(instantiatedPlanet);

        }
        camMove.planets = planetComponentList;
        //Each batch is a max of 1023 asteroids
        int totalFullBatches = totalAsteroids / 1023;

        int others = totalAsteroids % 1023;
        propBlocks = new MaterialPropertyBlock[totalFullBatches + 1];
        for (int i = 0; i <= totalFullBatches; i++)
        {
            int amount = (i == totalFullBatches) ? others : 1023;
            if (amount <= 0) break;

            propBlocks[i] = new MaterialPropertyBlock();
            Matrix4x4[] batchData = new Matrix4x4[amount];
            List<Vector4> asteroidSpinSpeeds = new List<Vector4>();
            List<Vector4> meshOffsets = new List<Vector4>();
            for (int j = 0; j < amount; j++)
            {
                Vector2 edgePoint = Random.onUnitCircle * asteroidBeltDistance;
                Vector3 position = new Vector3(edgePoint.x + Random.Range(-asteroidBeltSize, asteroidBeltSize), edgePoint.y + Random.Range(-asteroidBeltSize, asteroidBeltSize), Random.Range(-10, 10));
                Matrix4x4 asteroidPos = Matrix4x4.Translate(position);
                Vector3 asteroidSeedOffset = new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000));
                Vector3 asteroidSpinSpeed = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
                Matrix4x4 asteroidLocalPos = asteroidPos;
                batchData[j] = asteroidLocalPos;
                asteroidSpinSpeeds.Add(asteroidSpinSpeed);
                meshOffsets.Add(asteroidSeedOffset);
            }
            propBlocks[i].SetVectorArray("_SurfaceOffset", meshOffsets);
            propBlocks[i].SetVectorArray("_SpinOffset", asteroidSpinSpeeds);

            asteroidPositions.Add(batchData);
        }
        LoadingState.planetsFinishedLoading = true;
    }


}
