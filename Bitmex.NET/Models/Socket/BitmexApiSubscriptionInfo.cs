using Bitmex.NET.Dtos.Socket;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Bitmex.NET.Models.Socket
{
    public abstract class BitmexApiSubscriptionInfo
    {
        public string SubscriptionName { get; }

        public object[] Args { get; protected set; }

        public object[] SubscriptionWithArgs
        {
            get
            {
                if (Args != null && Args.Length > 0)
                {
                    for (int ii=0; ii < Args.Length; ++ii)
                    {
                        Args[ii] = SubscriptionName + ":" + Args[ii];
                    }

                    return Args;
                }
                else
                {
                    return new object[] { SubscriptionName };
                }
            }
        }

        protected BitmexApiSubscriptionInfo(string subscriptionName)
        {
            SubscriptionName = subscriptionName;
        }

        public abstract void Execute(JToken data, BitmexActions action);
    }

    public class BitmexApiSubscriptionInfo<TResult> : BitmexApiSubscriptionInfo
        where TResult : class
    {
        public Action<BitmexSocketDataMessage<TResult>> Act { get; }

        public BitmexApiSubscriptionInfo(SubscriptionType subscriptionType, Action<BitmexSocketDataMessage<TResult>> act) : base(Enum.GetName(typeof(SubscriptionType), subscriptionType))
        {
            Act = act;
        }

        public BitmexApiSubscriptionInfo<TResult> WithArgs(params object[] args)
        {
            Args = args;
            return this;
        }

        public static BitmexApiSubscriptionInfo<TResult> Create(SubscriptionType subscriptionType, Action<BitmexSocketDataMessage<TResult>> act)
        {
            return new BitmexApiSubscriptionInfo<TResult>(subscriptionType, act);
        }

        public override void Execute(JToken data, BitmexActions action)
        {
            Act?.Invoke(new BitmexSocketDataMessage<TResult>(action, data.ToObject<TResult>()));
        }

    }
}
