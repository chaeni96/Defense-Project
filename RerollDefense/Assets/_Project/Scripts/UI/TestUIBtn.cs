using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUIBtn : MonoBehaviour
{
    public async void TestWildCardBtn()
    {
        
        var wildCardUI = await UIManager.Instance.ShowUI<WildCardSelectUI>();
        wildCardUI.SetWildCardDeck();

    }
}
