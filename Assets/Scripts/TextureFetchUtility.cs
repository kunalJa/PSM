using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class TextureFetchUtility
{
    public static async Task<Texture2D> FetchAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }

            Debug.LogError($"TextureFetchUtility: Failed to fetch image from {url}: {request.error}");
            return null;
        }
    }
}
