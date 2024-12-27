using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlacedObject : BasicObject
{

    //��ġ�� ������Ʈ���� �ʿ��Ѱ� -> Ŭ�������� �˾�â ���;ߵ�

    //ex) 2x2�� ������ �� ��ϸ� �����ص� Ŭ��â�� ���;��ϰ� ������ �� 4���� �����Ǿ����

    //�ʿ��Ѱ� tileShpaeData�ε� �̸����� �����ϸ� �Ȱ��� ����� �� ������Ե�
    //������ ����� ����ִ� ��ϵ鸸 �����ؾߵ�
    //������ �޾ƿö� 

    public TMP_Text unitNameText;

    public string tileUniqueID;

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

            unitNameText.text = unitData.f_name;
        }

    }

  
    //���� tileshpae�� ���� ������ �������ϼ������Ƿ� ������ ���� ID �ο�
    public void RegistereTileID(string uniqueID)
    {
        tileUniqueID = uniqueID;

    }
}
