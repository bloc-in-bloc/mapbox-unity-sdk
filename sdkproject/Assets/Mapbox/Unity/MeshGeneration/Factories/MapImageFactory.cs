namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;

	public enum MapImageType
	{
		BasicMapboxStyle,
		Custom,
		None
	}

	/// <summary>
	/// Uses raster image services to create materials & textures for terrain
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Image Factory")]
	public class MapImageFactory : AbstractTileFactory
	{
		[SerializeField]
		ImageryLayerProperties _properties;
		protected ImageDataFetcher DataFetcher;

		public ImageryLayerProperties Properties
		{
			get
			{
				return _properties;
			}
		}

		public string TilesetId
		{
			get
			{
				return _properties.sourceOptions.Id;
			}

			set
			{
				_properties.sourceOptions.Id = value;
			}
		}

		#region UnityMethods
		protected virtual void OnDestroy()
		{
			//unregister events
			if (DataFetcher != null)
			{
				DataFetcher.TextureReceived -= OnImageRecieved;
				DataFetcher.FetchingError -= OnDataError;
			}
		}
		#endregion

		#region DataFetcherEvents
		private void OnImageRecieved(UnityTile tile, RasterTile rasterTile)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);

				if (tile.RasterDataState != TilePropertyState.Unregistered)
				{
					if (rasterTile.Texture2D != null)
					{
						tile.SetRasterTexture(rasterTile);
					}
					else
					{
						tile.SetRasterData(rasterTile, _properties.rasterOptions.useMipMap, _properties.rasterOptions.useCompression);
					}
				}
			}

			rasterTile.Clear();
		}

		//merge this with OnErrorOccurred?
		protected virtual void OnDataError(UnityTile tile, RasterTile rasterTile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				if (tile.RasterDataState != TilePropertyState.Unregistered)
				{
					tile.RasterDataState = TilePropertyState.Error;
					_tilesWaitingResponse.Remove(tile);
					OnErrorOccurred(e);
				}

			}
		}
		#endregion

		#region AbstractFactoryOverrides
		protected override void OnInitialized()
		{
			DataFetcher = new ImageDataFetcher();
			DataFetcher.TextureReceived += OnImageRecieved;
			DataFetcher.FetchingError += OnDataError;
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ImageryLayerProperties)options;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (_properties.sourceType == ImagerySourceType.None)
			{
				tile.SetRasterData(null);
				tile.RasterDataState = TilePropertyState.None;
				return;
			}
			else
			{
				tile.RasterDataState = TilePropertyState.Loading;

				var parent = tile.UnwrappedTileId.Parent;
				for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
				{
					var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(TilesetId, parent.Canonical);
					if (cacheItem != null && cacheItem.Texture2D != null)
					{
						tile.SetParentTexture(parent, cacheItem.Texture2D);
						break;
					}

					parent = parent.Parent;
				}

				if (_properties.sourceType != ImagerySourceType.Custom)
				{
					_properties.sourceOptions.layerSource = MapboxDefaultImagery.GetParameters(_properties.sourceType);
				}
				ImageDataFetcherParameters parameters = new ImageDataFetcherParameters()
				{
					canonicalTileId = tile.CanonicalTileId,
					tile = tile,
					tilesetId = TilesetId,
					useRetina = _properties.rasterOptions.useRetina
				};
				DataFetcher.FetchData(parameters);
			}
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(UnityTile tile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
			if (tile != null)
			{
				tile.RasterDataState = TilePropertyState.Error;
				foreach (var exception in e.Exceptions)
				{
					Debug.Log(exception);
				}

				;
			}
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			DataFetcher.CancelFetching(tile.UnwrappedTileId, TilesetId);
			if (_tilesWaitingResponse != null && _tilesWaitingResponse.Contains(tile))
			{
				_tilesWaitingResponse.Remove(tile);
			}
		}

		public override void Clear()
		{
			//DestroyImmediate(DataFetcher);
		}

		protected override void OnPostProcess(UnityTile tile)
		{

		}

		public override void UnbindEvents()
		{
			base.UnbindEvents();
		}

		protected override void OnUnbindEvents()
		{
		}
		#endregion
	}
}
