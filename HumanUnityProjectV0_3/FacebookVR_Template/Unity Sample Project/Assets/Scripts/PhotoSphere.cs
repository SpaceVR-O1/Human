using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class PhotoSphere : MonoBehaviour
{
	[HideInInspector] public string[] FileList;
	private List<Texture2D> _cubemaps = new List<Texture2D>();
	private int _currentIndex = -1;
	private Material _sphereMaterial;

	private void Start() 
	{
		_sphereMaterial = GetComponent<MeshRenderer>().sharedMaterial;
		GatherImages();
		StartCoroutine(WaitOnFirstImage());
	}

	private IEnumerator WaitOnFirstImage()
	{
		while (_cubemaps.Count == 0)
		{
			yield return null;
		}
		DisplayNextImage();
	}

	private void Update()
	{
		if (Input.anyKeyDown || OVRInput.GetDown(OVRInput.Button.One)) DisplayNextImage();
	}

	private void GatherImages()
	{
		var list = FileList;
		SpriteRetriever.Instance.OnTextureLoaded += OnTextureLoaded;
		foreach (var entry in list)
		{
			var fileType = entry.Substring(entry.Length - 3);
			if (fileType != "png" && fileType != "jpg") continue;
			SpriteRetriever.Instance.RetrieveTexture(entry);
		}
	}

	private void DisplayNextImage()
	{
		_currentIndex++;
		if (_currentIndex >= _cubemaps.Count) _currentIndex = 0;

		if (_cubemaps.Count > _currentIndex)
		{
			Debug.Log("Displaying image: " + _cubemaps[_currentIndex].name);
			_sphereMaterial.SetTexture("_Tex", _cubemaps[_currentIndex]);
		}
	}

	private void OnTextureLoaded(Texture2D tex)
	{
		_cubemaps.Add(tex);
	}
}
