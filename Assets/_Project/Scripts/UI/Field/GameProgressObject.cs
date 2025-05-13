using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameProgressObject : MonoBehaviour
{
    [SerializeField] private Image iconImage; // 웨이브 아이콘
    [SerializeField] private Image bgImage; // 웨이브 아이콘

    [SerializeField] private GameObject activeIcon; // 현재 활성화된 웨이브 표시 아이콘

    // 웨이브 타입에 따른 아이콘 스프라이트 매핑
    [SerializeField] private Sprite normalWaveSprite;
    [SerializeField] private Sprite bossWaveSprite;
    [SerializeField] private Sprite eventWaveSprite;
    [SerializeField] private Sprite wildCardWaveSprite;
    [SerializeField] private Sprite huntingWaveSprite;

    [SerializeField] private Color activeColor = Color.white; // 활성화된 아이콘 색상 
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 비활성화 색상

    // 초기화 - 웨이브 데이터 기반으로 아이콘 설정
    public void Initialize(WaveBase wave, bool isActive)
    {
        SetIcon(wave);
        SetActive(isActive);
    }

    // 웨이브 타입에 따라 아이콘 변경
    public void SetIcon(WaveBase wave)
    {
        if (iconImage == null) return;

        if (wave is NormalBattleWave)
            iconImage.sprite = normalWaveSprite;
        else if (wave is BossBattleWave)
            iconImage.sprite = bossWaveSprite;
        else if (wave is EventEnemyWave)
            iconImage.sprite = eventWaveSprite;
        else if (wave is WildcardWave)
            iconImage.sprite = wildCardWaveSprite;
        else if (wave is HuntingSelectTimeWave)
            iconImage.sprite = huntingWaveSprite;
    }

    // 현재 활성화된 웨이브 표시
    public void SetActive(bool isActive)
    {
        if (activeIcon != null)
            activeIcon.SetActive(isActive);


        // 아이콘 색상 변경
        if (iconImage != null && bgImage != null)
        {
            iconImage.color = isActive ? activeColor : inactiveColor;
            bgImage.color = isActive ? activeColor : inactiveColor;
        }
    }
}
