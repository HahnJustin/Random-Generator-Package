using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct MetadataEntry
{
    public int layerId;
    public int x;
    public int y;
    public FixedString64Bytes field;
    public int value;
    public int shiftX;
    public int shiftY;
    public bool inverseY;

    public void Shift(int2 shift, bool inverseY)
    {
        shiftX = shift.x;
        shiftY += shift.y;
    }

    public int GetShiftedX() => x + shiftX;

    public int GetShiftedY() => y + shiftY;
}