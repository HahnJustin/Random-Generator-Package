using System;
using Dalichrome.RandomGenerator.Configs;
using UnityEngine;

namespace Dalichrome.RandomGenerator {

    [CreateAssetMenu(menuName = "RandomGenerator/GenerationParamsObject")]
    public class GenerationParamsObject : ScriptableObject
    {
        public string Name { get { return name; } set { name = value; }}
        [SerializeField] private new string name;

        public GenerationParams GenerationParams { get { return genParams; } set { genParams = value; } }
        [SerializeField] private GenerationParams genParams;

        public string DateCreated { get { return dateCreated; } }
        [ReadOnly, SerializeField] private string dateCreated;

        public string Version { get { return version; } }
        [ReadOnly, SerializeField] private string version;

        public GenerationParamsObject(string _name = null)
        {
            version = VersionController.GetVersion();
            dateCreated = DateTime.Now.ToString("yyyy/M/d H:m");

            if(name != null)
            {
                name = _name;
            }
            else
            {
                name = "Gen " + dateCreated;
            }
        }
    }
}
