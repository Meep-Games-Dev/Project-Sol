using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StationO;
public class main
{
    public TMP_Dropdown resourceDropDown;
    void Update()
    {
        UpdateResourceDropdown(SpaceStation.resources);
    }

    public void UpdateResourceDropdown(Dictionary<ResourceType, int> resources)
    {
        resourceDropDown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData
        {
            text = "Resources"
        });
        for (int i = 0; i < resources.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData 
            {
                text = StringUtils.Nicify(resources[i].type.ToString()).ToLower() + " : " + resources[i].amount,
            });
        }
        resourceDropDown.AddOptions(options);
    }
}