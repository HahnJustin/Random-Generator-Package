using System;
using Dalichrome.RandomGenerator.Configs;
using UnityEngine;

namespace Dalichrome.RandomGenerator {

    [CreateAssetMenu(menuName = "RandomGenerator/GenerationParamsObject")]
    public class GenerationParamsObject : ScriptableObject
    {
        public GenerationParams GenerationParams { get { return genParams; } set { genParams = value; } }
        [SerializeField] private GenerationParams genParams;

        public string DateCreated { get { return dateCreated; } }
        [ReadOnly, SerializeField] private string dateCreated;

        public string Version { get { return version; } }
        [ReadOnly, SerializeField] private string version;

        public GenerationParamsObject()
        {
            version = VersionController.GetVersion();
            dateCreated = DateTime.Now.ToString("yyyy/M/d H:m");
        }
    }
}
