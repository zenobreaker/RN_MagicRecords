using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
    : Singleton<ResourceManager>
{
    private Dictionary<string, Sprite> spriteCache = new();

    public Sprite GetSprite(string address)
    {
        if (spriteCache.TryGetValue(address, out var sprite))
            return sprite;

        sprite = Resources.Load<Sprite>(address);

        spriteCache[address] = sprite;

        return sprite;
    }
}