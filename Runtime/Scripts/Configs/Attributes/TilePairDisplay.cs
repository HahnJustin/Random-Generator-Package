// Runtime assembly (NOT inside an Editor folder)
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class TilePairDisplayAttribute : Attribute
{
    public readonly bool ShowKeyDropdown;
    public readonly bool ShowValueDropdown;

    public TilePairDisplayAttribute(bool showKey = true, bool showValue = true)
    {
        ShowKeyDropdown = showKey;
        ShowValueDropdown = showValue;
    }
}
