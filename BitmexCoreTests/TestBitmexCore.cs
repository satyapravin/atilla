using NUnit.Framework;
using BitmexCore.Models;
using System.ComponentModel;
using System.Collections.Generic;

namespace BitmexCoreTests
{
    class TestQueryStringParams : QueryStringParams
    {
        [DisplayName("ParamString")]
        public string ParamString { get; set; }

        [DisplayName("ParamDecimal")]
        public decimal? ParamDecimal { get; set; }
    }

    class TestQueryStringParamsWithFilter : QueryStringParamsWithFilter
    {
        [DisplayName("ParamString")]
        public string ParamString { get; set; }

        [DisplayName("ParamDecimal")]
        public decimal? ParamDecimal { get; set; }
    }

    class SomeQueryStringParams : QueryStringParams
    {
        [DisplayName("val")]
        public string Value { get; set; }
    }
    class SomeQueryBoolParams : QueryStringParams
    {
        [DisplayName("val")]
        public bool Value { get; set; }
    }
    class SomeQueryBoolNullableParams : QueryStringParams
    {
        [DisplayName("val")]
        public bool? Value { get; set; }
    }

    class WithTwoQueryStringParams : QueryStringParams
    {
        [DisplayName("val")]
        public string Value { get; set; }
        [DisplayName("val1")]
        public string Value1 { get; set; }
    }

    class SomeQueryStringParamsWithFilter : QueryStringParamsWithFilter
    {
        [DisplayName("val")]
        public string Value { get; set; }
    }

    public class TestBitmexCore
    {
        [Test]
        public void should_return_query_string_with_filter()
        {
            // arrange
            var sut = new SomeQueryStringParamsWithFilter
            {
                Filter = new Dictionary<string, string>
                {
                    {"symbol","XBTUSD" },
                    {"cnt","1" }
                },
                Value = "123"
            };

            // act
            var result = sut.ToQueryString();

            // assert
            Assert.AreEqual("val=123&filter=%7b%22symbol%22%3a%22XBTUSD%22%2c%22cnt%22%3a%221%22%7d", result);
        }
       
        [Test]
        public void should_create_params_string()
        {
            // arrange
            var sut = new SomeQueryStringParams
            {
                Value = "someValue"
            };

            // act

            var result = sut.ToQueryString();

            Assert.AreEqual("val=someValue", result);
        }

        [Test]
        public void should_create_params_string_with_two_params()
        {
            // arrange
            var sut = new WithTwoQueryStringParams
            {
                Value = "someValue",
                Value1 = "someValue1"
            };

            // act

            var result = sut.ToQueryString();

            // assert
            Assert.AreEqual("val=someValue&val1=someValue1", result);
        }

     public void should_return_bool_in_lower_case(bool input, string expected)
        {
            // arrange
            var sut = new SomeQueryBoolParams
            {
                Value = input
            };

            // act

            var result = sut.ToQueryString();

            // assert
            Assert.AreEqual($"val={expected}", result);
        }

        public void should_return_bool_nullable_in_lower_case(bool? input, string expected)
        {
            // arrange
            var sut = new SomeQueryBoolNullableParams
            {
                Value = input
            };

            // act

            var result = sut.ToQueryString();

            // assert
            Assert.AreEqual($"val={expected}", result);
        }

        [Test]
        public void TestBool()
        {
            should_return_bool_in_lower_case(true, "true");
            should_return_bool_in_lower_case(false, "false");
            should_return_bool_nullable_in_lower_case(true, "true");
            should_return_bool_nullable_in_lower_case(false, "false");
            should_return_bool_nullable_in_lower_case(null, "");
        }

        [Test]
        public void TestEnvironmentNames()
        {
            Assert.AreEqual("www.bitmex.com", Environments.Values[BitmexEnvironment.Prod]);
            Assert.AreEqual("testnet.bitmex.com", Environments.Values[BitmexEnvironment.Test]);
        }

        [Test]
        public void TestQueryStringParams()
        {
            TestQueryStringParams test = new TestQueryStringParams { ParamString = "TestString" };
            Assert.AreEqual(@"ParamString=TestString&ParamDecimal=", test.ToQueryString());
            test.ParamDecimal = 20m;
            Assert.AreEqual(@"ParamString=TestString&ParamDecimal=20", test.ToQueryString());
            TestQueryStringParamsWithFilter testFilter = new TestQueryStringParamsWithFilter
            {
                ParamString = "TestString",
                ParamDecimal = 20m,
                Filter = new Dictionary<string, string>() { { "ParamString", "Value" } }
            };
            
            Assert.AreEqual(@"ParamString=TestString&ParamDecimal=20&filter=%7b%22ParamString%22%3a%22Value%22%7d", testFilter.ToQueryString());
        }
    }
}