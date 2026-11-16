using UnityEngine;

/// <summary>Volta o jogador para o ponto inicial se ele cair do mapa.</summary>
public class RespawnOnFall : MonoBehaviour
{
    [SerializeField] private float minY = -15f;

    private Vector3 spawnPosition;
    private Rigidbody rb;

    private void Awake()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (transform.position.y < minY)
        {
            rb.velocity = Vector3.zero;
            rb.position = spawnPosition;
        }
    }
}
