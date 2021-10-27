namespace Mapbox.Unity.Location
{
	/// <summary>
	/// Wrapper to use Unity's LocationInfo as MapboxLocationInfo
	/// </summary>
	public struct MapboxLocationInfoUnityWrapper : IMapboxLocationInfo
	{

		public MapboxLocationInfoUnityWrapper(double latitude, double longitude, double altitude, float horizontalAccuracy, float verticalAccuracy, double timestamp)
		{
			this._latitude = latitude;
			this._longitude = longitude;
			this._altitude = altitude;
			this._horizontalAccuracy = horizontalAccuracy;
			this._verticalAccuracy = verticalAccuracy;
			this._timestamp = timestamp;
		}

		private double _latitude;
		private double _longitude;
		private double _altitude;
		private float _horizontalAccuracy;
		private float _verticalAccuracy;
		private double _timestamp;


		public double latitude { get { return _latitude; } }

		public double longitude { get { return _longitude; } }

		public double altitude { get { return _altitude; } }

		public float horizontalAccuracy { get { return _horizontalAccuracy; } }

		public float verticalAccuracy { get { return _verticalAccuracy; } }

		public double timestamp { get { return _timestamp; } }
	}
}
