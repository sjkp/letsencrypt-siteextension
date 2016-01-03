using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public interface INamedObject
    {
        string Name { get; set; }
    }
}