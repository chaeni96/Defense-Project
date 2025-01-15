using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PreviewUnitObject : BasicObject
{

    //배치된 오브젝트에서 필요한거 -> 클릭했을때 팝업창 나와야됨

    //ex) 2x2의 유닛중 한 블록만 선택해도 클릭창이 나와야하고 삭제시 총 4개가 삭제되어야함

    //필요한건 tileShpaeData인데 이름으로 판정하면 똑같은 블록이 다 사라지게됨
    //선택한 블록이 들어있는 블록들만 제거해야됨
    //데이터 받아올때 

    //public TMP_Text unitNameText;

    private SpriteRenderer sprite;

    public override void Initialize()
    {
        base.Initialize();

    }
 
    public void InitializeObject(string tileShapeName, int tileIndex)
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileIndex < tileShapeData.f_unitBuildData.Count)
        {
            var buildData = tileShapeData.f_unitBuildData[tileIndex];
            var unitData = buildData.f_unitData;
        }

    }

}
