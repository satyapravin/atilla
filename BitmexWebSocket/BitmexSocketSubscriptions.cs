﻿using BitmexCore.Dtos;
using BitmexWebSocket.Dtos.Socket;
using BitmexWebSocket.Models.Socket;
using System;
using System.Collections.Generic;

namespace BitmexWebSocket
{
    public static class BitmexSocketSubscriptions
    {
        public static BitmexApiSubscriptionInfo<IEnumerable<InstrumentDto>> CreateInstrumentSubsription(Action<BitmexSocketDataMessage<IEnumerable<InstrumentDto>>> act, object[] symbols)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<InstrumentDto>>.Create(SubscriptionType.instrument, act).WithArgs(symbols);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<OrderBook10Dto>> CreateOrderBook10Subsription(Action<BitmexSocketDataMessage<IEnumerable<OrderBook10Dto>>> act, object[] symbols)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<OrderBook10Dto>>.Create(SubscriptionType.orderBook10, act).WithArgs(symbols);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<OrderBookDto>> CreateOrderBookL2Subsription(Action<BitmexSocketDataMessage<IEnumerable<OrderBookDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<OrderBookDto>>.Create(SubscriptionType.orderBookL2, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<OrderBookDto>> CreateOrderBookL2_25Subsription(Action<BitmexSocketDataMessage<IEnumerable<OrderBookDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<OrderBookDto>>.Create(SubscriptionType.orderBookL2_25, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<OrderDto>> CreateOrderSubsription(Action<BitmexSocketDataMessage<IEnumerable<OrderDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<OrderDto>>.Create(SubscriptionType.order, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<PositionDto>> CreatePositionSubsription(Action<BitmexSocketDataMessage<IEnumerable<PositionDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<PositionDto>>.Create(SubscriptionType.position, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<TradeDto>> CreateTradeSubsription(Action<BitmexSocketDataMessage<IEnumerable<TradeDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<TradeDto>>.Create(SubscriptionType.trade, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>> CreateTradeBucket1MSubsription(Action<BitmexSocketDataMessage<IEnumerable<TradeBucketedDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>>.Create(SubscriptionType.tradeBin1m, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>> CreateTradeBucket5MSubsription(Action<BitmexSocketDataMessage<IEnumerable<TradeBucketedDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>>.Create(SubscriptionType.tradeBin5m, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>> CreateTradeBucket1HSubsription(Action<BitmexSocketDataMessage<IEnumerable<TradeBucketedDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>>.Create(SubscriptionType.tradeBin1h, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>> CreateTradeBucket1DSubsription(Action<BitmexSocketDataMessage<IEnumerable<TradeBucketedDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<TradeBucketedDto>>.Create(SubscriptionType.tradeBin1d, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<LiquidationDto>> CreateLiquidationSubsription(Action<BitmexSocketDataMessage<IEnumerable<LiquidationDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<LiquidationDto>>.Create(SubscriptionType.liquidation, act);
        }

        public static BitmexApiSubscriptionInfo<IEnumerable<ExecutionDto>> CreateExecutionSubsription(Action<BitmexSocketDataMessage<IEnumerable<ExecutionDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<ExecutionDto>>.Create(SubscriptionType.execution, act);
        }

        /// <summary>
        /// Updates on your current account balance and margin requirements
        /// </summary>
        /// <param name="act">Your Action when socket get data</param>
        /// <returns>Margin Subscription info</returns>
        public static BitmexApiSubscriptionInfo<IEnumerable<MarginDto>> CreateMarginSubscription(Action<BitmexSocketDataMessage<IEnumerable<MarginDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<MarginDto>>.Create(SubscriptionType.margin, act);
        }


        /// <summary>
        /// Bitcoin address balance data, including total deposits & withdrawals
        /// </summary>
        /// <param name="act">Your Action when socket get data</param>
        /// <returns>Wallet Subscription info</returns>
        public static BitmexApiSubscriptionInfo<IEnumerable<WalletDto>> CreateWalletSubscription(Action<BitmexSocketDataMessage<IEnumerable<WalletDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<WalletDto>>.Create(SubscriptionType.wallet, act);
        }

        /// <summary>
        /// Bitcoin address balance data, including total deposits & withdrawals
        /// </summary>
        /// <param name="act">Your Action when socket get data</param>
        /// <returns>Funding Subscription info</returns>
        public static BitmexApiSubscriptionInfo<IEnumerable<FundingDto>> CreateFundingSubscription(Action<BitmexSocketDataMessage<IEnumerable<FundingDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<FundingDto>>.Create(SubscriptionType.funding, act);
        }

        /// <summary>
        /// Site announcements
        /// </summary>
        /// <param name="act">Your Action when socket get data</param>
        /// <returns>Announcement Subscription info</returns>
        public static BitmexApiSubscriptionInfo<IEnumerable<AnnouncementDto>> CreateAnnouncementSubscription(Action<BitmexSocketDataMessage<IEnumerable<AnnouncementDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<AnnouncementDto>>.Create(SubscriptionType.announcement, act);
        }

        /// <summary>
        /// Trollbox chat
        /// </summary>
        /// <param name="act">Your Action when socket get data</param>
        /// <returns>Chat Subscription info</returns>
        public static BitmexApiSubscriptionInfo<IEnumerable<ChatDto>> CreateChatSubscription(Action<BitmexSocketDataMessage<IEnumerable<ChatDto>>> act)
        {
            return BitmexApiSubscriptionInfo<IEnumerable<ChatDto>>.Create(SubscriptionType.chat, act);
        }
    }
}
