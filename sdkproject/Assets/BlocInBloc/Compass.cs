using System;
using BlocInBloc;
using Mapbox.Examples;
using Mapbox.Unity.Ar;
using Mapbox.Unity.Location;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Bib {
    public class Compass : MonoBehaviour {
        public Text magneticNorth;
        public Text trueNorth;
        public Text interference;
        public Text distance;
        public Text rotation;
        public Text gps;
        public GameObject north;
        public Text targetLocation;
        public Text targetLocationWorld;

        public Image magneticNorthImg;
        public Image trueNorthImg;
        public Image gpsNorthImg;
        public CustomSpawnOnMap customSpawn;
        public SimpleAutomaticSynchronizationContextBehaviour contextBehaviour;

        void Awake () {
            LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += OnLocationUpdated;
            contextBehaviour.OnAlignmentAvailable += OnAlignmentAvailable;
        }

        public void Init () {
            targetLocation.text = $"TargetLocation: { customSpawn._location }";
            targetLocationWorld.text = $"TargetLocationWorld: { customSpawn.worldLocation }";
        }

        void Update () {
            magneticNorth.text = $"Magnetic North: {NativeCompass.Instance.magneticHeading}";
            trueNorth.text = $"True North: {NativeCompass.Instance.trueHeading}";
            // interference.text = Input.compass.rawVector.magnitude > 60 ? $"Interference: <color=red>{ Input.compass.rawVector.magnitude }</color>" : $"Interference: <color=green>{ Input.compass.rawVector.magnitude }</color>";

            // read the magnetometer / compass value and generate a quaternion
            Quaternion trueHeading = Quaternion.Euler (0, 0, NativeCompass.Instance.trueHeading);
            // interpolate between old and new position
            trueNorthImg.transform.localRotation = Quaternion.Slerp (trueNorthImg.transform.localRotation, trueHeading, 0.05f);

            // read the magnetometer / compass value and generate a quaternion
            Quaternion magneticHeading = Quaternion.Euler (0, 0, NativeCompass.Instance.magneticHeading);
            // interpolate between old and new position
            magneticNorthImg.transform.localRotation = Quaternion.Slerp (magneticNorthImg.transform.localRotation, magneticHeading, 0.05f);

            if (customSpawn._isSpawn) {
                distance.text = $"Distance to spawn: {Convert.ToString (Vector3.Distance (Camera.main.transform.position, customSpawn._spawnedObject.transform.position))}";
            }

            float gpsNorthDirection = Vector3.SignedAngle (north.transform.forward, Vector3.ProjectOnPlane (Camera.main.transform.up, Vector3.up), Vector3.up);
            gpsNorthImg.transform.localRotation = Quaternion.Slerp (gpsNorthImg.transform.localRotation, Quaternion.Euler (0, 0, gpsNorthDirection), 0.05f);
        }

        void OnLocationUpdated (Location location) {
            var latitudeLongitude = location.LatitudeLongitude;
            gps.text = string.Format (
                "Location[{0:yyyyMMdd-HHmmss}]: {1},{2}\tAccuracy: {3}\tHeading: {4}"
                , UnixTimestampUtils.From (location.Timestamp)
                , latitudeLongitude.x
                , latitudeLongitude.y
                , location.Accuracy
                , location.UserHeading
            );

            // Mapbox.Unity.Utilities.Console.Instance.Log ($"Compute: {customSpawn._location} || {Conversions.GeoToWorldPosition (customSpawn._location, customSpawn._map.CenterMercator).ToVector3xz ()}", "green");
        }

        void OnAlignmentAvailable (Mapbox.Unity.Ar.Alignment alignment) {
            rotation.text = $"Rotation: {alignment.Rotation.ToString ()}";
            north.transform.localRotation = Quaternion.Euler (0, alignment.Rotation, 0);
        }
    }
}