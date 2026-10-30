using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(PlacementDefinitionSRP)), CanEditMultipleObjects]
public sealed class PlacementDefinitionSRPEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/Scripts/Dungeon/SRP/Editor/SRPInspector.uss");
        if (styles != null) root.styleSheets.Add(styles);

        var frequencyTitle = new Label("Fréquence");
        frequencyTitle.AddToClassList("srp-asset-section");
        root.Add(frequencyTitle);
        var row = new VisualElement { name = "placement-probability-row" };
        row.AddToClassList("srp-probability-row");
        var probabilityLabel = new Label("Probabilité");
        probabilityLabel.AddToClassList("srp-probability-label");
        row.Add(probabilityLabel);
        var prefix = new Label("1 /");
        prefix.AddToClassList("srp-probability-prefix");
        row.Add(prefix);
        var denominator = new IntegerField
        {
            name = "placement-probability-denominator", bindingPath = "probabilityDenominator", isDelayed = true,
            tooltip = "Modifier x : 1 tentative pour x cases. x doit être supérieur ou égal à 1."
        };
        denominator.AddToClassList("srp-probability-denominator");
        denominator.RegisterValueChangedCallback(change =>
        {
            if (change.newValue < 1) denominator.value = 1;
        });
        row.Add(denominator);
        root.Add(row);
        var hint = new Label("Plus x est grand, plus l'objet est rare. Barrels : à la création de la salle. Pièges : par niveau de difficulté.");
        hint.AddToClassList("srp-explanation");
        root.Add(hint);

        var placementTitle = new Label("Placement");
        placementTitle.AddToClassList("srp-asset-section");
        root.Add(placementTitle);
        root.Add(new PropertyField(serializedObject.FindProperty("prefab"), "Prefab"));
        root.Add(new PropertyField(serializedObject.FindProperty("footprint"), "Taille en cases"));
        root.Add(new PropertyField(serializedObject.FindProperty("wallPadding"), "Marge aux murs") { tooltip = "Distance minimale aux murs, en cases." });

        var details = new Foldout { text = "Identifiant et aperçu", value = false };
        details.Add(new PropertyField(serializedObject.FindProperty("placementId"), "Identifiant"));
        details.Add(new PropertyField(serializedObject.FindProperty("previewColor"), "Couleur de la grille"));
        root.Add(details);
        return root;
    }
}
