using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private static EventManager _instance;
    public static EventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("EventManager");
                    _instance = obj.AddComponent<EventManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    // �̺�Ʈ ID�� ���� �̺�Ʈ ��ü�� �����ϴ� Dictionary
    private Dictionary<string, IEvent> registeredEvents = new Dictionary<string, IEvent>();

    // ��� ��ü�� ��ϵ� �̺�Ʈ ���
    private Dictionary<GameObject, List<KeyValuePair<EventTriggerType, string>>> objectEvents =
        new Dictionary<GameObject, List<KeyValuePair<EventTriggerType, string>>>();

    // �̺�Ʈ ���
    public void RegisterEvent(string eventId, IEvent gameEvent)
    {
        if (!registeredEvents.ContainsKey(eventId))
        {
            registeredEvents.Add(eventId, gameEvent);
        }
        else
        {
            registeredEvents[eventId] = gameEvent;
        }
    }

    // ��ü�� �̺�Ʈ ����
    public void AssignEventToObject(GameObject obj, EventTriggerType triggerType, string eventId)
    {
        if (!objectEvents.ContainsKey(obj))
        {
            objectEvents[obj] = new List<KeyValuePair<EventTriggerType, string>>();
        }

        objectEvents[obj].Add(new KeyValuePair<EventTriggerType, string>(triggerType, eventId));
    }

    // �̺�Ʈ ����
    public void UnassignEventsFromObject(GameObject obj)
    {
        if (objectEvents.ContainsKey(obj))
        {
            objectEvents.Remove(obj);
        }
    }

    // �̺�Ʈ Ʈ����
    public void TriggerEvent(GameObject obj, EventTriggerType triggerType, Vector3 position)
    {
        if (objectEvents.TryGetValue(obj, out var events))
        {
            foreach (var eventPair in events)
            {
                if (eventPair.Key == triggerType &&
                    registeredEvents.TryGetValue(eventPair.Value, out var gameEvent))
                {
                    gameEvent.StartEvent(obj, position);
                }
            }
        }
    }

    // �̺�Ʈ �����ͷκ��� �̺�Ʈ ��ü ����
    public IEvent CreateEventFromData(D_EventDummyData eventData)
    {
        switch (eventData.f_eventType)
        {
            case EventType.SpawnEnemy:
                if (eventData is D_SpawnEnemyEventData spawnData)
                {
                    return new SpawnEnemyEvent(spawnData);
                }
                break;

                // �߰� �̺�Ʈ Ÿ�Կ� ���� ó��...
        }

        return null;
    }
}