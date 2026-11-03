using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class QuickSaveController : MonoBehaviour
{
    private VisualElement _mainPanel;
    private VisualElement _newGamePanel;
    private VisualElement _loadPanel;
    private Button _continueButton;
    private Label _messageLabel;
    private TextField _nameInput;
    private SlotListView _slotList;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _mainPanel = root.Q("MainPanel");
        _newGamePanel = root.Q("NewGamePanel");
        _loadPanel = root.Q("LoadPanel");
        _continueButton = root.Q<Button>("ContinueButton");
        _messageLabel = root.Q<Label>("MessageLabel");
        _nameInput = root.Q<TextField>("NameInput");
        _slotList = new SlotListView(root.Q("SlotList"));

        _continueButton.clicked += Continue;
        root.Q<Button>("NewGameButton").clicked += OpenNewGame;
        root.Q<Button>("LoadButton").clicked += OpenLoad;
        root.Q<Button>("QuitButton").clicked += Application.Quit;
        root.Q<Button>("StartButton").clicked += StartNewGame;
        root.Q<Button>("NewGameBackButton").clicked += ShowMain;
        root.Q<Button>("LoadBackButton").clicked += ShowMain;

        ShowMain();
        _messageLabel.text = GameSession.PendingMessage ?? string.Empty;
        GameSession.PendingMessage = null;
    }

    private void ShowMain()
    {
        ShowOnly(_mainPanel);
        _messageLabel.text = string.Empty;
        _continueButton.SetEnabled(GameSession.MostRecentSlot() != null);
    }

    private void ShowOnly(VisualElement panel)
    {
        _mainPanel.EnableInClassList("hidden", panel != _mainPanel);
        _newGamePanel.EnableInClassList("hidden", panel != _newGamePanel);
        _loadPanel.EnableInClassList("hidden", panel != _loadPanel);
    }

    private void Continue()
    {
        LoadSlot(GameSession.MostRecentSlot());
    }

    private void OpenNewGame()
    {
        _nameInput.value = GameSession.Settings.PlayerName;
        ShowOnly(_newGamePanel);
    }

    private void StartNewGame()
    {
        GameSession.Settings.PlayerName = _nameInput.value;
        GameSession.SaveSettings();
        GameSession.NewGame(_nameInput.value);
    }

    private void OpenLoad()
    {
        _slotList.Show(SaveSlots.All, "Laden", false, LoadSlot);
        ShowOnly(_loadPanel);
    }

    private void LoadSlot(string slot)
    {
        if (!GameSession.Load(slot))
        {
            ShowMain();
            _messageLabel.text = $"{SaveSlots.DisplayName(slot)} kon niet worden geladen";
        }
    }
}
