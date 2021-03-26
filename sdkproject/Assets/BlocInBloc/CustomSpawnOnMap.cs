using Mapbox.Unity.Ar;

namespace Mapbox.Examples {
    using System.Collections.Generic;
    using Mapbox.Unity.Map;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using UnityEngine;

    public class CustomSpawnOnMap : MonoBehaviour {
        [SerializeField]
        AbstractMap _map;
        Vector2d _location;

        public GameObject _spawnedObject;
        public AverageHeadingAlignmentStrategy averageHeadingAlignmentStrategy;
        public bool _isSpawn = false;
        public string _latitude;
        public string _longitude;

        public void SetLatitude (string latitude) {
            _latitude = latitude;
        }

        public void SetLongitude (string longitude) {
            _longitude = longitude;
        }

        public void Spawn () {
            _location = Conversions.StringToLatLon ($"{_latitude},{_longitude}");
             _spawnedObject.transform.position = Conversions.GeoToWorldPosition(_location, _map.CenterMercator).ToVector3xz();
            // _spawnedObject.transform.position = _map.GeoToWorldPosition (_location, true);
            // _spawnedObject.transform.rotation = averageHeadingAlignmentStrategy._targetRotation;
            _spawnedObject.SetActive (true);
            _isSpawn = true;
        }

        // private void Update () {
        //     if (!_isSpawn) {
        //         return;
        //     }
        //     _spawnedObject.transform.localPosition = _map.GeoToWorldPosition (_location, true);
        // }
    }
}