using UnityEngine;

public class Park : MonoBehaviour
{
    [SerializeField] Vector2 size = new Vector2(40f, 30f);

    public Vector2 Size => size;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}
