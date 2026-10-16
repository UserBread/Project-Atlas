using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class DatasetLoader
{
    public static IEnumerator LoadGeoJsonFromStreamingAssets(
        string fileName,
        Action<string> onLoaded,
        Action<string> onError = null)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        string json = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(www.error);
                yield break;
            }
            json = www.downloadHandler.text;
        }
#else
        if (!File.Exists(path))
        {
            onError?.Invoke("GeoJSON file not found: " + path);
            yield break;
        }
        json = File.ReadAllText(path);
#endif

        onLoaded?.Invoke(json);
    }
}
