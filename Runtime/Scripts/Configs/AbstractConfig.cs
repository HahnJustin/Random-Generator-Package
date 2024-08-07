using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public abstract class AbstractConfig
    {
        [Hidden] public virtual Enum Type { get; }

        public virtual string Name { get; set; }

        protected string _name = "";

        [Hidden] public StringType DescriptionStringType { get { return _description; } }

        protected StringType _description = StringType.NA;

        [Hidden] public virtual bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        [SerializeField] private bool _enabled = true;

        public override bool Equals(object obj)
        {
            if (obj is not AbstractConfig config) return false;

            string thisJson = JsonUtility.ToJson(this);
            string otherJson = JsonUtility.ToJson(config);

            return string.Equals(thisJson, otherJson);
        }
    }
}