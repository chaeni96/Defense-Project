using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PreviewUnitObject : BasicObject
{

    //��ġ�� ������Ʈ���� �ʿ��Ѱ� -> Ŭ�������� �˾�â ���;ߵ�

    //ex) 2x2�� ������ �� ��ϸ� �����ص� Ŭ��â�� ���;��ϰ� ������ �� 4���� �����Ǿ����

    //�ʿ��Ѱ� tileShpaeData�ε� �̸����� �����ϸ� �Ȱ��� ����� �� ������Ե�
    //������ ����� ����ִ� ��ϵ鸸 �����ؾߵ�
    //������ �޾ƿö� 

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
