using Dalichrome.RandomGenerator.Databases;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
namespace Dalichrome.RandomGenerator
{

    public class LayerInfoGrabber : ILayerInfoGrabber
    {
        [SerializeField] private LayerDatabase database;

        public void SetDatabase(LayerDatabase database)
        {
            this.database = database;
        }

        public int GetSortingOrder(LayerType layer)
        {
            if (database == null)
            {
                return (int)layer + (layer == LayerType.Object ? -1 : 0);
            }
            return database.GetValue(layer).sortingOrder;
        }

        public int GetSortingLayerID(LayerType layer)
        {
            if (database == null)
            {
                return SortingLayer.NameToID("Default");
            }
            return SortingLayer.NameToID(database.GetValue(layer).sortingLayerName);
        }

        public int GetLayerID(LayerType layer)
        {
            if (database == null)
            {
                return LayerMask.NameToLayer("Default");
            }
            return LayerMask.NameToLayer(database.GetValue(layer).layerName);
        }

        public bool GetHasCollider(LayerType layer)
        {
            if (database == null)
            {
                return false;
            }
            return database.GetValue(layer).hasCollider;
        }
    }
}