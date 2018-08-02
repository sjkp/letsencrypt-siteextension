using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public interface IFileSystem
    {
        Task<bool> Exists(string v);
        Task WriteAllText(string v, string pemKey);
        Task<string> ReadAllText(string v);

        Task<byte[]> Read(string v);
        Task Write(string v, byte[] data);
    }
}