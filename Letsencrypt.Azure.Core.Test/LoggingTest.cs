using LetsEncrypt.Azure.Core.V2;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class LoggingTest
    {
        [TestMethod]
        public async Task TestLogging()
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
            .AddConsole()            
            .AddDebug();
            var logger = loggerFactory.CreateLogger<AcmeClient>();
            logger.LogInformation("Initial message");
         
            var client = new AcmeClientTest(logger);
            await client.TestEndToEndAzure();
        }      
    }
}
