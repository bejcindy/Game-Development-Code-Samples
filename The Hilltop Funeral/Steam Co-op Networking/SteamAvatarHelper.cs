using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public static class SteamAvatarHelper
{
    public static void GetLocalUserAvatar(Image targetImage, AvatarSize size = AvatarSize.Large)
    {
        if (!SteamManager.Initialized) return;

        CSteamID steamId = SteamUser.GetSteamID();
        GetAvatarForUser(steamId, targetImage, size);
    }

    public static void GetAvatarForUser(CSteamID steamId, Image targetImage, AvatarSize size = AvatarSize.Large)
    {
        if (!SteamManager.Initialized) return;

        int avatarHandle = size switch
        {
            AvatarSize.Small => SteamFriends.GetSmallFriendAvatar(steamId),
            AvatarSize.Medium => SteamFriends.GetMediumFriendAvatar(steamId),
            AvatarSize.Large => SteamFriends.GetLargeFriendAvatar(steamId),
            _ => SteamFriends.GetLargeFriendAvatar(steamId)
        };

        if (avatarHandle <= 0) return;

        Sprite sprite = GetSteamImageAsSprite(avatarHandle);
        if (sprite != null)
        {
            targetImage.sprite = sprite;
        }
    }

    public static Sprite GetSteamImageAsSprite(int imageHandle)
    {
        Texture2D texture = GetSteamImageAsTexture(imageHandle);
        if (texture == null) return null;

        // Convert Texture2D to Sprite
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    public static Texture2D GetSteamImageAsTexture(int imageHandle)
    {
        if (!SteamUtils.GetImageSize(imageHandle, out uint width, out uint height))
            return null;

        uint imageSize = width * height * 4;
        byte[] imageData = new byte[imageSize];

        if (!SteamUtils.GetImageRGBA(imageHandle, imageData, (int)imageSize))
            return null;

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);

        // Flip vertically (Steam images are upside down)
        byte[] flippedData = new byte[imageSize];
        int rowSize = (int)(width * 4);
        for (int row = 0; row < height; row++)
        {
            System.Array.Copy(imageData, row * rowSize, flippedData, ((int)height - 1 - row) * rowSize, rowSize);
        }

        texture.LoadRawTextureData(flippedData);
        texture.Apply();

        return texture;
    }

    public enum AvatarSize { Small, Medium, Large }
}