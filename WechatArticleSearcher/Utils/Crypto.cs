using System.Security.Cryptography;
using System.Text;

namespace WechatArticleSearcher.Utils;

public class Crypto
{
    public static string ComputeSha256Hash(string input)
    {
        // 将输入字符串转换为字节数组
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            // 将字节数组转换为十六进制字符串
            var builder = new StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2")); // 转换为两位十六进制数

            return builder.ToString();
        }
    }
    
}