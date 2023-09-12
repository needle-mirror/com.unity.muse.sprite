using System;
using Unity.Muse.Sprite.Common.Backend;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    static class Utilities
    {
        public static readonly string emptyGUID = Guid.Empty.ToString();
        const string k_TempGUIDPrefix = "StyleTrainerTempGUID-";

        public static bool ValidStringGUID(string s)
        {
            return !string.IsNullOrEmpty(s) && s != emptyGUID;
        }

        public static string CreateTempGuid()
        {
            return $"{k_TempGUIDPrefix}{Guid.NewGuid()}";
        }

        public static bool IsTempGuid(string guid)
        {
            return guid.StartsWith(k_TempGUIDPrefix);
        }

        public static Texture2D placeHolderTexture => DuplicateResourceTexture("Unity.Muse.StyleTrainer/Images/placeholder");
        public static Texture2D errorTexture => DuplicateResourceTexture("Images/SpriteGenerateError");
        public static Texture2D forbiddenTexture => DuplicateResourceTexture("Unity.Muse.StyleTrainer/Images/forbidden");

        static Texture2D DuplicateResourceTexture(string path)
        {
            var t = Resources.Load<Texture2D>(path);
            return BackendUtilities.CreateTemporaryDuplicate(t, t.width, t.height);
        }

        public static bool ByteArraysEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }
    }
}