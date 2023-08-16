// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Plugins.RestServer is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins.RestServer.Models
{
    public class SignerModel
    {
        public IEnumerable<WitnessRuleModel> Rules { get; set; }
        public UInt160 Account { get; set; }
        public IEnumerable<UInt160> AllowedContracts { get; set; }
        public IEnumerable<ECPoint> AllowedGroups { get; set; }
        public WitnessScope Scopes { get; set; }
    }
}