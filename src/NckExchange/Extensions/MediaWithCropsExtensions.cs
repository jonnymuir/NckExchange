using Umbraco.Cms.Core.Models;

namespace NckExchange.Extensions;

public static class MediaWithCropsExtensions
{
    public static string GetAltText(this MediaWithCrops mediaItem, string altTextAlias = "altText")
    {
        return mediaItem.Value<string>(altTextAlias) ?? string.Empty;
    }
}