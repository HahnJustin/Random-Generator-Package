// UUIDFieldAttribute.cs (Configs asm; no reference to UserData)
using System;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [AttributeUsage(AttributeTargets.Field)]
    public class UUIDFieldAttribute : PropertyAttribute
    {
        /// Fully qualified type name, e.g. "Dalichrome.RandomGenerator.UserData.StructureTable"
        public readonly string AssetTypeName;
        /// Optional Resources subfolder(s) to narrow the search.
        public readonly string[] FoldersOverride;

        public UUIDFieldAttribute(string assetTypeName, params string[] foldersOverride)
        {
            AssetTypeName = assetTypeName;
            FoldersOverride = foldersOverride;
        }
    }
}
