using UnityEngine;

/// <summary>New structures arrive on the island when the corresponding chapter is delivered.</summary>
public sealed class IslandLandmark : MonoBehaviour
{
    [Range(1,8)] public int chapter;
    public Transform structure;
    public Transform lantern;
    IslandGame game;
    void Start() { game=FindFirstObjectByType<IslandGame>(); }
    void Update()
    {
        if(!game || game.State==null || !structure)return;
        bool visible=game.State.CompletedChapters>=chapter;
        if(structure.gameObject.activeSelf!=visible)structure.gameObject.SetActive(visible);
        if(visible && lantern)lantern.Rotate(0,25*Time.deltaTime,0);
    }
}
