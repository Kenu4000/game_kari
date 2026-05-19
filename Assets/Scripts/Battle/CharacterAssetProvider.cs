using System;
using UnityEngine;

namespace GameKari.Battle
{
    public static class CharacterAssetProvider
    {
        private const string CharacterAssetBasePath = "Battle/Characters/";

        public static CharacterData CreateCharacterDataById(string characterId)
        {
            CharacterData asset = LoadCharacterAsset(characterId);
            if (asset != null)
            {
                return asset;
            }

            throw new InvalidOperationException($"CharacterData asset not found for id: {characterId}");
        }

        private static CharacterData LoadCharacterAsset(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            return Resources.Load<CharacterData>(CharacterAssetBasePath + characterId);
        }
    }
}




