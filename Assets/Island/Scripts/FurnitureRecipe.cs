using UnityEngine;

[CreateAssetMenu(menuName = "Island/Furniture Recipe", fileName = "NewFurniture")]
public class FurnitureRecipe : ScriptableObject
{
    [Tooltip("セーブデータで使う一意のID。公開後は変更しないでください。")]
    public string id;
    public string displayName;
    [Min(0)] public int woodCost;
    [Min(0)] public int stoneCost;
    [Tooltip("完成報告が必要な本編依頼の数。0は最初から製作可能。")]
    [Range(0, 8)] public int unlockAfterChapters;
    public GameObject prefab;
}
