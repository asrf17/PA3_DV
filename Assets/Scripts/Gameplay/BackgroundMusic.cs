using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.ignoreListenerPause = true;
        }

        void Start()
        {
            if (source.clip && !source.isPlaying) source.Play();
        }
    }
}
