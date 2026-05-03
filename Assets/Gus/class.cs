using UnityEngine;
using System.Collections.Generic;
public class Station
{
    public Dictionary<Resources, int> resources = new Dictionary<Resources, int>()
        {   
            {Resources.Ore, 0}, //ore -> metals 
            {Resources.H2O, 0}, //water
            {Resources.Carbon, 0}, //carbon
            {Resources.Food, 0}, //food
            {Resources.RefinedMetal, 0}, // Refined Metal
            {Resources.Population, 50}, // your people 
            {Resources.Credits, 0}, // Credits, affected in ResorceMiner and Hanger/trade modules
            {Resources.EnergyCells, 5} // power, 1 solar panel array gives + 10 power, each module gives -1 power and you can transmit power to other stations
        };
    public List<GameObject> pieces = new List<GameObject>();
}
public interface IActivable
{
    void Activation();
    void upgrade();
    public string type {get; set;}

}

