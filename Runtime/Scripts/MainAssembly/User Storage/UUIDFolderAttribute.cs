// UUIDFolderAttribute.cs (any runtime asm)
using System;

namespace Dalichrome.RandomGenerator.UserData
{
    /// Annotate UUID assets with their Resources subfolders.
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class UUIDFolderAttribute : Attribute
    {
        public readonly string[] Folders;
        public UUIDFolderAttribute(params string[] folders) => Folders = folders;
    }
}
