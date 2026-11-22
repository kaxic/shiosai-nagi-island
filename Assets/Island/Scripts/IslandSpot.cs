using UnityEngine;

/// <summary>Scene-editable interaction point. The ID stays the same when moving or renaming it.</summary>
public sealed class IslandSpot : MonoBehaviour
{
    public enum Kind { Tree, Stone, Workbench, Farm, Cabin, FurnitureSlot, TidePool }
    [Range(0, 2)] public int shoreIndex;

    public Kind kind;
    [Tooltip("セーブに使う固定ID。複製したときは新しい値に変更します。")]
    public string id;
    [Min(0)] public float blockingRadius = 0.65f;
    [Min(0.3f)] public float reach = 2.1f;
    [Min(1)] public int resourceYield = 3;
    [Min(1)] public int regrowSeconds = 20;
    [Min(1)] public int cropGrowSeconds = 35;
    [Tooltip("通常モデル。畑の場合は土のモデル。")] public GameObject model;
    [Tooltip("採集後のモデル、または畑の芽。")] public GameObject alternate;
    [Tooltip("収穫できる作物モデル。")] public GameObject ripe;

    public Vector2 Center => new Vector2(transform.position.x, transform.position.z);

    void OnDrawGizmos()
    {
        if (Application.isPlaying || kind != Kind.FurnitureSlot) return;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.24f, 0.65f, 0.45f, 0.85f);
        Gizmos.DrawWireCube(Vector3.up * 0.06f, new Vector3(2, 0.08f, 1.8f));
        Gizmos.matrix = previous;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.75f, 0.22f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, reach);
        if (blockingRadius <= 0) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blockingRadius);
    }
}
