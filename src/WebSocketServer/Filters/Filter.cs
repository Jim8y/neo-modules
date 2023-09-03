// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Network.RPC is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable
using Neo.Json;
namespace Neo.Plugins.WebSocketServer.Filters;

public abstract class Filter
{
    public abstract Filter FromJson(JObject json);
}

// public record BlockFilter(int? Primary, uint? Since, uint? Till);
//
// public record TransactionFilter(UInt160? Sender, UInt160? Signer);
//
// public record NotificationFilter(UInt160? Contract, string? Name);
//
// public record ExecutionFilter(string? State, UInt256? Container);
