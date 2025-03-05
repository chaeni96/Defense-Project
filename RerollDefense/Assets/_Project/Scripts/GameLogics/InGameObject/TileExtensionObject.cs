using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileExtensionObject : MonoBehaviour
{
    public SpriteRenderer tileSprite;

    private UnitController parentUnit;
    private Vector2 offsetFromParent;


    public void Initialize(UnitController parent, Vector2 offset)
    {
        parentUnit = parent;
        offsetFromParent = offset;
    }

   
}
