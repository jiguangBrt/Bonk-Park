using UnityEngine;

public class Park : MonoBehaviour
{
    [SerializeField] Vector2 referenceViewport = new Vector2(19.2f, 10.8f);
    [SerializeField] Vector2 arenaSize = new Vector2(21f, 11.8f);

    public Vector2 ReferenceViewport => referenceViewport;
    public Vector2 Size => arenaSize;
    public float TargetAspect => referenceViewport.x / referenceViewport.y;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaSize.x, arenaSize.y, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(referenceViewport.x, referenceViewport.y, 0f));
    }
}
