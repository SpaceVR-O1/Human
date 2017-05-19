using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SpriteRetriever : MonoBehaviour
{
	public static SpriteRetriever Instance;
	public int MaxSimultaneous;
	public Action<Texture2D> OnTextureLoaded;

	private Queue<string> _downloadQueue = new Queue<string>();
	private int _inProgress = 0;

	private void Awake()
	{
		Instance = this;
	}

	public void RetrieveTexture(string url)
	{
		lock (_downloadQueue)
		{
			_downloadQueue.Enqueue(url);
		}
	}

	private System.Collections.IEnumerator LoadImage(string url)
	{		
		Debug.Log("Loading image: " + url);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
		var directory = "file://" + Path.Combine(Application.streamingAssetsPath, url);
#elif UNITY_ANDROID
		var directory = Path.Combine(Application.streamingAssetsPath, url);
#endif
		Debug.Log("Getting file: " + directory);
		WWW www = new WWW(directory);
		www.threadPriority = ThreadPriority.Low;
		yield return www;
		var tex = www.texture;
		tex.name = url;
		if (OnTextureLoaded != null) OnTextureLoaded(tex);
		_inProgress--;
	}

	private void Update()
	{		
		while (_inProgress < MaxSimultaneous && _downloadQueue.Count > 0)
		{
			lock (_downloadQueue)
			{
				var newEntry = _downloadQueue.Dequeue();
				StartCoroutine(LoadImage(newEntry));
				_inProgress++;
			}
		}
	}
}
