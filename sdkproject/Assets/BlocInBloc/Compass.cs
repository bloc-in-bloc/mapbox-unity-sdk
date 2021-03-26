using System;
using Mapbox.Examples;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour {
    public Text magneticNorth;
    public Text trueNorth;
    public Text interference;
    public Text distance;

    public Image magneticNorthImg;
    public Image trueNorthImg;
    public CustomSpawnOnMap customSpawn;

    void Update () {
        magneticNorth.text = $"Magnetic North: {Input.compass.magneticHeading}";
        trueNorth.text = $"True North: {Input.compass.trueHeading}";
        interference.text = Input.compass.rawVector.magnitude > 60 ? $"Interference: <color=red>{ Input.compass.rawVector.magnitude }</color>" : $"Interference: <color=green>{ Input.compass.rawVector.magnitude }</color>";

        // read the magnetometer / compass value and generate a quaternion
        Quaternion trueHeading = Quaternion.Euler (0, 0, Input.compass.trueHeading);
        // interpolate between old and new position
        trueNorthImg.transform.localRotation = Quaternion.Slerp (trueNorthImg.transform.localRotation, trueHeading, 0.05f);

        // read the magnetometer / compass value and generate a quaternion
        Quaternion magneticHeading = Quaternion.Euler (0, 0, Input.compass.magneticHeading);
        // interpolate between old and new position
        magneticNorthImg.transform.localRotation = Quaternion.Slerp (trueNorthImg.transform.localRotation, magneticHeading, 0.05f);

        if (customSpawn._isSpawn) {
            distance.text = $"Distance to spawn: {Convert.ToString (Vector3.Distance (Camera.main.transform.position, customSpawn._spawnedObject.transform.position))}";
        }
    }
}