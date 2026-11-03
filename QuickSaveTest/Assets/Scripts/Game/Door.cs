using UnityEngine;

public class Door : MonoBehaviour
{
    public string requiredItem;
    public float interactDistance = 3.5f;

    private bool _warned;

    private void Start()
    {
        if (GameSession.IsCollected(name))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        bool isClose = Vector3.Distance(PlayerController.Instance.transform.position, transform.position) <= interactDistance;
        if (!isClose)
        {
            _warned = false;
            return;
        }

        if (GameSession.RemoveItem(requiredItem))
        {
            GameSession.MarkCollected(name);
            GameSession.PendingMessage = "The door is open";
            GameAudio.PlayBeep();
            gameObject.SetActive(false);
        }
        else if (!_warned)
        {
            _warned = true;
            GameSession.PendingMessage = $"You need a {requiredItem}";
        }
    }
}
