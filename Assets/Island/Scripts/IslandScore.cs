using UnityEngine;

public sealed class IslandScore : MonoBehaviour
{
    AudioSource music;
    void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.clip = Resources.Load<AudioClip>("Island/Media/HarborMorning");
        music.loop = true; music.playOnAwake = false; music.volume = .5f;
    }
    public void SetEnabled(bool on)
    {
        if (!music.clip) return;
        if (on && !music.isPlaying) music.Play();
        if (!on) music.Pause();
    }
}
