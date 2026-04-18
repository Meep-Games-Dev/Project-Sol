using UnityEngine;

public class Inspectable : MonoBehaviour
{
    public InspectableTypes type;
    public string title;
    [TextArea(5, 10)]
    public string description;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        if (type == InspectableTypes.Planet)
        {
            title = GetComponent<Planet>().planetName;
            description = GetComponent<Planet>().planetDescription;
        }
        else if (type == InspectableTypes.Enemy)
        {
            title = "Enemy Craft";
            description = "A spacecraft from one of your enemies.";
        }
    }
}
