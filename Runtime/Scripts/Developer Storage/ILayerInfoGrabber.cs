using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public interface ILayerInfoGrabber
    {
        public int GetSortingOrder(LayerType layer);

        public int GetSortingLayerID(LayerType layer);

        public int GetLayerID(LayerType layer);

        public string GetTag(LayerType layer);

        public bool GetHasCollider(LayerType layer);

        public bool GetUseCompositeCollider(LayerType layer);

        public Material GetMaterial(LayerType layer);
    }
}
