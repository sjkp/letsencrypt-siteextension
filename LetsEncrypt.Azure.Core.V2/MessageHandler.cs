using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public abstract class MessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            var requestInfo = string.Format("{0} {1}, headers {2}", request.Method, request.RequestUri, string.Join(",", request.Headers
                .Where(s => !string.Equals(s.Key, "Authorization", StringComparison.InvariantCultureIgnoreCase))
                .Select(s => $"{s.Key} = {string.Join("|", s.Value)}")
                ));

            byte[] requestMessage = null;
            if (request.Content != null)
            {
                requestMessage = await request.Content.ReadAsByteArrayAsync();
            }

            await IncommingMessageAsync(corrId, requestInfo, requestMessage);

            var response = await base.SendAsync(request, cancellationToken);

            byte[] responseMessage = null;
            if (response.Content != null)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
            }
           
            await OutgoingMessageAsync(corrId, requestInfo, responseMessage);

            return response;
        }


        protected abstract Task IncommingMessageAsync(string correlationId, string requestInfo, byte[] message);
        protected abstract Task OutgoingMessageAsync(string correlationId, string requestInfo, byte[] message);
    }



    public class MessageLoggingHandler : MessageHandler
    {
        private readonly ILogger logger;

        public MessageLoggingHandler(ILogger logger)
        {
            this.logger = logger;
        }
        protected override async Task IncommingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            await Task.Run(() =>
                logger.LogInformation(string.Format("{0} - Request: {1}\r\n{2}", correlationId, requestInfo, message != null ? Encoding.UTF8.GetString(message) : String.Empty)));
        }


        protected override async Task OutgoingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            await Task.Run(() =>
                logger.LogInformation(string.Format("{0} - Response: {1}\r\n{2}", correlationId, requestInfo, message != null ? Encoding.UTF8.GetString(message) : String.Empty)));
        }
    }
}
