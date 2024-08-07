using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Configs
{

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ColorAttribute : System.Attribute
    {
        public Color color;
        public ColorAttribute(Color color)
        {
            this.color = color;
        }

        public ColorAttribute(float r, float g, float b, float a)
        {
            this.color = new Color(r, g, b, a);
        }

        public ColorAttribute(string colorHex)
        {
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            this.color = color;
        }
    }
}