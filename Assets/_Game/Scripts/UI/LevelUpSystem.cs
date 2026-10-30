using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using Button = UnityEngine.UI.Button;

[MovedFrom(true, null, "Dungeon.Runtime", "LevelUpSystem")]
[DisallowMultipleComponent]
public class LevelUpSystem : MonoBehaviour
{
    [SerializeField] LevelManager _system;
    [SerializeField] GameManager gameManager;
    [SerializeField] CanvasGroup panel;
    [SerializeField] List<GameObject> _images = new List<GameObject>();
    [SerializeField] GameObject _player;
    public UnityEvent TimeStopped = new UnityEvent();
    public UnityEvent TimeUnFreezed = new UnityEvent();
    readonly Dictionary<Button, UpgradeOffer> choices = new Dictionary<Button, UpgradeOffer>();
    readonly Dictionary<Button, UnityAction> listeners = new Dictionary<Button, UnityAction>();
    LifeSystem _lifeSystem;
    Movement _movement;
    bool open;
    float previousTimeScale;

    public bool IsOpen => open;
    public bool CanPresentUpgrade => !open && _system != null && _system.Upgrades > 0
        && (gameManager == null || (!gameManager.IsGameEnded && !gameManager.IsExitSequenceActive)) && _player != null && listeners.Count > 0;
    public GameObject Player => _player;
    public void SetPlayer(GameObject player) { _player = player; ResolvePlayer(); }
    public bool TryGetOffer(Button button, out UpgradeOffer offer) => choices.TryGetValue(button, out offer);
    void ResolvePlayer()
    {
        _lifeSystem = _player != null ? _player.GetComponentInChildren<LifeSystem>(true) : null;
        _movement = _player != null ? _player.GetComponentInChildren<Movement>(true) : null;
    }
    void OnEnable()
    {
        if (_system != null) _system.LevelUP.AddListener(OnLevelUp);
        if (gameManager != null) { gameManager.ExitSequenceStarted += Close; gameManager.GameEnded += Close; gameManager.RunStarted += Close; }
        ResolvePlayer(); SetVisible(false);
        foreach (GameObject image in _images)
        {
            if (image == null || !image.TryGetComponent(out Button button) || listeners.ContainsKey(button)) continue;
            UnityAction listener = () => LeveledUp(button);
            listeners.Add(button, listener); button.onClick.AddListener(listener); image.SetActive(false);
        }
    }
    void OnDisable()
    {
        if (_system != null) _system.LevelUP.RemoveListener(OnLevelUp);
        if (gameManager != null) { gameManager.ExitSequenceStarted -= Close; gameManager.GameEnded -= Close; gameManager.RunStarted -= Close; }
        Close();
        foreach (var pair in listeners) if (pair.Key != null) pair.Key.onClick.RemoveListener(pair.Value);
        listeners.Clear(); choices.Clear();
        foreach (GameObject image in _images) if (image != null) image.SetActive(false);
    }
    void Update() { if (CanPresentUpgrade) OnLevelUp(); }
    void OnLevelUp()
    {
        if (!CanPresentUpgrade) return;
        ResolvePlayer();
        if (_lifeSystem == null || _movement == null) { Debug.LogError("Assign the Player's LifeSystem and Movement before offering upgrades.", this); return; }
        var definitions = _system.PlayerStats.StatDefinitions;
        if (definitions.Count != 7) { Debug.LogError("Assign the seven stat definitions on PlayerStats before offering upgrades.", this); return; }
        var available = new StatDefinition[definitions.Count];
        int types = 0;
        for (int i = 0; i < available.Length; i++)
        {
            var definition = definitions[i];
            if (definition == null || !definition.IsValid || (types & (1 << (int)definition.Stat)) != 0)
            { Debug.LogError("Assign distinct stat definitions with one valid gain for each rarity.", this); return; }
            available[i] = definition; types |= 1 << (int)definition.Stat;
        }
        for (int i = available.Length - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); var swap = available[i]; available[i] = available[j]; available[j] = swap; }
        choices.Clear(); int index = 0;
        foreach (var pair in listeners)
        {
            Button button = pair.Key;
            if (index >= available.Length) { button.gameObject.SetActive(false); continue; }
            StatDefinition stat = available[index++];
            UpgradeRarity rarity = UpgradeRarityRules.Roll(_system.Luck, Random.value);
            var entry = stat.GetGain(rarity);
            var offer = new UpgradeOffer(stat, entry.Rarity, entry.Gain); choices.Add(button, offer);
            button.GetComponent<UpgradeCardView>().Present(stat, entry.Rarity, LabelFor(offer));
            button.gameObject.SetActive(true);
        }
        _player.GetComponent<PlayerImpactFeedback>()?.CancelFreeze();
        previousTimeScale = Time.timeScale; open = true; SetVisible(true);
        Time.timeScale = 0f; TimeStopped.Invoke();
        foreach (var choice in choices)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(choice.Key.gameObject);
            break;
        }
    }
    public void LeveledUp(Button button)
    {
        if (!open || button == null || !choices.TryGetValue(button, out UpgradeOffer offer)) return;
        float gain = Stats.FromPoints(offer.GainPoints);
        switch (offer.Stat)
        {
            case 0: _lifeSystem.AddMaxHealth(offer.GainPoints); break;
            case 1: _movement.AddSpeed(gain); break;
            case 2: _lifeSystem.AddDefense(Mathf.RoundToInt(gain)); break;
            case 3: _movement.AddMaxSpeed(gain); break;
            case 4: _movement.AddMaxNitro(gain); break;
            case 5: _movement.AddManiability(gain); break;
            case 6: _system.AddLuck(gain); break;
        }
        _system.ConsumeUpgrade(); Close();
    }
    string LabelFor(UpgradeOffer offer)
    {
        float current = offer.Stat == 0 ? _lifeSystem.MaxHealth : Stats.ToPoints(offer.Stat switch
        {
            1 => _movement.Speed, 2 => _lifeSystem.Defense,
            3 => _movement.MaximumSpeed, 4 => _movement.MaximumNitro, 5 => _movement.Maniability, _ => _system.Luck
        });
        return $"{offer.Definition.DisplayName}\n{current:0.##} → {current + offer.GainPoints:0.##}";
    }
    void Close()
    {
        bool wasOpen = open; open = false; choices.Clear(); SetVisible(false);
        foreach (GameObject image in _images) if (image != null) image.SetActive(false);
        if (wasOpen) { Time.timeScale = previousTimeScale; TimeUnFreezed.Invoke(); }
    }
    void SetVisible(bool visible)
    {
        if (panel == null) return;
        panel.alpha = visible ? 1f : 0f; panel.interactable = panel.blocksRaycasts = visible;
    }
}
