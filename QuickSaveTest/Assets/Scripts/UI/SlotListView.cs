using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class SlotListView
{
    private readonly VisualElement _container;

    private IEnumerable<string> _slots;
    private string _actionLabel;
    private bool _canUseEmptySlot;
    private Action<string> _onAction;

    public SlotListView(VisualElement container)
    {
        _container = container;
    }

    public void Show(IEnumerable<string> slots, string actionLabel, bool canUseEmptySlot, Action<string> onAction)
    {
        _slots = slots;
        _actionLabel = actionLabel;
        _canUseEmptySlot = canUseEmptySlot;
        _onAction = onAction;
        Refresh();
    }

    public void Refresh()
    {
        _container.Clear();

        foreach (string slot in _slots)
        {
            _container.Add(CreateRow(slot));
        }
    }

    private VisualElement CreateRow(string slot)
    {
        bool exists = SaveSystem.Exists(slot);
        SlotSummary summary = SaveSystem.ReadSummary(slot);
        bool isCorrupt = exists && summary == null;

        VisualElement row = new VisualElement();
        row.AddToClassList("slot-row");

        VisualElement text = new VisualElement();
        text.AddToClassList("slot-text");
        row.Add(text);

        Label title = new Label(SaveSlots.DisplayName(slot));
        title.AddToClassList("slot-title");
        text.Add(title);

        text.Add(new Label(Describe(summary, exists)));

        Button action = new Button(() => _onAction(slot)) { text = _actionLabel };
        action.SetEnabled(!isCorrupt && (exists || _canUseEmptySlot));
        row.Add(action);

        Button delete = new Button(() =>
        {
            SaveSystem.Delete(slot);
            Refresh();
        })
        { text = "Delete" };
        delete.AddToClassList("danger");
        delete.SetEnabled(exists);
        row.Add(delete);

        return row;
    }

    private static string Describe(SlotSummary summary, bool exists)
    {
        if (summary != null)
        {
            return $"{summary.PlayerName} - {summary.Coins} coins - {summary.SavedAt:dd-MM HH:mm:ss}";
        }

        return exists ? "Damaged file" : "Empty";
    }
}
