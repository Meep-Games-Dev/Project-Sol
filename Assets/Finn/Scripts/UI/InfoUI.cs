using UnityEngine;

public class InfoUI : MonoBehaviour
{
    public int idx;
    public UIManager UIManager;

    private void Start()
    {
        UIManager = FindFirstObjectByType<UIManager>();
    }

    public void Kill()
    {
        UIManager.KillDialogue(idx);
    }
}
