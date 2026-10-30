using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SettingsSRP)), CanEditMultipleObjects]
public sealed class SettingsSRPEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/Scripts/Dungeon/SRP/Editor/SRPInspector.uss");
        if (styles != null) root.styleSheets.Add(styles);
        var hint = new HelpBox("Réglez la probabilité « 1 / x » directement dans chaque placement, par exemple Barrel ou Spike.", HelpBoxMessageType.Info);
        root.Add(hint);
        var initialTitle = new Label("À la création de la salle");
        initialTitle.AddToClassList("srp-asset-section");
        root.Add(initialTitle);
        root.Add(new PropertyField(serializedObject.FindProperty("barrel"), "Barrels (facultatif)"));
        var trapsTitle = new Label("À chaque niveau de difficulté ajouté");
        trapsTitle.AddToClassList("srp-asset-section");
        root.Add(trapsTitle);
        root.Add(new PropertyField(serializedObject.FindProperty("traps"), "Pièges"));
        return root;
    }
}
