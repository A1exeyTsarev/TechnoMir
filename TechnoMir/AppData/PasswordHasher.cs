using System.Security.Cryptography;
using System.Text;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Хеширование паролей. В таблице Users хранится не пароль,
    /// а его SHA-256 в виде 64 шестнадцатеричных символов (заглавные буквы).
    /// </summary>
    public static class PasswordHasher
    {
        public static string GetHash(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                var sb = new StringBuilder(64);
                foreach (byte b in bytes)
                    sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }

        /// <summary>Совпадает ли введённый пароль с сохранённым хешем.</summary>
        public static bool Verify(string password, string storedHash)
            => storedHash != null && GetHash(password) == storedHash.ToUpperInvariant();
    }
}
