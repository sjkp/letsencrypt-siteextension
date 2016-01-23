using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Hosting;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ARMExplorer.Controllers
{
    public static class HyakUtils
    {


        public static string CSMUrl
        {
            get;
            set;
        }
    }
}
