using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip clapClip;
    [SerializeField] private AudioClip rWeirdClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayClickSound()
    {
        PlayClip(clickClip);
    }

    public void PlayClapSound()
    {
        PlayClip(clapClip);
    }

    public void PlayRWeirdSound()
    {
        PlayClip(rWeirdClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}