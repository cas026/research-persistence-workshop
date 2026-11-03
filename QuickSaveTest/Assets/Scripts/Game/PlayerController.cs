using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public float moveSpeed = 7f;

    private CharacterController _controller;
    private Vector3 _startPosition;
    private float _verticalVelocity;

    private void Awake()
    {
        Instance = this;
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        GameSession.EnsureGameExists();
    }

    private void Start()
    {
        if (GameSession.TakeRestorePosition())
        {
            PlayerData data = GameSession.Current.Player;
            Teleport(data.Position, data.Rotation);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Move(Vector3.ClampMagnitude(direction, 1f));
    }

    public void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + Physics.gravity.y * Time.deltaTime;

        _controller.Move((direction * moveSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        PlayerData data = GameSession.Current.Player;
        data.Health -= amount;

        if (data.Health <= 0f)
        {
            data.Health = 100f;
            Teleport(_startPosition, Quaternion.identity);
            GameSession.PendingMessage = "You are back at the start";
        }
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        // An active CharacterController overwrites the new position.
        _controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _controller.enabled = true;
        _verticalVelocity = 0f;
    }
}
