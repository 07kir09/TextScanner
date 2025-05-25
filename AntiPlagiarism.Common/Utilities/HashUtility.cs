using System.Security.Cryptography;
namespace AntiPlagiarism.Common.Utilities
{
    public static class HashUtility
    {
        public static async Task<string> ComputeSha256Hash(Stream content)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(content);
            
            // Сбрасываем позицию потока, чтобы его можно было использовать снова
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}