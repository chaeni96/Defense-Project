using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : StaticObject
{

    //��ġ�� ������Ʈ���� �ʿ��Ѱ� -> Ŭ�������� �˾�â ���;ߵ�

    //ex) 2x2�� ������ �� ��ϸ� �����ص� Ŭ��â�� ���;��ϰ� ������ �� 4���� �����Ǿ����

    //�ʿ��Ѱ� tileShpaeData�ε� �̸����� �����ϸ� �Ȱ��� ����� �� ������Ե�
    //������ ����� ����ִ� ��ϵ鸸 �����ؾߵ�
    //������ �޾ƿö� 

    public string tileUniqueID;

    public override void Initialize()
    {
        base.Initialize();

    }
    public override void Update()
    {
        base.Update();
    }

   
    public void RegistereTileID(string uniqueID)
    {
        tileUniqueID = uniqueID;

    }
}
