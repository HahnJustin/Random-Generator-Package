using System;
using Unity.Collections;
using Unity.Mathematics;

[Serializable]
public struct MetadataEntry
{
    public int layerId;
    public int x;
    public int y;
    public FixedString64Bytes field;
    public float value;
    public int shiftX;
    public int shiftY;
    public bool inverseY;

    public int3 Int3 { get { return new int3(x, y, layerId); } }

    public void Shift(int2 shift, bool inverseY)
    {
        shiftX = shift.x;
        shiftY += shift.y;
    }

    public int GetShiftedX() => x + shiftX;

    public int GetShiftedY() => y + shiftY;
}