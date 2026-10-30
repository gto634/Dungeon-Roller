using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class CorridorVisual : MonoBehaviour
{
    const int CurrentHierarchyVersion = 6;
    [SerializeField, HideInInspector] int hierarchyVersion;
    [SerializeField] Corridor corridor;
    [SerializeField] StyleCorridor style;
    [SerializeField] Transform modelRoot;
    [SerializeField] CorridorIndicatorFeedback indicators;
    [SerializeField, HideInInspector] CorridorPassage passage;
    [SerializeField] CorridorPassage doorA, doorB;
    [SerializeField] Transform physic;
    [SerializeField] Transform action;
    [SerializeField] Transform feedback;
    [SerializeField] CorridorPassageAction passageAction;
    [SerializeField, HideInInspector] Transform generatedRoot;

    public Corridor Owner => corridor;
    public Transform ModelRoot => modelRoot;
    public CorridorIndicatorFeedback Indicators => indicators;
    public CorridorPassage Passage => doorA != null ? doorA : passage;
    public CorridorPassage DoorA => doorA;
    public CorridorPassage DoorB => doorB;
    public CorridorPassageAction PassageAction => passageAction;
    public bool HasActionHierarchy => hierarchyVersion == CurrentHierarchyVersion && modelRoot != null && indicators != null && physic != null
        && action != null && feedback != null && passageAction != null
        && doorA != null && doorB != null && doorA != doorB
        && !doorA.CoversWholeCorridor && !doorB.CoversWholeCorridor;

    void Reset() => corridor = GetComponentInParent<Corridor>();
    void OnEnable()
    {
        if (corridor == null) return;
        corridor.DataChanged += Rebuild;
        if (corridor.IsInitialized) Rebuild();
    }
    void OnDisable()
    {
        if (corridor != null) corridor.DataChanged -= Rebuild;
    }

    [ContextMenu("Rebuild Visuals")]
    public void Rebuild()
    {
        if (corridor == null || !corridor.IsInitialized) return;
        if (style == null || !style.IsValid)
        {
            Debug.LogError("Assign a complete corridor style before building its visuals.", this);
            return;
        }
        corridor.RefreshTransform();
        RestoreSavedParts();
        CorridorData data = corridor.Data;
        modelRoot.SetLocalPositionAndRotation(Vector3.zero,
            Quaternion.Euler(0f, -Mathf.Atan2(data.Direction.y, data.Direction.x) * Mathf.Rad2Deg, 0f));
        physic.SetPositionAndRotation(modelRoot.position, modelRoot.rotation);
        modelRoot.gameObject.SetActive(true);
        physic.gameObject.SetActive(true);
        feedback.gameObject.SetActive(true);
        action.gameObject.SetActive(true);
        doorA.Bind(corridor, style);
        doorB.Bind(corridor, style);
        passageAction.Bind(corridor, doorA, doorB);
    }

    void RestoreSavedParts()
    {
        if (HasActionHierarchy) return;
        bool consumedA = doorA != null && doorA.GetComponent<ICorridorTriggerState>()?.IsConsumed == true;
        bool consumedB = doorB != null && doorB.GetComponent<ICorridorTriggerState>()?.IsConsumed == true;
        var previousDoors = corridor.GetComponentsInChildren<CorridorPassage>(true);
        foreach (var door in previousDoors) DungeonObjectUtility.DestroyOwned(door.gameObject);
        if (modelRoot != null) DungeonObjectUtility.DestroyOwned(modelRoot.gameObject);
        if (generatedRoot != null) DungeonObjectUtility.DestroyOwned(generatedRoot.gameObject);
        if (physic != null) DungeonObjectUtility.DestroyOwned(physic.gameObject);
        if (action != null) DungeonObjectUtility.DestroyOwned(action.gameObject);
        if (feedback != null) DungeonObjectUtility.DestroyOwned(feedback.gameObject);
        generatedRoot = null;

        GameObject replacement = Instantiate(style.CorridorPrefab, corridor.transform, false);
        CorridorVisual template = replacement.GetComponentInChildren<CorridorVisual>(true);
        modelRoot = template.modelRoot;
        modelRoot.SetParent(transform, false);
        indicators = template.indicators;
        feedback = template.feedback;
        feedback.SetParent(transform, false);
        physic = template.physic;
        physic.SetParent(corridor.transform, false);
        action = template.action;
        action.SetParent(corridor.transform, false);
        doorA = template.doorA;
        doorB = template.doorB;
        passage = null;
        passageAction = template.passageAction;
        hierarchyVersion = template.hierarchyVersion;
        passageAction.Bind(corridor, doorA, doorB);
        passageAction.RestoreConsumption(consumedA, consumedB);
        if (passageAction.HasPassed) indicators.SetAllGreen();
        DungeonObjectUtility.DestroyOwned(replacement);
    }

    [ContextMenu("Clear Visuals")]
    public void ClearVisuals()
    {
        if (modelRoot != null) modelRoot.gameObject.SetActive(false);
        if (physic != null) physic.gameObject.SetActive(false);
        if (action != null) action.gameObject.SetActive(false);
        if (feedback != null) feedback.gameObject.SetActive(false);
        if (generatedRoot != null) DungeonObjectUtility.DestroyOwned(generatedRoot.gameObject);
        generatedRoot = null;
    }
}
