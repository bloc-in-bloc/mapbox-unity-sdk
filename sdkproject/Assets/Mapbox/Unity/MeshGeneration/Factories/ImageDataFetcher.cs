﻿using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using UnityEngine;

public class ImageDataFetcher : DataFetcher
{
	public Action<UnityTile, RasterTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, Texture2D> TextureRecieved = (t, s) => { };
	public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public override void FetchData(DataFetcherParameters parameters)
	{
		var imageDataParameters = parameters as ImageDataFetcherParameters;
		if(imageDataParameters == null)
		{
			return;
		}

		FetchData(imageDataParameters.tilesetId, imageDataParameters.canonicalTileId, imageDataParameters.useRetina, imageDataParameters.tile);
	}

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchData(string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
		if (textureItem != null)
		{
			TextureRecieved(unityTile, textureItem.Texture2D);
			return;
		}

		//FileCacheCheck
		if (MapboxAccess.Instance.CacheManager.TextureFileExists(tilesetId, tileId)) //not in memory, check file cache
		{
			MapboxAccess.Instance.CacheManager.GetTextureItemFromFile(tilesetId, tileId, (textureCacheItem) =>
			{
				TextureRecieved(unityTile, textureCacheItem.Texture2D);

				//after returning what we already have
				//check if it's out of date, if so check server for update
				if (textureCacheItem.ExpirationDate < DateTime.Now)
				{
					CreateWebRequest(tilesetId, tileId, useRetina, textureCacheItem.ETag, unityTile);
				}
			});

			return;
		}

		//not in cache so web request
		CreateWebRequest(tilesetId, tileId, useRetina, String.Empty, unityTile);
	}

	private void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useRetina, string etag, UnityTile unityTile = null)
	{
		RasterTile rasterTile;
		if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
		{
			rasterTile = useRetina ? new RetinaRasterTile() : new RasterTile();
		}
		else if (tilesetId.StartsWith("http"))
		{
			rasterTile = new CustomRasterTile();
		}
		else
		{
			rasterTile = useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
		}

		if (unityTile != null)
		{
			unityTile.AddTile(rasterTile);
		}

		EnqueueForFetching(new FetchInfo()
		{
			TileId = tileId,
			TilesetId = tilesetId,
			RasterTile = rasterTile,
			ETag = etag,
			Callback = () => { FetchingCallback(tileId, rasterTile, unityTile); }
		});
	}

	private void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
	{
		if (unityTile != null && unityTile.CanonicalTileId != rasterTile.Id)
		{
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			return;
		}

		if (rasterTile.HasError)
		{
			FetchingError(unityTile, rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), unityTile, rasterTile.Exceptions));
		}
		else
		{
			MapboxAccess.Instance.CacheManager.AddTextureItem(
				rasterTile.TilesetId,
				rasterTile.Id,
				new TextureCacheItem()
				{
					ETag = rasterTile.ETag,
					Data = rasterTile.Data,
					ExpirationDate = rasterTile.ExpirationDate,
					Texture2D = rasterTile.Texture2D
				},
				true);

			if (rasterTile.StatusCode != 304) //NOT MODIFIED
			{
				DataRecieved(unityTile, rasterTile);
			}
		}

		if (unityTile != null)
		{
			unityTile.RemoveTile(rasterTile);
		}
	}
}

public class ImageDataFetcherParameters : DataFetcherParameters
{
	public bool useRetina = true;
}
