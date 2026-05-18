using System;
using System.IO;

namespace WebApplication_Project1_Ecommerce.Utility
{
    public static class ProductImageHelper
    {
        public const string FallbackProductImageUrl = "/images/products/467bbcec-5054-4efc-b735-5faf550aba6c.png";
        public const string ProductImageFolder = "images/products";

        public static string NormalizeProductImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return FallbackProductImageUrl;
            }

            var normalized = imageUrl.Trim().Replace('\\', '/');

            if (normalized.StartsWith("~", StringComparison.Ordinal))
            {
                normalized = normalized[1..];
            }

            var marker = "/" + ProductImageFolder + "/";
            var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                normalized = normalized[markerIndex..];
            }

            normalized = "/" + normalized.TrimStart('/');

            return normalized == "/" ? FallbackProductImageUrl : normalized;
        }

        public static string BuildProductImageUrl(string fileName)
        {
            return "/" + ProductImageFolder + "/" + fileName;
        }

        public static string GetProductImageFilePath(string webRootPath, string imageUrl)
        {
            var normalized = NormalizeProductImageUrl(imageUrl);
            var relativePath = normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(webRootPath, relativePath);
        }

        public static bool IsCustomProductImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return false;
            }

            return !string.Equals(
                NormalizeProductImageUrl(imageUrl),
                FallbackProductImageUrl,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
