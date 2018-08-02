using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class FileSystem : IFileSystem
    {
        public Task<bool> Exists(string v)
        {
            return Task.FromResult(File.Exists(v));
        }

        public Task<byte[]> Read(string v)
        {
            return Task.FromResult(File.ReadAllBytes(v));
        }

        public Task<string> ReadAllText(string v)
        {
            return Task.FromResult(File.ReadAllText(v));
        }

        public Task Write(string v, byte[] data)
        {
            File.WriteAllBytes(v, data);
            return Task.FromResult(0);
        }

        public Task WriteAllText(string v, string pemKey)
        {
            File.WriteAllText(v, pemKey);
            return Task.FromResult(0);
        }
    }
}
