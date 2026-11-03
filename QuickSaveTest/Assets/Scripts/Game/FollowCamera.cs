using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 15f, -10f);

    private void LateUpdate()
    {
        Transform player = PlayerController.Instance.transform;
        transform.position = player.position + offset;
        transform.LookAt(player.position);
    }
}
