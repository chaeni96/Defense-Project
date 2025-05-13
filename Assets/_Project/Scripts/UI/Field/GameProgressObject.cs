using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameProgressObject : MonoBehaviour
{
    [SerializeField] private Image iconImage; // ���̺� ������
    [SerializeField] private Image bgImage; // ���̺� ������

    [SerializeField] private GameObject activeIcon; // ���� Ȱ��ȭ�� ���̺� ǥ�� ������

    // ���̺� Ÿ�Կ� ���� ������ ��������Ʈ ����
    [SerializeField] private Sprite normalWaveSprite;
    [SerializeField] private Sprite bossWaveSprite;
    [SerializeField] private Sprite eventWaveSprite;
    [SerializeField] private Sprite wildCardWaveSprite;
    [SerializeField] private Sprite huntingWaveSprite;

    [SerializeField] private Color activeColor = Color.white; // Ȱ��ȭ�� ������ ���� 
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f); // ��Ȱ��ȭ ����

    // �ʱ�ȭ - ���̺� ������ ������� ������ ����
    public void Initialize(WaveBase wave, bool isActive)
    {
        SetIcon(wave);
        SetActive(isActive);
    }

    // ���̺� Ÿ�Կ� ���� ������ ����
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

    // ���� Ȱ��ȭ�� ���̺� ǥ��
    public void SetActive(bool isActive)
    {
        if (activeIcon != null)
            activeIcon.SetActive(isActive);


        // ������ ���� ����
        if (iconImage != null && bgImage != null)
        {
            iconImage.color = isActive ? activeColor : inactiveColor;
            bgImage.color = isActive ? activeColor : inactiveColor;
        }
    }
}
