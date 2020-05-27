using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestsBase
{
    public class RestTestBase
    {
        public static Mock<HttpMessageHandler> GetMockHttpHandler()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            return handlerMock;
        }

        public static void SetupForResponse(Mock<HttpMessageHandler> handler, HttpResponseMessage msg)
        {
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(msg).Verifiable();
        }
    }
}
