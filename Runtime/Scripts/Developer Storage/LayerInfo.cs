using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LayerInfo
{
    public string layerName;
    public bool hasCollider = false;
    public int sortingOrder;
    public int tieOrder;
}