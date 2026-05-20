using UnityEngine;

public class PlayAnimationAudioOnEnter : StateMachineBehaviour
{
    [SerializeField] private AnimationAudioLibrary audioLibrary;
    [SerializeField] private string soundName;
    [SerializeField] private bool playOnEnter = true;

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

        source.PlayOneShot(clip);
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
