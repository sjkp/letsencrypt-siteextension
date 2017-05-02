using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Models
{
    class KuduModels
    {
    }
    public class DirectoryInfo
    {
        public FileOrDirectory[] Content { get; set; }
    }

    public class FileOrDirectory
    {
        public string name { get; set; }
        public int size { get; set; }
        public DateTime mtime { get; set; }
        public DateTime crtime { get; set; }
        public string mime { get; set; }
        public string href { get; set; }
        public string path { get; set; }
    }

}
