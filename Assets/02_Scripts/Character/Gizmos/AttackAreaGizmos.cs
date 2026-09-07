using UnityEngine;

public class AttackAreaGizmos : MonoBehaviour
{
    [SerializeField] private Vector2 _size = new Vector2(1f, 2f);
    [SerializeField] private float _offset = 1f;
    private Vector2 _direction;
    private Vector2 _center;

    protected SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDrawGizmos()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

        _direction = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
        _center = (Vector2)transform.position + (_direction * _offset);

        // Gizmos.color = Color.chartreuse;
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f); // a: alpha 투명도 (1f 이면 불투명)
                                                    // Gizmos.DrawWireSphere(transform.position, );  // 3d로 그림
                                                    // Gizmos.DrawWireCube(_center, _size);    
        Gizmos.DrawCube(_center, _size);
    }
}
