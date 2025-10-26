using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class AbstractUserData : ScriptableObject
{
    // not serialized here (your subclasses hold the id field)
    public abstract int GetId();

#if UNITY_EDITOR
    // Global Ågsomething changedÅh signal for editor caches
    public static event Action<AbstractUserData> AnyChanged;

    int _lastIdSnapshot = int.MinValue;

    protected virtual void OnValidate()
    {
        int cur = GetId();
        if (cur != _lastIdSnapshot)
        {
            _lastIdSnapshot = cur;
            AnyChanged?.Invoke(this); // tell drawers/caches to refresh
        }
    }
#endif
}
