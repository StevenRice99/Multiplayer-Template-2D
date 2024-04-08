using Mirror;
using UnityEngine;

/// <summary>
/// The controller for the characters.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(NetworkTransformReliable))]
[RequireComponent(typeof(NetworkIdentity))]
public class PlayerController : NetworkBehaviour
{
    /// <summary>
    /// If this player is ready to start the match.
    /// </summary>
    [field: SyncVar]
    public bool Ready { get; private set; }

    /// <summary>
    /// The name of the player.
    /// </summary>
    [field: SyncVar]
    public string PlayerName { get; private set; }
    
    /// <summary>
    /// The collider for the character.
    /// </summary>
    private BoxCollider2D _boxCollider;

    /// <summary>
    /// The velocity the player is moving at.
    /// </summary>
    private Vector2 _velocity;
    
    /// <summary>
    /// If the character is grounded or not.
    /// </summary>
    private bool _grounded;

    /// <summary>
    /// Set that this player is ready or not.
    /// </summary>
    /// <param name="ready">The value to set being ready to.</param>
    [Command]
    public void SetReadyCmd(bool ready)
    {
        Ready = ready;
    }

    private void Awake()
    {
        // Store a reference to this player.
        GameManager.players.Add(this);
    }

    private void OnDestroy()
    {
        // Remove the reference to this player.
        GameManager.players.Remove(this);
    }

    private void Start()
    {
        // Get the box collider.
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    public override void OnStartLocalPlayer()
    {
        // Keep a reference to the local player.
        GameManager.localPlayer = this;
        
        // Set the name of the player.
        SetNameCmd(GameManager.PlayerName);
    }

    private void Update()
    {
        // Do nothing on remote clients.
        if (!isLocalPlayer)
        {
            return;
        }

        // Handle movement on the local client.
        // Movement logic based on: https://github.com/IronWarrior/2DCharacterControllerTutorial
        // Check if on the ground.
        if (_grounded)
        {
            // Zero out the velocity if on the ground.
            _velocity.y = 0;

            if (GameManager.Jump)
            {
                // Calculate the velocity required to achieve the target jump height.
                _velocity.y = Mathf.Sqrt(2 * GameManager.JumpForce * Mathf.Abs(Physics2D.gravity.y));
            }
        }

        // Get movement.
        _velocity.x = GameManager.Move * GameManager.Speed;

        // Add gravity.
        _velocity.y += Physics2D.gravity.y * Time.deltaTime;

        // Perform the move with the transform.
        Transform t = transform;
        t.Translate(_velocity * Time.deltaTime);

        // Assume we are not grounded unless we detect the ground.
        _grounded = false;

        // Retrieve all colliders we have intersected after velocity has been applied.
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(t.position, _boxCollider.size, 0))
        {
            // Ignore our own collider.
            if (hit == _boxCollider)
            {
                continue;
            }

            ColliderDistance2D colliderDistance = hit.Distance(_boxCollider);

            // Ensure that we are still overlapping this collider.
            // The overlap may no longer exist due to another intersected collider pushing us out of this one.
            if (!colliderDistance.isOverlapped)
            {
                continue;
            }

            // Ensure we are not stuck in a collider.
            t.Translate(colliderDistance.pointA - colliderDistance.pointB);

            // If we intersect an object beneath us, set grounded to true. 
            if (Vector2.Angle(colliderDistance.normal, Vector2.up) < 90 && _velocity.y < 0)
            {
                _grounded = true;
            }
        }
    }

    /// <summary>
    /// Update the name on the server.
    /// </summary>
    /// <param name="playerName">The name.</param>
    [Command]
    private void SetNameCmd(string playerName)
    {
        PlayerName = playerName;
    }
}