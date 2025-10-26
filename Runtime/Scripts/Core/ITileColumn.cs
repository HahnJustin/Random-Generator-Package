using Unity.Mathematics;

public interface ITileColumn
{
    int Length { get; }
    int this[int index] { get; }
    int X { get; }
    int Y { get; }
    bool IsValid { get; }
    int2 Int2 { get; }
}
