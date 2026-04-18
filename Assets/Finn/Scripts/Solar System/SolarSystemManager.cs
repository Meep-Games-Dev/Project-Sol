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
    public int asteroidBeltSize = 100;
    public int asteroidBeltDistance = 100000;
    public Material orbitMat;
    public int resolution;
    public Camera cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Generate(size, 5, new Vector2(0, 0));
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
        for (int i = 0; i < planetsList.Count; i++)
        {
            Planet planet = planetComponentList[i];
            planetPivots[i].transform.Rotate(Vector3.forward, planet.rotationalSpeed * Time.deltaTime);
        }
    }

    public void Generate(int size, int planets, Vector2 centerPosition)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
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
            ln.widthMultiplier = 1.5f;
            ln.material = orbitMat;
            ln.alignment = LineAlignment.TransformZ;

            instantiatedPlanet.transform.localScale = new Vector3(planetSize, planetSize, planetSize);
            Material planetMat = instantiatedPlanet.GetComponent<MeshRenderer>().material;
            planetMat.SetColor("_PlanetSurfaceMain", planetColor);
            planetMat.SetFloat("_Min", Random.Range(-0.1f, 0.5f));
            planetMat.SetFloat("_Max", Random.Range(1f, 1.3f));
            planetMat.SetVector("_SurfaceOffset", new Vector3(Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f)));

            GameObject instantiatedAtmos = instantiatedPlanet.transform.GetChild(0).gameObject;
            //instantiatedAtmos.transform.localScale = new Vector3(planetSize * planet2atmos, planetSize * planet2atmos, planetSize * planet2atmos);
            Material atmosMat = instantiatedAtmos.GetComponent<MeshRenderer>().material;
            atmosMat.SetColor("_AtmosphereColor", atmosphereColor);
            atmosMat.SetColor("_CloudColor", cloudColor);
            atmosMat.SetFloat("_CloudCover", Random.Range(0f, 0.8f));
            instantiatedPlanet.AddComponent<Planet>();

            planetComponentList.Add(instantiatedPlanet.GetComponent<Planet>());
            planetsList.Add(instantiatedPlanet);

        }

        //for (int i = 0; i < 100000; i++)
        //{
        //    float distanceFromSun = Random.Range(500, size);
        //    Vector2 edgePoint = Random.onUnitCircle * size;
        //    Vector3 position = new Vector3(edgePoint.x + centerPosition.x + Random.Range(-5, 5), edgePoint.y + centerPosition.y + Random.Range(-5, 5), Random.Range(-10, 10));
        //    GameObject asteroid = Instantiate(asteroidPrefab, position, Quaternion.identity);
        //    Material planetMat = asteroid.GetComponent<MeshRenderer>().material;
        //    planetMat.SetVector("_SurfaceOffset", new Vector3(Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f)));
        //    asteroidList.Add(asteroid);
        //}

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
    }


}
