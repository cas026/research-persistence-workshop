using UnityEngine;

public class Hazard : MonoBehaviour
{
    public float damagePerSecond = 35f;

    private Bounds _area;

    private void Start()
    {
        _area = GetComponent<Collider>().bounds;
    }

    private void Update()
    {
        PlayerController player = PlayerController.Instance;
        Vector3 position = player.transform.position;
        position.y = _area.center.y;

        if (_area.Contains(position))
        {
            player.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}
