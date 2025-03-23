using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

[UIInfo("WaveFinishFloatingUI", "WaveFinishFloatingUI", true)]
public class WaveFinishFloatingUI : FloatingPopupBase
{
    [SerializeField] private TMP_Text waveFinishText;
    [SerializeField] private float fadeInDuration = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private CanvasGroup canvasGroup;

    private TaskCompletionSource<bool> fadeOutComplete;

    public override void InitializeUI()
    {
        base.InitializeUI();

        canvasGroup.alpha = 0;
        fadeOutComplete = new TaskCompletionSource<bool>();
    }

    public void UpdateWaveInfo(string waveText)
    {
        waveFinishText.text = waveText;
        PlayFadeAnimation();
    }

    private void PlayFadeAnimation()
    {
        // FadeIn ����
        canvasGroup.DOFade(1, fadeInDuration)
            .OnComplete(() => {
                // FadeIn �Ϸ� �� 2�� ����ߴٰ� FadeOut ����
                canvasGroup.DOFade(0, fadeOutDuration)
                    .SetDelay(0.8f)  // ���⿡ ���ϴ� ���� �ð�(��) ����
                    .OnComplete(() => {
                        fadeOutComplete.SetResult(true);
                    });
            });
    }

    public Task WaitForFadeOut()
    {
        return fadeOutComplete.Task;
    }

    public override void HideUI()
    {

        base.HideUI();
    }
}
