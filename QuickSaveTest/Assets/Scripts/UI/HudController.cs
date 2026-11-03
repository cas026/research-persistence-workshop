using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    private const float MessageDuration = 2.5f;

    private Label _nameLabel;
    private Label _coinsLabel;
    private Label _inventoryLabel;
    private Label _messageLabel;
    private VisualElement _healthFill;
    private VisualElement _pausePanel;
    private VisualElement _pauseButtons;
    private VisualElement _slotPanel;
    private Label _slotTitle;
    private SlotListView _slotList;
    private float _messageTimer;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _nameLabel = root.Q<Label>("NameLabel");
        _coinsLabel = root.Q<Label>("CoinsLabel");
        _inventoryLabel = root.Q<Label>("InventoryLabel");
        _messageLabel = root.Q<Label>("MessageLabel");
        _healthFill = root.Q("HealthFill");
        _pausePanel = root.Q("PausePanel");
        _pauseButtons = root.Q("PauseButtons");
        _slotPanel = root.Q("SlotPanel");
        _slotTitle = root.Q<Label>("SlotTitle");
        _slotList = new SlotListView(root.Q("SlotList"));

        root.Q<Button>("ResumeButton").clicked += () => SetPaused(false);
        root.Q<Button>("SaveButton").clicked += OpenSave;
        root.Q<Button>("LoadButton").clicked += OpenLoad;
        root.Q<Button>("MenuButton").clicked += GameSession.BackToMenu;
        root.Q<Button>("SlotBackButton").clicked += ShowPauseButtons;

        SetPaused(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(Time.timeScale != 0f);
        }

        PlayerData player = GameSession.Current.Player;
        _nameLabel.text = player.Name;
        _coinsLabel.text = $"Coins: {player.Coins}";
        _healthFill.style.width = Length.Percent(Mathf.Clamp(player.Health, 0f, 100f));
        _inventoryLabel.text = GameSession.Current.Inventory.Count == 0
            ? "Backpack: empty"
            : "Backpack: " + string.Join(", ", GameSession.Current.Inventory.Select(i => $"{i.Name} x{i.Amount}"));

        UpdateMessage();
    }

    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        _pausePanel.EnableInClassList("hidden", !paused);
        ShowPauseButtons();
    }

    private void UpdateMessage()
    {
        if (GameSession.PendingMessage != null)
        {
            _messageLabel.text = GameSession.PendingMessage;
            GameSession.PendingMessage = null;
            _messageTimer = MessageDuration;
        }

        _messageTimer -= Time.unscaledDeltaTime;
        _messageLabel.EnableInClassList("hidden", _messageTimer <= 0f);
    }

    private void ShowPauseButtons()
    {
        _pauseButtons.RemoveFromClassList("hidden");
        _slotPanel.AddToClassList("hidden");
    }

    private void OpenSave()
    {
        _slotTitle.text = "Save";
        _slotList.Show(SaveSlots.Manual, "Save", true, SaveToSlot);
        ShowSlots();
    }

    private void OpenLoad()
    {
        _slotTitle.text = "Load";
        _slotList.Show(SaveSlots.All, "Load", false, LoadFromSlot);
        ShowSlots();
    }

    private void ShowSlots()
    {
        _pauseButtons.AddToClassList("hidden");
        _slotPanel.RemoveFromClassList("hidden");
    }

    private void SaveToSlot(string slot)
    {
        GameSession.PendingMessage = GameSession.Save(slot)
            ? $"Saved to {SaveSlots.DisplayName(slot)}"
            : $"Saving to {SaveSlots.DisplayName(slot)} failed";

        _slotList.Refresh();
    }

    private void LoadFromSlot(string slot)
    {
        if (!GameSession.Load(slot))
        {
            GameSession.PendingMessage = $"{SaveSlots.DisplayName(slot)} could not be loaded";
        }
    }
}
