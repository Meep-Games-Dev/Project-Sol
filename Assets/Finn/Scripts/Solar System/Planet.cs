using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Planet : MonoBehaviour
{

    public string planetName;
    public string planetDescription;
    public PlanetType planetType;
    public Resources planetResourceAbundance;
    public string readablePlanetType;
    public string readablePlanetResourceAbundance;
    public List<Resource> planetResources = new List<Resource>();
    private static readonly string[] prefixes = { "Astro", "Zenth", "Kryl", "Xen", "Velt", "Omni", "Quar", "Myn", "Gly", "Alder", "Star" };
    private static readonly string[] middles = { "o", "ara", "on", "i", "u", "vadi", "etor", "ili", "oi", "in", "of", "ik", "iti" };
    private readonly System.Random rnd = new();
    public float rotationalSpeed;

    public float localRotationalSpeed;

    public Color planetColor;
    public Color atmosphereColor;
    public Color cloudColor;
    public SphereCollider colliderP;
    public float min;
    public float max;
    public Vector2 surfaceOffset;

    public float cloudCover;

    public float size;

    public Faction homePlanetOf;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        rotationalSpeed = UnityEngine.Random.Range(0.01f, 0.2f);
        localRotationalSpeed = UnityEngine.Random.Range(0.1f, 0.5f);
        colliderP = GetComponentInChildren<SphereCollider>();
        string part1 = prefixes[rnd.Next(prefixes.Length)];

        string part2 = middles[rnd.Next(middles.Length)];

        planetName = part1 + part2;

        if (rnd.Next(100) < 30)
        {
            planetName += " " + RandUtils.RandomGreekLetter();
        }

        Array possibleResources = Enum.GetValues(typeof(Resources));

        planetResourceAbundance = (Resources)possibleResources.GetValue(UnityEngine.Random.Range(0, possibleResources.Length));

        Array possiblePlanetTypes = Enum.GetValues(typeof(PlanetType));

        for (int i = 0; i < possibleResources.Length; i++)
        {
            Resource resource = new Resource();
            resource.type = (Resources)possibleResources.GetValue(i);
            if (resource.type == planetResourceAbundance)
            {
                resource.amount = rnd.Next(100, 150);
            }
            else
            {
                if (rnd.Next(0, 100) > 40)
                {
                    resource.amount = rnd.Next(0, 50);
                }

            }
            planetResources.Add(resource);
        }

        planetType = (PlanetType)possiblePlanetTypes.GetValue(UnityEngine.Random.Range(0, possiblePlanetTypes.Length));


        while (planetType == PlanetType.Enemy || planetType == PlanetType.Allied)
        {
            planetType = (PlanetType)possiblePlanetTypes.GetValue(UnityEngine.Random.Range(0, possiblePlanetTypes.Length));
        }
        readablePlanetResourceAbundance = StringUtils.Nicify(planetResourceAbundance.ToString().ToLower());
        readablePlanetType = StringUtils.Nicify(planetType.ToString());

        if (planetType == PlanetType.IndependentPeaceful)
        {
            string[] descriptionPart0 =
            {
                "are welcome to visitors",
                "would like to be ruled",
                "welcome outsiders",
                "are looking for a ruler"
            };
            planetDescription =
                $"The Empire once heavily used {planetName} for {StringUtils.Nicify(planetResourceAbundance.ToString()).ToLower()}, but its use is long past." +
                $" {planetName} is now ruled independently by the people who were once part of the empire." +
                $" Now you can find an excess of {StringUtils.Nicify(planetResourceAbundance.ToString()).ToLower()} here." +
                $" The people of {planetName} {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]}";
        }
        else if (planetType == PlanetType.IndependentMilitary)
        {
            string[] descriptionPart0 =
{
                "guard it with their lives",
                "are hostile to anyone who comes near",
                "despise outsiders",
                "prey on passing ships"
            };
            planetDescription =
                $"The Empire once heavily used {planetName} for {StringUtils.Nicify(planetResourceAbundance.ToString()).ToLower()}, but its use is long past." +
                $" This planet is now ruled independently by the people who were once part of the empire." +
                $" Now you can find an excess of {StringUtils.Nicify(planetResourceAbundance.ToString()).ToLower()} here." +
                $" The people of this planet {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]}";
        }
        else if (planetType == PlanetType.LivableUninhabited)
        {
            string[] description =
            {
                $"The Empire once used {planetName}, but long ago abandoned it for one reason or another. Now it sits, drifting in space, waiting for someone to claim it.",
                $"Nobody has ever set foot on {planetName}. It has been unknown to the rest of the universe for a long time. Now you can claim it and call it home."
            };
            planetDescription = description[rnd.Next(0, description.Length)];
        }
        else if (planetType == PlanetType.NotLivable)
        {
            string[] descriptionPart0 =
            {
                "once was a",
                "used to be a",
                "at one point was a"
            };
            string[] descriptionPart1 =
            {
                "bustling metropolis"
                , "large mining world"
                , "trade hub"
                , "military base"
                , "research world"
            };
            string[] descriptionPart2 =
            {
                "Thick clouds darken the sky",
                "Lava pours from crevices in the planet",
                "Fire scorches the ground",
                "Strange aliens walk the planet"
            };
            string[] descriptionPart3 =
            {
                "an experiment gone wrong",
                "an asteroid impact",
                "a mining disaster",
                "a war",
                "a bomb",
                "a meltdown"
            };

            planetDescription = $"{planetName} {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]} {descriptionPart1[rnd.Next(0, descriptionPart1.Length)]}, but now it is completely unlivable. " +
                $"{descriptionPart2[rnd.Next(0, descriptionPart2.Length)]}, a result from {descriptionPart3[rnd.Next(0, descriptionPart3.Length)]} forcing the Empire to abandon {planetName}. " +
                $"Now it is completely uninhabitable with no chance to be restored anytime soon.";
        }
        if (homePlanetOf != Faction.None)
        {
            planetDescription = $"{planetName} is the home planet of the {StringUtils.Nicify(homePlanetOf.ToString())} .\n It's most abundant resource is {StringUtils.Nicify(planetResourceAbundance.ToString()).ToLower()}";
            if (homePlanetOf == Faction.Freindly)
            {
                planetType = PlanetType.Allied;
            }
            else if (homePlanetOf == Faction.Enemy)
            {
                planetType = PlanetType.Enemy;
            }
        }

    }
    private void Update()
    {
        transform.Rotate(rotationalSpeed * Time.deltaTime * Vector3.right);
    }

}
