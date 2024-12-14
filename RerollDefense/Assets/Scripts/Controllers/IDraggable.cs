using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IDraggable : IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    public void OnDragBegin();
    public void OnDrag();

    public void OnDragUp();
}
