using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TransitionAnimationPanel : MonoBehaviour
{
    [Header("Rotator")]
    [SerializeField] private GameObject rotatorGO;
    [SerializeField] private Vector3 rotateToEuler = new Vector3(0f, 180f, 0f);
    [Header("Rotate - Subir (to)")]
    [SerializeField] private float rotateToDuration = 0.35f;
    [SerializeField] private Ease rotateToEase = Ease.OutCubic;
    [Header("Rotate - Bajar (back)")]
    [SerializeField] private float rotateBackDuration = 0.35f;
    [SerializeField] private Ease rotateBackEase = Ease.InCubic;

    [Header("Panel")]
    [SerializeField] private UiPanelTransition uiPanelTransition;
    [SerializeField] private float delayBeforeDescend = 0.1f;
    [SerializeField] private float delayBeforeFadeOut = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip clipSound;
    [SerializeField] private AudioSource audioSource;

    private Tween activeTween;

    public void PlayPanelAnimation()
    {
        if (rotatorGO == null)
        {
            // If there's no rotator, just trigger panel fade immediately
            uiPanelTransition?.TransitionOut();
            return;
        }

        Transform rt = rotatorGO.transform;
        Vector3 originalEuler = rt.localEulerAngles;

        KillActiveTween();

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOLocalRotate(rotateToEuler, rotateToDuration).SetEase(rotateToEase));
        seq.AppendInterval(delayBeforeDescend);
        seq.AppendCallback(() =>
        {
            if (clipSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clipSound);
                }
                else
                {
                    Vector3 pos = (Camera.main != null) ? Camera.main.transform.position : transform.position;
                    AudioSource.PlayClipAtPoint(clipSound, pos);
                }
            }
        });
        seq.Append(rt.DOLocalRotate(originalEuler, rotateBackDuration).SetEase(rotateBackEase));
        seq.OnComplete(() => StartCoroutine(DelayedFadeOut()));

        activeTween = seq;
    }

    private IEnumerator DelayedFadeOut()
    {
        yield return new WaitForSeconds(delayBeforeFadeOut);
        uiPanelTransition?.TransitionOut();
    }

    private void KillActiveTween()
    {
        if (activeTween != null)
        {
            activeTween.Kill(false);
            activeTween = null;
        }
    }

    private void OnDisable()
    {
        KillActiveTween();
    }
}
