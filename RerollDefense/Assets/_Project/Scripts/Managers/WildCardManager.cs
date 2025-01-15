using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildCardManager : MonoBehaviour
{
    private static WildCardManager _instance;

    public static WildCardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WildCardManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("WildCardManager");
                    _instance = singleton.AddComponent<WildCardManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ���ϵ� ī�峻�� ��� ȿ�� ����
  
    // ���� �ν��� ����
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null) return;

        foreach (var buffData in cardData.f_BuffData)
        {
            // ������ ��� Subject�� ���� ����
            BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject);
        }
    }


  
    public void CleanUp()
    {
        // �ʿ��� ���� �۾�
    }


}
