using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PopupBase : UIBase
{
    //[SerializeField] private DOTweenAnimation popupAnimation;
    private Coroutine openAnimationCoroutine;
    private Coroutine hideAnimationCoroutine;

    private void OnDestroy()
    {
        if (openAnimationCoroutine != null)
        {
            StopCoroutine(openAnimationCoroutine);
            openAnimationCoroutine = null;
        }

        if (hideAnimationCoroutine != null)
        {
            StopCoroutine(hideAnimationCoroutine);
            hideAnimationCoroutine = null;
        }
    }

    public override void InitializeUI()
    {
        base.InitializeUI();
        gameObject.SetActive(true);
        //openAnimationCoroutine = StartCoroutine(PlayOpenAnimation());
    }

    //protected IEnumerator PlayOpenAnimation()
    //{
    //    if (popupAnimation != null)
    //    {
    //        popupAnimation.DORestart();
    //        popupAnimation.DOPlay();

    //        if (popupAnimation.tween != null)
    //        {
    //            yield return popupAnimation.tween.WaitForCompletion();
    //        }
    //    }
    //    else
    //    {
    //        yield return new WaitForSecondsRealtime(0.14f);
    //    }

    //    openAnimationCoroutine = null;
    //}

    public override void HideUI()
    {
        if (!gameObject.activeInHierarchy)
        {
            if (DestroyOnHide)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        hideAnimationCoroutine = StartCoroutine(PlayHideAnimation());
        base.HideUI();
    }

    //protected override IEnumerator PlayHideAnimation()
    //{
    //    if (popupAnimation != null)
    //    {
    //        popupAnimation.DOPlayBackwards();

    //        if (popupAnimation.tween != null)
    //        {
    //            yield return popupAnimation.tween.WaitForCompletion();
    //        }
    //    }
    //    else
    //    {
    //        yield return new WaitForSecondsRealtime(0.14f);
    //    }

    //    hideAnimationCoroutine = null;
    //}
}
