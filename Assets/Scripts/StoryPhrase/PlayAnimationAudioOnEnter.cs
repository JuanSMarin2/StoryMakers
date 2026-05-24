using UnityEngine;

public class PlayAnimationAudioOnEnter : StateMachineBehaviour
{
    [SerializeField] private AnimationAudioLibrary audioLibrary;
    [SerializeField] private string soundName;
    [SerializeField] private bool playOnEnter = true;
    [SerializeField] private float audibleDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!playOnEnter)
        {
            return;
        }

        TryPlaySound(animator);
    }

    private void TryPlaySound(Animator animator)
    {
        if (audioLibrary == null || animator == null)
        {
            return;
        }

        AudioClip clip;
        if (!audioLibrary.TryGetClip(soundName, out clip))
        {
            return;
        }

        AudioSource source = FindAudioSource(animator);
        if (source == null)
        {
            return;
        }

        AudioFadeLimiter limiter = source.GetComponent<AudioFadeLimiter>();
        if (limiter == null)
        {
            limiter = source.gameObject.AddComponent<AudioFadeLimiter>();
        }

        limiter.PlayClip(source, clip, Mathf.Max(0f, audibleDuration), Mathf.Max(0f, fadeOutDuration));
    }

    private static AudioSource FindAudioSource(Animator animator)
    {
        AudioSource source = animator.GetComponent<AudioSource>();
        if (source != null)
        {
            return source;
        }

        return animator.GetComponentInParent<AudioSource>();
    }
}

public class AudioFadeLimiter : MonoBehaviour
{
    private AudioSource source;
    private float baseVolume = 1f;
    private float audibleDuration;
    private float fadeOutDuration;
    private float elapsed;
    private float fadeElapsed;
    private bool fading;
    private bool isPlaying;

    public void PlayClip(AudioSource audioSource, AudioClip clip, float maxAudibleDuration, float fadeDuration)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        source = audioSource;
        if (!isPlaying)
        {
            baseVolume = Mathf.Clamp01(source.volume);
        }

        audibleDuration = Mathf.Max(0f, maxAudibleDuration);
        fadeOutDuration = Mathf.Max(0f, fadeDuration);

        source.Stop();
        source.volume = baseVolume;
        source.clip = clip;
        source.Play();

        elapsed = 0f;
        fadeElapsed = 0f;
        fading = false;
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying || source == null)
        {
            return;
        }

        elapsed += Time.deltaTime;

        if (!fading && elapsed >= audibleDuration)
        {
            fading = true;
            fadeElapsed = 0f;
        }

        if (fading)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeOutDuration > 0f ? Mathf.Clamp01(fadeElapsed / fadeOutDuration) : 1f;
            source.volume = Mathf.Lerp(baseVolume, 0f, t);

            if (t >= 1f)
            {
                source.Stop();
                source.volume = baseVolume;
                isPlaying = false;
            }
        }
    }
}
