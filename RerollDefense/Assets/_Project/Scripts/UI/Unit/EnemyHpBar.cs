using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBar : MonoBehaviour
{
    [SerializeField] private Image hpFillImage;

    public void UpdateHP(float ratio)
    {
        hpFillImage.fillAmount = ratio;
    }
}
