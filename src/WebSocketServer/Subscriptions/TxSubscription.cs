// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Network.RPC is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Plugins.WebSocketServer.Events;
using Neo.Plugins.WebSocketServer.Filters;
namespace Neo.Plugins.WebSocketServer.Subscriptions;

public class TxSubscription : Subscription
{
    public UInt256? TxId { get; set; }

    public override Subscription FromJson(JObject json)
    {
        TxId = UInt256.Parse(json["txid"]?.GetString());
        Filter = CreateFilterFromJson<TransactionFilter>(json);
        WssEvent = WssEventId.TransactionEventId;
        return this;
    }
}
