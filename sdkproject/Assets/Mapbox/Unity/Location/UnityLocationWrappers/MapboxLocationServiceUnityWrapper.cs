namespace Mapbox.Unity.Location
{


	using BlocInBloc;
	using UnityEngine;


	/// <summary>
	/// Wrap Unity's LocationService into MapboxLocationService
	/// </summary>
	public class MapboxLocationServiceUnityWrapper : IMapboxLocationService
	{

		public bool isEnabledByUser { get { return true; } }


		public LocationServiceStatus status
		{
			get
			{
				switch (NativeCompass.Instance.locationStatus)
				{
					case NativeCompass.LocationStatus.Starting:
					case NativeCompass.LocationStatus.Initializing:
						return LocationServiceStatus.Initializing;
					case NativeCompass.LocationStatus.Failed:
						return LocationServiceStatus.Failed;
					case NativeCompass.LocationStatus.Running:
						return LocationServiceStatus.Running;
					default:
						return LocationServiceStatus.Stopped;
				}
			}
		}


		public IMapboxLocationInfo lastData { get { return new MapboxLocationInfoUnityWrapper (NativeCompass.Instance.locationLatitude, NativeCompass.Instance.locationLongitude, NativeCompass.Instance.locationAltitude, NativeCompass.Instance.locationHorizontalAccuracy, 0f, NativeCompass.Instance.locationTimestamp); } }


		public void Start (float desiredAccuracyInMeters, float updateDistanceInMeters)
		{
			NativeCompass.Instance.Start ();
			// Input.location.Start (desiredAccuracyInMeters, updateDistanceInMeters);
		}


		public void Stop ()
		{
			NativeCompass.Instance.Stop ();
			// Input.location.Stop ();
		}
	}
}
