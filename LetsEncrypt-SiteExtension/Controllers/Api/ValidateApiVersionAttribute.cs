using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;

namespace LetsEncrypt.SiteExtension.Controllers.Api
{
    public class ValidateApiVersionAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            object apiversion = string.Empty;
            if (actionContext.ActionArguments.TryGetValue("apiversion", out apiversion))
            {
                var errorMessage = string.Empty;
                if (!TryValidateApiVersion(apiversion as string, out errorMessage))
                {
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, errorMessage);
                }
            }
        }

        private bool TryValidateApiVersion(string apiversion, out string error)
        {
            error = string.Empty;
            if (apiversion == "2017-09-01")
            {
                return true;
            }
            error = "Missing query string api-version or unsupported api-version. Only supported api-version is 2017-09-01";
            return false;

        }
    }
}