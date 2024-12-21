using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullWindowInGameDlg : UIBase
{

    //TODO : UIBase 상속받아야됨

    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;
    public GameObject fourthCardDeck;



    //상점에서 확률 가지고 와서 카드 덱 4개 설치 
    public override void InitializeUI()
    {
        base.InitializeUI();
    }


}
