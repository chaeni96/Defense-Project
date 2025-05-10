using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitItemSlotObject : MonoBehaviour
{

    [SerializeField] private SpriteRenderer itemIcon;



    public void InitItemIcon(Sprite icon)
    {
        itemIcon.sprite = icon;
    }



}
