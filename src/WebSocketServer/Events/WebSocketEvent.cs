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
namespace Neo.Plugins.WebSocketServer.Events;

public abstract class WebSocketEvent
{
    public WssEventId WssEvent { get; set; }
    public JObject Data { get; set; }
}
