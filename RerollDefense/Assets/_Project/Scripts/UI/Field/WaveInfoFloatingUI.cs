using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[UIInfo("WaveInfoFloatingUI", "WaveInfoFloatingUI", false)]
public class WaveInfoFloatingUI : FloatingPopupBase
{

    [SerializeField] private TMP_Text waveTimeInfoText;
    [SerializeField] private TMP_Text spawnEnemyInfoText;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;

    public override void InitializeUI()
    {
        base.InitializeUI();

        canvasGroup.alpha = 0;
    }

    public void UpdateWaveInfo(string waveText, string enemyText)
    {
        waveTimeInfoText.text = waveText;
        spawnEnemyInfoText.text = enemyText;
        FadeIn();
    }

    private void FadeIn()
    {
        canvasGroup.DOFade(1, fadeInDuration);
    }

    public override void HideUI()
    {
        canvasGroup.DOFade(0, fadeOutDuration).OnComplete(() => base.HideUI());

    }
}
