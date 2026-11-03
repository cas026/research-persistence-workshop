using UnityEngine;

public class LevelController : MonoBehaviour
{
    public float autosaveInterval = 30f;

    private float _autosaveTimer;

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuickSave();
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            QuickLoad();
            return;
        }

        UpdateAutosave();
    }

    public void QuickSave()
    {
        GameSession.PendingMessage = GameSession.Save(SaveSlots.Quick) ? "Quicksave created" : "Quicksave failed";
    }

    public void QuickLoad()
    {
        if (!GameSession.Load(SaveSlots.Quick))
        {
            GameSession.PendingMessage = "No usable quicksave found";
        }
    }

    private void UpdateAutosave()
    {
        _autosaveTimer += Time.deltaTime;

        if (_autosaveTimer >= autosaveInterval)
        {
            _autosaveTimer = 0f;

            if (GameSession.Save(SaveSlots.Auto))
            {
                GameSession.PendingMessage = "Autosaved";
            }
        }
    }

    private void OnApplicationQuit()
    {
        GameSession.Save(SaveSlots.Auto);
    }
}
