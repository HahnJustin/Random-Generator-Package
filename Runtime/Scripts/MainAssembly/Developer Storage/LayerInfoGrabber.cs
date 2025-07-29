using Dalichrome.RandomGenerator.Databases;
using Dalichrome.RandomGenerator.Core;
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

            string tempName = database.GetValue(layer).layerName;
            if (string.IsNullOrEmpty(tempName))
            {
                tempName = "Default";
            }

            int layerID = LayerMask.NameToLayer(tempName);
            if( layerID == -1)
            {
                Debug.LogError($"LayerType {layer}'s layer name defined in LayerDatabase '{tempName}' does not exist");
            }
            return LayerMask.NameToLayer(tempName);
        }

        public string GetTag(LayerType layer)
        {
            if (database == null)
            {
                return "Untagged";
            }
            return database.GetValue(layer).tag;
        }

        public bool GetHasCollider(LayerType layer)
        {
            if (database == null)
            {
                return false;
            }
            return database.GetValue(layer).hasCollider;
        }

        public bool GetUseCompositeCollider(LayerType layer)
        {
            if (database == null) return false;
            return database.GetValue(layer).useCompositeCollider;
        }

        public Material GetMaterial(LayerType layer)
        {
            if (database == null)
            {
                return null;
            }
            return database.GetValue(layer).material;
        }
    }
}