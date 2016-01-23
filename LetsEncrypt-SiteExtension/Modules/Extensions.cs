using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace ARMExplorer.Modules
{
    public static class Extensions
    {
        public static HttpWebResponse GetResponseWithoutExceptions(this HttpWebRequest request)
        {
           try
            {
                return (HttpWebResponse) request.GetResponse();
            }
            catch (WebException e)
            {
                return (HttpWebResponse) e.Response;
            }
        }
    }
}