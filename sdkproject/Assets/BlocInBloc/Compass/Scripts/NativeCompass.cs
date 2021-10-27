#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace BlocInBloc {
    public class NativeCompass {
        public enum LocationStatus { NotInitialized, Initializing, Starting, Failed, Authorized, Unauthorized, Running, Stopped }

        /// <summary>
        /// Private constructor for singleton
        /// </summary>
        NativeCompass () { }

        public static NativeCompass Instance {
            get {
                if (_instance == null) {
                    _instance = new NativeCompass ();
                    _instance.Init ();
                }
                return _instance;
            }
        }
        private static NativeCompass _instance;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport ("__Internal")]
        private static extern bool InitLocationManager ();
        [DllImport ("__Internal")]
        private static extern bool RequestAuthorizationNeeded ();
        [DllImport ("__Internal")]
        private static extern void StartUpdate ();
        [DllImport ("__Internal")]
        private static extern void StopUpdate ();

        #region Heading
        [DllImport ("__Internal")]
        private static extern bool GetHeadingAvailable ();
        [DllImport ("__Internal")]
        private static extern float GetMagneticHeading ();
        [DllImport ("__Internal")]
        private static extern float GetTrueHeading ();
        [DllImport ("__Internal")]
        private static extern float GetHeadingAccuracy ();
        [DllImport ("__Internal")]
        private static extern double GetHeadingTimestamp ();
        [DllImport ("__Internal")]
        private static extern float GetMagneticField ();
        #endregion

        #region Location
        [DllImport ("__Internal")]
        private static extern bool GetLocationAvailable ();
        [DllImport ("__Internal")]
        private static extern double GetLocationLatitude ();
        [DllImport ("__Internal")]
        private static extern double GetLocationLongitude ();
        [DllImport ("__Internal")]
        private static extern double GetLocationAltitude ();
        [DllImport ("__Internal")]
        private static extern float GetLocationHorizontalAccuracy ();
        [DllImport ("__Internal")]
        private static extern float GetCourseAccuracy ();
        [DllImport ("__Internal")]
        private static extern float GetCourse ();
        [DllImport ("__Internal")]
        private static extern double GetLocationTimestamp ();
        // Values : NotInitialized, Initializing, Starting, Failed, Unauthorized, Running, Stopped, Authorized
        [DllImport ("__Internal")]
        private static extern string GetLocationStatus ();
        #endregion
        [DllImport ("__Internal")]
        private static extern string GetErrorMsg ();
#endif

        public bool isInitialized { get; private set; }

        #region Heading
        public float unityMainCameraHeading {
            get {
                Vector3 direction = Camera.main.transform.forward;
                if (Vector3.Angle (Vector3.up, direction) > 90) {
                    direction = Camera.main.transform.up;
                }
                Vector3 headingDirection = Vector3.ProjectOnPlane (direction, Vector3.up);
                return Vector3.SignedAngle (Vector3.forward, headingDirection, Vector3.up);
            }
        }

        public bool headingAvailable {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetHeadingAvailable ();
#else
                return Input.compass.enabled;
#endif
            }
        }

        public float trueHeading {
            get {
#if UNITY_EDITOR
                return unityMainCameraHeading;
#elif UNITY_IOS && !UNITY_EDITOR
                return GetTrueHeading ();
#else
                return Input.compass.trueHeading;
#endif
            }
        }

        public float magneticHeading {
            get {
#if UNITY_EDITOR
                return unityMainCameraHeading;
#elif UNITY_IOS && !UNITY_EDITOR
                return GetMagneticHeading ();
#else
                return Input.compass.magneticHeading;
#endif
            }
        }

        public float headingAccuracy {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetHeadingAccuracy ();
#else
                return Input.compass.headingAccuracy;
#endif
            }
        }

        public float magneticField {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetMagneticField ();
#else
                return Input.compass.rawVector.magnitude;
#endif
            }
        }

        public double headingTimestamp {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetHeadingTimestamp ();
#else
                return Input.compass.timestamp;
#endif
            }
        }
        #endregion

        #region Location
        public bool? locationAuthorizationNeeded {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return RequestAuthorizationNeeded ();
#elif UNITY_ANDROID
                return !Permission.HasUserAuthorizedPermission (Permission.FineLocation);
#else
                return null;
#endif
            }
        }

        public bool locationAvailable {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationAvailable ();
#else
                return !Input.location.lastData.Equals (default (LocationInfo));
#endif
            }
        }

        public double locationLatitude {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationLatitude ();
#else
                return Input.location.lastData.latitude;
#endif
            }
        }

        public double locationLongitude {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationLongitude ();
#else
                return Input.location.lastData.longitude;
#endif
            }
        }

        public double locationAltitude {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationAltitude ();
#else
                return Input.location.lastData.altitude;
#endif
            }
        }

        public float locationHorizontalAccuracy {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationHorizontalAccuracy ();
#else
                return Input.location.lastData.horizontalAccuracy;
#endif
            }
        }

        public double locationTimestamp {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetLocationTimestamp ();
#else
                return Input.location.lastData.timestamp;
#endif
            }
        }

        public float locationCourseAccuracy {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetCourseAccuracy ();
#else
                return 0f;
#endif
            }
        }

        public float locationCourse {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetCourse ();
#else
                return 0f;
#endif
            }
        }

        public LocationStatus locationStatus {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return (LocationStatus) Enum.Parse (typeof (LocationStatus), GetLocationStatus ());
#else
                return ParseStatus (Input.location.status);
#endif
            }
        }
        #endregion

        public string errorMsg {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return GetErrorMsg ();
#else
                return null;
#endif
            }
        }

        public bool Init () {
#if UNITY_IOS && !UNITY_EDITOR
            isInitialized = InitLocationManager ();
#elif UNITY_ANDROID && !UNITY_EDITOR
            isInitialized = true;
#else
            isInitialized = false;
#endif
            return isInitialized;
        }

        public void Start () {
            Debug.Log("NativeCompass Start !!");
#if UNITY_IOS && !UNITY_EDITOR
            StartUpdate ();
#else
#if UNITY_ANDROID
            if (locationAuthorizationNeeded.Value) {
                Permission.RequestUserPermission (Permission.FineLocation);
            }
#endif
            // start the location service
            Input.location.Start ();
            // enable the compass
            Input.compass.enabled = true;
            // enable the gyro
            Input.gyro.enabled = true;
#endif
        }

        public void Stop () {
#if UNITY_IOS && !UNITY_EDITOR
            StopUpdate ();
#else
            // stop the location service
            Input.location.Stop ();
            // disable the compass
            Input.compass.enabled = false;
            // disable the gyro
            Input.gyro.enabled = false;
#endif
        }

        LocationStatus ParseStatus (LocationServiceStatus locationServiceStatus) {
            LocationStatus locationStatus = LocationStatus.Stopped;
            switch (locationServiceStatus) {
                case LocationServiceStatus.Running:
                    locationStatus = LocationStatus.Running;
                    break;
                case LocationServiceStatus.Failed:
                    locationStatus = LocationStatus.Failed;
                    break;
                case LocationServiceStatus.Initializing:
                    locationStatus = LocationStatus.Initializing;
                    break;
                case LocationServiceStatus.Stopped:
                    locationStatus = LocationStatus.Stopped;
                    break;
            }
            return locationStatus;
        }
    }
}