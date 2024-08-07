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
    }
}