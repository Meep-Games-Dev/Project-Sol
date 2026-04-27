#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Inspectable))]

public class EditorInspectable : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        if (serializedObject == null)
            return new VisualElement();
        VisualElement root = new VisualElement();

        PropertyField modeField = new PropertyField(serializedObject.FindProperty("type"));
        PropertyField titleField = new PropertyField(serializedObject.FindProperty("title"));
        PropertyField textField = new PropertyField(serializedObject.FindProperty("description"));


        root.Add(modeField);
        root.Add(titleField);
        root.Add(textField);


        void UpdateVisibility(InspectableTypes mode)
        {
            textField.style.display = (mode == InspectableTypes.Misc) ? DisplayStyle.Flex : DisplayStyle.None;
            titleField.style.display = (mode == InspectableTypes.Misc) ? DisplayStyle.Flex : DisplayStyle.None;
        }
        Inspectable targetScript = (Inspectable)target;
        UpdateVisibility(targetScript.type);

        modeField.RegisterValueChangeCallback(evt =>
        {
            UpdateVisibility((InspectableTypes)evt.changedProperty.enumValueIndex);
        });

        return root;
    }
}
#endif