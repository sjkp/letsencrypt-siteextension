using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace LetsEncrypt.Azure.Core.Models
{
    public class SettingEntry : INamedObject
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int Count { get; set; }
    }
}
