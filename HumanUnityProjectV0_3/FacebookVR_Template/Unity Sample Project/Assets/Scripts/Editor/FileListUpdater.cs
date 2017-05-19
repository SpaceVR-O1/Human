using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;

public class FileListUpdater : MonoBehaviour 
{
	[PostProcessScene]
	private static void OnProcessScene()
	{
		Debug.Log("Processing file list");
		var sphere = FindObjectOfType<PhotoSphere>();
		var info = new DirectoryInfo(Application.streamingAssetsPath);
		sphere.FileList = info.GetFiles().Select(e => e.Name).ToArray();
	}
}
