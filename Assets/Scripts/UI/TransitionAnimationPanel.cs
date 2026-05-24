using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TransitionAnimationPanel : MonoBehaviour
{
    [Header("Rotator")]
    [SerializeField] private GameObject rotatorGO;
    [SerializeField] private Vector3 rotateToEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] private float rotateDuration = 0.35f;
    [SerializeField] private Ease rotateEase = Ease.OutCubic;

    [Header("Panel")]
    [SerializeField] private UiPanelTransition uiPanelTransition;
    [SerializeField] private float delayBeforeFadeOut = 0.5f;

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
        seq.Append(rt.DOLocalRotate(rotateToEuler, rotateDuration).SetEase(rotateEase));
        seq.Append(rt.DOLocalRotate(originalEuler, rotateDuration).SetEase(rotateEase));
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
