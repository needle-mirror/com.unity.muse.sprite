using System;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Unity.Muse.Sprite.Common.Backend
{
    internal static class BackendUtilities
    {
        public static UnityWebRequest SendRequest<T>(string url, T data, Action<UnityWebRequest> onDone, string method = "POST")
        {
            var request = new UnityWebRequest(url, method);
            if (method == "POST")
            {
                string requestJSON = JsonUtility.ToJson(data);
                request.SetRequestHeader("content-type", "application/json; charset=UTF-8");
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJSON));
                request.uploadHandler.contentType = "application/json";
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            AsyncOperation operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    onDone(request);
                }
                finally
                {
                    request.uploadHandler?.Dispose();
                    request.downloadHandler?.Dispose();
                    request.Dispose();
                }
            };
            return request;
        }

        public static string ConvertTextureToPNGBase64(Texture2D t)
        {
            if (t == null)
                return "";
            if (!t.isReadable)
                t = CreateTemporaryDuplicate(t, t.width, t.height);
            var s = System.Convert.ToBase64String(t.EncodeToPNG());
            return s;
        }

        public static Texture2D CreateTemporaryDuplicate(Texture2D original, int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            //if (!ShaderUtil.hardwareSupportsRectRenderTexture || !(bool)(UnityEngine.Object)original)
            if (original == null)
                return (Texture2D)null;
            RenderTexture active = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
            Graphics.Blit((UnityEngine.Texture)original, temporary);
            RenderTexture.active = temporary;
            bool flag = width >= SystemInfo.maxTextureSize || height >= SystemInfo.maxTextureSize;
            Texture2D temporaryDuplicate = new Texture2D(width, height, format, original.mipmapCount > 1 || flag);
            temporaryDuplicate.ReadPixels(new Rect(0.0f, 0.0f, (float)width, (float)height), 0, 0);
            temporaryDuplicate.Apply();
#if UNITY_EDITOR
            temporaryDuplicate.alphaIsTransparency = original.alphaIsTransparency;
#endif
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return temporaryDuplicate;
        }

        public static Texture2D SaveTexture2DToFile(string fileName, Texture2D texture)
        {
#if UNITY_EDITOR
            if (texture != null)
            {
                if (!texture.isReadable)
                    texture = CreateTemporaryDuplicate(texture, texture.width, texture.height);
                var savedLocation = SaveBytesToFile(fileName, texture.EncodeToPNG());
                return AssetDatabase.LoadAssetAtPath<Texture2D>(savedLocation);
            }
#endif
            return null;
        }

        public static string SaveBytesToFile(string fileName, byte[] bytes)
        {
#if UNITY_EDITOR
            var f = AssetDatabase.GenerateUniqueAssetPath(fileName);
            File.WriteAllBytes(f, bytes);
            AssetDatabase.ImportAsset(f, ImportAssetOptions.Default);
            return f;
#else
            return string.Empty;
#endif

        }

        public static void LogFile(string filename, string log)
        {
#if UNITY_EDITOR
            File.AppendAllText(filename, log);
#endif
        }

        public static Texture2D SpriteAsTexture(UnityEngine.Sprite sprite)
        {
            var texture = sprite.texture;
            Matrix4x4 transform = Matrix4x4.identity;
            var uvs = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
            Vector2[] vertices = sprite.vertices;
            var triangles = sprite.triangles;
            Vector2 pivot = sprite.pivot;
            var spriteWidth = sprite.rect.width;
            var spriteHeight = sprite.rect.height;

            var restoreRT = RenderTexture.active;
            var renderTexture = new RenderTexture((int)sprite.rect.width, (int)sprite.rect.height, 0, RenderTextureFormat.ARGB32);

            RenderTexture.active = renderTexture;
            var temporary = RenderTexture.GetTemporary(renderTexture.descriptor);
            var copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
            copyMaterial.mainTexture = texture;
            copyMaterial.mainTextureScale = Vector2.one;
            copyMaterial.mainTextureOffset = Vector2.zero;
            copyMaterial.SetPass(0);
            GL.Clear(true, true, new Color(1f, 1f, 1f, 0f));
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            Color color = Color.white;
            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;
            for (int i = 0; i < triangles.Length; ++i)
            {
                ushort index = triangles[i];
                Vector3 vertex = vertices[index];
                vertex = transform.MultiplyPoint(vertex);
                Vector2 uv = uvs[index];
                GL.Color(color);
                GL.TexCoord(new Vector3(uv.x, uv.y, 0));
                GL.Vertex3((vertex.x * pixelsToUnits + pivot.x) / spriteWidth, (vertex.y * pixelsToUnits + pivot.y) / spriteHeight, 0);
            }
            GL.End();
            GL.PopMatrix();

            Texture2D copy = new Texture2D((int)spriteWidth, (int)spriteHeight, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.filterMode = texture != null ? texture.filterMode : FilterMode.Point;
            copy.anisoLevel = texture != null ? texture.anisoLevel : 0;
            copy.wrapMode = texture != null ? texture.wrapMode : TextureWrapMode.Clamp;
            copy.ReadPixels(new Rect(0, 0, spriteWidth, spriteHeight), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(temporary);

            RenderTexture.active = restoreRT;
            copyMaterial.SafeDestroy();
            renderTexture.SafeDestroy();
            return copy;
        }
    }
}