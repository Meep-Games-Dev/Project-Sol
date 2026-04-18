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
    public List<Resource> planetResources;
    private static readonly string[] prefixes = { "Astro", "Zenth", "Kryl", "Xen", "Velt", "Omni", "Quar", "Myn", "Gly", "Alder" };
    private static readonly string[] middles = { "o", "ara", "on", "i", "u", "vadi", "etor", "ili", "oi" };
    private static readonly string[] designators = { "Alpha", "Beta", "Gamma", "Prime" };
    private readonly System.Random rnd = new();
    public float rotationalSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationalSpeed = UnityEngine.Random.Range(0.5f, 4f);
        string part1 = prefixes[rnd.Next(prefixes.Length)];

        string part2 = middles[rnd.Next(middles.Length)];

        planetName = part1 + part2;

        if (rnd.Next(100) < 30)
        {
            planetName += " " + designators[rnd.Next(designators.Length)];
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
                resource.amount = rnd.Next(0, 50);
            }
        }

        planetType = (PlanetType)possiblePlanetTypes.GetValue(UnityEngine.Random.Range(0, possiblePlanetTypes.Length));


        while (planetType == PlanetType.Enemy || planetType == PlanetType.Allied)
        {
            planetType = (PlanetType)possiblePlanetTypes.GetValue(UnityEngine.Random.Range(0, possiblePlanetTypes.Length));
        }
        readablePlanetResourceAbundance = StringUtils.Nicify(planetResourceAbundance.ToString());
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
                $"The Empire once heavily used this world for {StringUtils.Nicify(planetResourceAbundance.ToString())}, but its use is long past." +
                $" This planet is now ruled independently by the people who were once part of the empire." +
                $" Now you can find an excess of {StringUtils.Nicify(planetResourceAbundance.ToString())} here." +
                $" The people of this planet {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]}";
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
                $"The Empire once heavily used this world for {StringUtils.Nicify(planetResourceAbundance.ToString())}, but its use is long past." +
                $" This planet is now ruled independently by the people who were once part of the empire." +
                $" Now you can find an excess of {StringUtils.Nicify(planetResourceAbundance.ToString())} here." +
                $" The people of this planet {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]}";
        }
        else if (planetType == PlanetType.LivableUninhabited)
        {
            string[] description =
            {
                "The Empire once used this world, but long ago abandoned it for one reason or another. Now it sits, drifting in space, waiting for someone to claim it.",
                "Nobody has ever set foot on this world. It has been unknown to the rest of the universe for a long time. Now you can claim it and call it home."
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

            planetDescription = $"This planet {descriptionPart0[rnd.Next(0, descriptionPart0.Length)]} {descriptionPart1[rnd.Next(0, descriptionPart1.Length)]}, but now it is completely unlivable." +
                $"{descriptionPart2[rnd.Next(0, descriptionPart2.Length)]}, a result from {descriptionPart3[rnd.Next(0, descriptionPart3.Length)]} forcing the Empire to abandon this planet." +
                $"Now it is completely uninhabitable with no chance to be restored anytime soon.";
        }

    }

}
