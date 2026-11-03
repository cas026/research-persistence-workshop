using UnityEngine;

// The object name is the save ID: renaming it invalidates old saves.
public class Pickup : MonoBehaviour
{
    public int coins;
    public string itemName;

    private void Start()
    {
        if (GameSession.IsCollected(name))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        GameSession.MarkCollected(name);
        GameSession.Current.Player.Coins += coins;

        if (!string.IsNullOrEmpty(itemName))
        {
            GameSession.AddItem(itemName);
            GameSession.PendingMessage = $"{itemName} picked up";
        }

        GameAudio.PlayBeep();
        gameObject.SetActive(false);
    }
}
