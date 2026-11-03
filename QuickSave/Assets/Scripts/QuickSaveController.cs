using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CI.QuickSave;

public class QuickSaveController : MonoBehaviour
{
    private TextField playerNameInput;
    private TextField ageInput;
    private Toggle soundToggle;
    private Label statusLabel;

    private string saveRootName = "DemoSave";

    void OnEnable()
    {
        Invoke(nameof(BindUI), 0.1f);
    }

    void BindUI()
    {
        UIDocument uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        var root = uiDoc.rootVisualElement;

        playerNameInput = root.Q<TextField>("PlayerNameInput");
        ageInput = root.Q<TextField>("AgeInput"); // Veranderd!
        soundToggle = root.Q<Toggle>("SoundToggle");
        statusLabel = root.Q<Label>("StatusLabel");

        root.Q<Button>("SaveButton")?.RegisterCallback<ClickEvent>(ev => Save());
        root.Q<Button>("LoadButton")?.RegisterCallback<ClickEvent>(ev => Load());
        root.Q<Button>("ClearButton")?.RegisterCallback<ClickEvent>(ev => ClearData());
        root.Q<Button>("StartButton")?.RegisterCallback<ClickEvent>(ev => StartGame());

        Load();
    }

    public void Save()
    {
        try
        {
            int ageVal = 0;
            int.TryParse(ageInput != null ? ageInput.value : "0", out ageVal);

            QuickSaveWriter.Create(saveRootName)
                           .Write("PlayerName", playerNameInput != null ? playerNameInput.value : "Speler1")
                           .Write("Age", ageVal) 
                           .Write("SoundEnabled", soundToggle != null ? soundToggle.value : true)
                           .Commit();

            if (statusLabel != null) statusLabel.text = "Profiel opgeslagen! (Naam, Leeftijd, Geluid)";
        }
        catch (System.Exception e)
        {
            if (statusLabel != null) statusLabel.text = "Fout bij opslaan!";
            Debug.LogError(e.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (QuickSaveWriter.RootExists(saveRootName))
            {
                QuickSaveReader.Create(saveRootName)
                               .Read<string>("PlayerName", (r) => { if (playerNameInput != null) playerNameInput.value = r; })
                               .Read<int>("Age", (r) => { if (ageInput != null) ageInput.value = r.ToString(); }) 
                               .Read<bool>("SoundEnabled", (r) => { if (soundToggle != null) soundToggle.value = r; });

                if (statusLabel != null) statusLabel.text = "Profiel succesvol geladen";
            }
            else
            {
                if (statusLabel != null) statusLabel.text = "Geen savebestand gevonden.";
            }
        }
        catch (System.Exception e)
        {
            if (statusLabel != null) statusLabel.text = "Fout bij inlezen.";
            Debug.LogError(e.Message);
        }
    }

    private void ClearData()
    {
        try
        {
            if (QuickSaveWriter.RootExists(saveRootName))
            {
                QuickSaveWriter.DeleteRoot(saveRootName);
            }

            if (playerNameInput != null) playerNameInput.value = "";
            if (ageInput != null) ageInput.value = ""; 
            if (soundToggle != null) soundToggle.value = false;

            if (statusLabel != null) statusLabel.text = "Savebestand en velden volledig gewist.";
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            if (statusLabel != null) statusLabel.text = "Fout bij wissen van data.";
        }
    }

    public void StartGame()
    {
        Save();
        SceneManager.LoadScene("game");
    }
}