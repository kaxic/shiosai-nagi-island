using UnityEngine;

// Short, original plucked notes. All samples are generated once at startup.
public sealed class IslandSounds : MonoBehaviour
{
    AudioSource speaker;
    AudioClip[] clips;

    void Awake()
    {
        speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.volume = 0.32f;
        clips = new[] {
            Pluck(210, 320, 0.18f), Pluck(540, 810, 0.19f),
            Pluck(392, 523, 0.32f), Pluck(440, 660, 0.46f), Pluck(523, 784, 0.75f)
        };
    }

    static AudioClip Pluck(float first, float second, float length)
    {
        const int hz = 22050;
        var samples = new float[(int)(hz * length)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / hz;
            float tail = Mathf.Clamp01((length - t) * 35);
            samples[i] = (Note(first, t) + 0.65f * Note(second, t - length * 0.23f)) * tail * 0.4f;
        }
        var clip = AudioClip.Create("島の音", samples.Length, 1, hz, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static float Note(float hz, float t)
    {
        if (t < 0) return 0;
        float a = 2 * Mathf.PI * hz * t;
        return Mathf.Min(1, t * 140) * Mathf.Exp(-8.2f * t)
            * (Mathf.Sin(a) + 0.22f * Mathf.Sin(2 * a) + 0.08f * Mathf.Sin(3 * a));
    }

    public void Play(int index) { if (speaker && clips != null) speaker.PlayOneShot(clips[index]); }
    void OnDestroy() { if (clips != null) foreach (var clip in clips) Destroy(clip); }
}
