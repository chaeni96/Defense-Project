using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IDraggable
{
    void OnDragStart();
    void OnDragUpdate();
    void OnDragEnd();
}
