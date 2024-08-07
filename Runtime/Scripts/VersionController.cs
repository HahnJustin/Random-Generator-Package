using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VersionController
{
    readonly private static string version = "0.1";

    public static string GetVersion()
    {
        return version;
    }
}
