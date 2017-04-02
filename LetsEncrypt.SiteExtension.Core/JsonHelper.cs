using Microsoft.Rest.Azure;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core
{
    public class JsonHelper
    {
        public static JsonSerializerSettings DefaultSerializationSettings
        {
            get
            {

                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    ContractResolver = (IContractResolver)new ReadOnlyJsonContractResolver(),
                    Converters = (IList<JsonConverter>)new List<JsonConverter>()
        {
          (JsonConverter) new Iso8601TimeSpanConverter()
        }
                };
                jsonSerializerSettings.Converters.Add((JsonConverter)new TransformationJsonConverter());
                jsonSerializerSettings.Converters.Add((JsonConverter)new CloudErrorJsonConverter());

                return jsonSerializerSettings;
            }
        }
    }
}
