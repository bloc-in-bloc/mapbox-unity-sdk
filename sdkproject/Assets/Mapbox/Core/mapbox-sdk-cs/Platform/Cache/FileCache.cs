using Mapbox.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace Mapbox.Platform.Cache
{
	public class FileCache
	{
		private static string CacheRootFolderName = "FileCache";
		public static string PersistantCacheRootFolderPath = Path.Combine(Application.persistentDataPath, CacheRootFolderName);
		private static string FileExtension = ".png";

		public Action<string, CanonicalTileId, TextureCacheItem> FileSaved = (tilesetName, tileId, cacheItem) => { };
		private Dictionary<string, string> MapIdToFolderNameDictionary;

		public FileCache()
		{
			_cachedResponses = new Dictionary<string, CacheItem>();
			_infosToSave = new Queue<InfoWrapper>();
			_infoKeys = new HashSet<string>();
			MapIdToFolderNameDictionary = new Dictionary<string, string>();
			Runnable.Run(FileScheduler());

			if (!Directory.Exists(PersistantCacheRootFolderPath))
			{
				Directory.CreateDirectory(PersistantCacheRootFolderPath);
			}
		}

#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif
		private uint _maxCacheSize;
		private object _lock = new object();
		private Dictionary<string, CacheItem> _cachedResponses;

		private Queue<InfoWrapper> _infosToSave;
		private HashSet<string> _infoKeys;

		private int _lastFileSaveFrame = 0;
		private string DataSerializationCulture = "en-US";

		public uint MaxCacheSize
		{
			get { return _maxCacheSize; }
		}

		public CacheItem Get(string tilesetId, CanonicalTileId tileId)
		{
			string key = tilesetId + "||" + tileId;

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
			UnityEngine.Debug.LogFormat("{0} {1}", methodName, key);
#endif

			if (!_cachedResponses.ContainsKey(key))
			{
				return null;
			}

			return _cachedResponses[key];
		}

		public bool Exists(string mapId, CanonicalTileId tileId)
		{
			string filePath = Path.Combine(PersistantCacheRootFolderPath, MapIdToFolderName(mapId) + "/" + TileIdToFileName(tileId) + FileExtension);
			return File.Exists(filePath);
		}

		public void Clear(string tilesetId)
		{
			lock (_lock)
			{
				tilesetId += "||";
				List<string> toDelete = _cachedResponses.Keys.Where(k => k.Contains(tilesetId)).ToList();
				foreach (string key in toDelete)
				{
					_cachedResponses.Remove(key);
				}
			}
		}

		public void Add(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
		{
			var key = tileId.GenerateKey(tilesetId);
			if (_infoKeys.Contains(key))
			{
				if(Debug.isDebugBuild) Debug.Log(string.Format("This image file ({0}) is already queued for saving. Removing (and destroying) first instance, adding new one.", key));
				//we can't find the first info object here in O(n) but we are removing it from _infoKeys
				//so we can find it and destroy the texture inside on update below
				_infoKeys.Remove(key);
			}

			_infoKeys.Add(key);
			var infoWrapper = new InfoWrapper(key, tilesetId, tileId, textureCacheItem);
			_infosToSave.Enqueue(infoWrapper);
		}

		private IEnumerator FileScheduler()
		{
			while (true)
			{
				if (_infosToSave.Count > 0)
				{
					var info = _infosToSave.Dequeue();
					if (_infoKeys.Contains(info.Key))
					{
						_infoKeys.Remove(info.Key);
						SaveInfo(info);
					}
					else
					{
						//this object was removed from queue, probably updates
						//texture inside should be destroyed
						GameObject.Destroy(info.TextureCacheItem.Texture2D);
						info.TextureCacheItem.Data = null;
						if(Debug.isDebugBuild) Debug.Log("Destroying the first copy of the same tile in file save queue");
					}
				}

				yield return null;
			}
		}

		private void SaveInfo(InfoWrapper info)
		{
			if (info.TextureCacheItem == null || info.TextureCacheItem.Data == null)
			{
				return;
			}

			string folderPath = Path.Combine(PersistantCacheRootFolderPath, MapIdToFolderName(info.MapId));

			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			info.TextureCacheItem.FilePath = Path.GetFullPath(Path.Combine(folderPath, TileIdToFileName(info.TileId) + FileExtension));

			using (FileStream sourceStream = new FileStream(info.TextureCacheItem.FilePath,
				FileMode.Create, FileAccess.Write, FileShare.Read,
				bufferSize: 4096, useAsync: false))
			{
				sourceStream.Write(info.TextureCacheItem.Data, 0, info.TextureCacheItem.Data.Length);
			}

			//We probably shouldn't delay this. It will only cause problems and it should be fast enough anyway
			FileSaved(info.MapId, info.TileId, info.TextureCacheItem);
		}

		public void GetAsync(string mapId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
		{
			string filePath = Path.Combine(PersistantCacheRootFolderPath, MapIdToFolderName(mapId) + "/" + TileIdToFileName(tileId));

			if (File.Exists(filePath + FileExtension))
			{
				Runnable.Run(LoadImageCoroutine(mapId, tileId, filePath, callback));
			}
		}

		private IEnumerator LoadImageCoroutine(string mapId, CanonicalTileId tileId, string filePath, Action<TextureCacheItem> callback)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file:///" + filePath + FileExtension))
			{
				yield return uwr.SendWebRequest();

				if (uwr.isNetworkError || uwr.isHttpError)
				{
					Debug.LogErrorFormat(uwr.error);
				}
				else
				{
					var textureCacheItem = new TextureCacheItem();
					textureCacheItem.Texture2D = DownloadHandlerTexture.GetContent(uwr);
					textureCacheItem.Texture2D.wrapMode = TextureWrapMode.Clamp;
					textureCacheItem.FilePath = filePath;

					callback(textureCacheItem);
				}
			}
		}

		private string MapIdToFolderName(string mapId)
		{
			if (MapIdToFolderNameDictionary.ContainsKey(mapId))
			{
				return MapIdToFolderNameDictionary[mapId];
			}
			var folderName = mapId;
			var chars = Path.GetInvalidFileNameChars();
			foreach (Char c in chars)
			{
				folderName = folderName.Replace(c, '-');
			}
			MapIdToFolderNameDictionary.Add(mapId, folderName);
			return folderName;
		}

		private string TileIdToFileName(CanonicalTileId tileId)
		{
			return tileId.Z.ToString() + "_" + tileId.X + "_" + tileId.Y;
		}

		public void Clear()
		{
			if (_cachedResponses != null)
			{
				_cachedResponses.Clear();
			}
			else
			{
				_cachedResponses = new Dictionary<string, CacheItem>();
			}
		}

		private void ClearFolder(string folderPath)
		{
			DirectoryInfo di = new DirectoryInfo(folderPath);

			foreach (FileInfo file in di.GetFiles())
			{
				file.Delete();
			}
		}

		public void ClearStyle(string style)
		{
			ClearFolder(Path.Combine(PersistantCacheRootFolderPath, style));
		}

		public void ClearAll()
		{
			DirectoryInfo di = new DirectoryInfo(PersistantCacheRootFolderPath);

			foreach (DirectoryInfo folder in di.GetDirectories())
			{
				ClearFolder(folder.FullName);
			}
		}

		private class InfoWrapper
		{
			public string Key;
			public string MapId;
			public CanonicalTileId TileId;
			public TextureCacheItem TextureCacheItem;

			public InfoWrapper(string key, string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem)
			{
				Key = key;
				MapId = mapId;
				TileId = tileId;
				TextureCacheItem = textureCacheItem;
			}
		}

		public void DeleteTileFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		public HashSet<string> GetFileList()
		{
			var _pathList = new HashSet<string>();
			if (Directory.Exists(FileCache.PersistantCacheRootFolderPath))
			{
				var dir = Directory.GetDirectories(FileCache.PersistantCacheRootFolderPath);
				foreach (var rasterDirectory in dir)
				{
					var directoryInfo = new DirectoryInfo(rasterDirectory);
					var files = directoryInfo.GetFiles();
					foreach (var fileInfo in files)
					{
						_pathList.Add(fileInfo.FullName);
					}
				}
			}

			return _pathList;
		}
	}
}
