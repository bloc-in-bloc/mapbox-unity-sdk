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
        public AbstractMap _map;
        public Vector2d _location {
            get {
                return Conversions.StringToLatLon ($"{_latitude},{_longitude}");
            }
        }

        public Vector2 worldLocation {
            get {
                return Conversions.GeoToWorldPosition (_location, _map.CenterMercator).ToVector3xz ();
            }
        }

        public GameObject _spawnedObject;
        public GameObject north;
        public Bib.Compass compass;
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
            _spawnedObject.transform.position = _map.GeoToWorldPosition (_location, false);
            _spawnedObject.transform.rotation = north.transform.rotation;
            _spawnedObject.SetActive (true);
            _isSpawn = true;
        }
    }
}