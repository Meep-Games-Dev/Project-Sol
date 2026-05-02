using UnityEngine;
using System.Collections.Generic;
public enum ResourceType
        {
            O2,
            Ore,
            H2O,
            Carbon,
            Food,
            Metals,
            People,
            Credits,
            PowerCells
        }
public class Station
{
    public Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>()
        {   
            {ResourceType.O2, 0}, //oxygen 
            {ResourceType.Ore, 0}, //ore -> metals 
            {ResourceType.H2O, 0}, //water
            {ResourceType.Carbon, 0}, //carbon
            {ResourceType.Food, 0}, //food
            {ResourceType.Metals, 0}, // Refined Metal
            {ResourceType.People, 50}, // your people 
            {ResourceType.Credits, 0}, // Credits, affected in ResorceMiner and Hanger/trade modules
            {ResourceType.PowerCells, 5} // power, 1 solar panel array gives + 10 power, each module gives -1 power and you can transmit power to other stations
        };
    public List<GameObject> pieces = new List<GameObject>();
}
public interface IActivable
{
    void Activation();
    void upgrade();
    public string type {get; set;}

}

