using NUnit.Framework;
using System.Net.Http;
using Moq;
using System.Threading.Tasks;
using Moq.Protected;
using System.Threading;
using System.Net;
using BitmexRESTApi;
using BitmexCore;
using Microsoft.Extensions.Logging.Abstractions;
using BitmexCore.Models;
using System.Collections.Generic;
using BitmexCore.Dtos;
using System;
using Newtonsoft.Json;
using TestsBase;

namespace BitmexRestApiTests
{
    public class TestBitmexRestApi
    {
        [Test]
        public void TestGetOrder()
        {
            var testOrderDtoOne = OrderTestBase.GetDummyOrder();
            string orderText = JsonConvert.SerializeObject(new List<OrderDto> { testOrderDtoOne });

            var msgHandler = RestTestBase.GetMockHttpHandler();
            RestTestBase.SetupForResponse(msgHandler, new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(orderText)
            }); 

            var auth = new BitmexAuthorization()
            {
                Key = "testKey", Secret = "testSecret", BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test
            };
            var bitmexApiSvc = BitmexApiService.CreateDefaultApi(auth, msgHandler.Object, new NullLoggerFactory());
            var param = new OrderGETRequestParams
            {
                Filter = new Dictionary<string, string>
            {
                { "open", "true" }
            }
            };
            var task = bitmexApiSvc.Execute(BitmexApiUrls.Order.GetOrder, param);
            
            Assert.AreEqual(task.Result.Result.Count, 1);
            OrderTestBase.Compare(testOrderDtoOne, task.Result.Result[0]);
        }

        [Test]
        public void TestPostOrder()
        {
            var param = new OrderPOSTRequestParams
            {
                OrderQty = 100,
                ExecInst = "ParticipateDoNotInitiate",
                OrdType = "Limit",
                Price = 200.15m,
                Side = "Buy",
                Symbol = "ETH"
            };

            var testOrderDtoOne = OrderTestBase.GetDummyOrder();
            string orderText = JsonConvert.SerializeObject(testOrderDtoOne);

            var msgHandler = RestTestBase.GetMockHttpHandler();
            RestTestBase.SetupForResponse(msgHandler, new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(orderText)
            });

            var auth = new BitmexAuthorization()
            {
                Key = "testKey",
                Secret = "testSecret",
                BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test
            };
            var bitmexApiSvc = BitmexApiService.CreateDefaultApi(auth, msgHandler.Object, new NullLoggerFactory());

            var task = bitmexApiSvc.Execute(BitmexApiUrls.Order.PostOrder, param);
            OrderTestBase.Compare(testOrderDtoOne, task.Result.Result);
        }

        [Test]
        public void TestPutOrder()
        {
            var testOrderDtoOne = OrderTestBase.GetDummyOrder();
            string orderText = JsonConvert.SerializeObject( testOrderDtoOne );

            var msgHandler = RestTestBase.GetMockHttpHandler();
            RestTestBase.SetupForResponse(msgHandler, new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(orderText)
            });

            var auth = new BitmexAuthorization()
            {
                Key = "testKey",
                Secret = "testSecret",
                BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test
            };
            var bitmexApiSvc = BitmexApiService.CreateDefaultApi(auth, msgHandler.Object, new NullLoggerFactory());

            var param = new OrderPUTRequestParams
            {
                OrderID = "order1",
                OrderQty = 100,
                Price = 201.1m,
            };

            var task = bitmexApiSvc.Execute(BitmexApiUrls.Order.PutOrder, param);
            OrderTestBase.Compare(testOrderDtoOne, task.Result.Result);
        }

        struct Error
        {
            public string message { get { return "message"; } set { message = value; } }
            public string name { get { return "name"; } set { name = value; } }
        }
        struct Message
        {
            public Error error { get; set;} 
        }
    
        [Test]
        public void TestDeleteOrder()
        {
            var msg = new Message();
            msg.error = new Error();

            string errorText = JsonConvert.SerializeObject(msg);

            var msgHandler = RestTestBase.GetMockHttpHandler();
            RestTestBase.SetupForResponse(msgHandler, new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(errorText)
            });

            var auth = new BitmexAuthorization()
            {
                Key = "testKey",
                Secret = "testSecret",
                BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test
            };
            var bitmexApiSvc = BitmexApiService.CreateDefaultApi(auth, msgHandler.Object, new NullLoggerFactory());

            var param = new OrderDELETERequestParams
            {
                OrderID = "order1"
            };

            var task = bitmexApiSvc.Execute(BitmexApiUrls.Order.DeleteOrder, param);
            Assert.That( () => task.Result, Throws.TypeOf<AggregateException>());
        }


        [Test]
        public void TestGetPosition()
        {
            var msgHandler = RestTestBase.GetMockHttpHandler();
            var auth = new BitmexAuthorization()
            {
                Key = "testKey",
                Secret = "testSecret",
                BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test
            };

            var bitmexApiSvc = BitmexApiService.CreateDefaultApi(auth, msgHandler.Object, new NullLoggerFactory());

            var testPositionDtoOne = PositionTestBase.GetDummyPosition();
            string posText = JsonConvert.SerializeObject(new List<PositionDto> { testPositionDtoOne });


            RestTestBase.SetupForResponse(msgHandler, new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(posText)
            });

            var param = new PositionGETRequestParams
            {
                Filter = new Dictionary<string, string>
                {   
                    { "symbol", "ETHUSD" }
                }
            };

            var task = bitmexApiSvc.Execute(BitmexApiUrls.Position.GetPosition, param);

            Assert.AreEqual(task.Result.Result.Count, 1);
            PositionTestBase.Compare(testPositionDtoOne, task.Result.Result[0]);
        }

    }
}