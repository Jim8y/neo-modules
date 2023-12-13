// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Network.RPC is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins
{
    partial class RpcServer
    {
        [RpcMethod]
        protected virtual JToken GetBestBlockHash(JArray parameters)
        {
            return NativeContract.Ledger.CurrentHash(_system.StoreView).ToString();
        }

        [RpcMethod]
        protected virtual JToken GetBlock(JArray parameters)
        {
            JToken key = parameters[0];
            bool verbose = parameters.Count >= 2 && parameters[1].AsBoolean();
            using var snapshot = _system.GetSnapshot();
            Block block;
            if (key is JNumber)
            {
                uint index = uint.Parse(key.AsString());
                block = NativeContract.Ledger.GetBlock(snapshot, index);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key.AsString());
                block = NativeContract.Ledger.GetBlock(snapshot, hash);
            }
            if (block == null)
                throw new RpcException(-100, "Unknown block");
            if (verbose)
            {
                JObject json = Utility.BlockToJson(block, _system.Settings);
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
                UInt256 hash = NativeContract.Ledger.GetBlockHash(snapshot, block.Index + 1);
                if (hash != null)
                    json["nextblockhash"] = hash.ToString();
                return json;
            }
            return Convert.ToBase64String(block.ToArray());
        }

        [RpcMethod]
        protected virtual JToken GetBlockHeaderCount(JArray parameters)
        {
            return (_system.HeaderCache.Last?.Index ?? NativeContract.Ledger.CurrentIndex(_system.StoreView)) + 1;
        }

        [RpcMethod]
        protected virtual JToken GetBlockCount(JArray parameters)
        {
            return NativeContract.Ledger.CurrentIndex(_system.StoreView) + 1;
        }

        [RpcMethod]
        protected virtual JToken GetBlockHash(JArray parameters)
        {
            uint height = uint.Parse(parameters[0].AsString());
            var snapshot = _system.StoreView;
            if (height <= NativeContract.Ledger.CurrentIndex(snapshot))
            {
                return NativeContract.Ledger.GetBlockHash(snapshot, height).ToString();
            }
            throw new RpcException(-100, "Invalid Height");
        }

        [RpcMethod]
        protected virtual JToken GetBlockHeader(JArray parameters)
        {
            JToken key = parameters[0];
            bool verbose = parameters.Count >= 2 && parameters[1].AsBoolean();
            var snapshot = _system.StoreView;
            Header header;
            if (key is JNumber)
            {
                uint height = uint.Parse(key.AsString());
                header = NativeContract.Ledger.GetHeader(snapshot, height);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key.AsString());
                header = NativeContract.Ledger.GetHeader(snapshot, hash);
            }
            if (header == null)
                throw new RpcException(-100, "Unknown block");

            if (verbose)
            {
                JObject json = header.ToJson(_system.Settings);
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - header.Index + 1;
                UInt256 hash = NativeContract.Ledger.GetBlockHash(snapshot, header.Index + 1);
                if (hash != null)
                    json["nextblockhash"] = hash.ToString();
                return json;
            }

            return Convert.ToBase64String(header.ToArray());
        }

        [RpcMethod]
        protected virtual JToken GetContractState(JArray parameters)
        {
            if (int.TryParse(parameters[0].AsString(), out int contractId))
            {
                var contracts = NativeContract.ContractManagement.GetContractById(_system.StoreView, contractId);
                return contracts?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
            }
            else
            {
                UInt160 scriptHash = ToScriptHash(parameters[0].AsString());
                ContractState contract = NativeContract.ContractManagement.GetContract(_system.StoreView, scriptHash);
                return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
            }
        }

        private static UInt160 ToScriptHash(string keyword)
        {
            foreach (var native in NativeContract.Contracts)
            {
                if (keyword.Equals(native.Name, StringComparison.InvariantCultureIgnoreCase) || keyword == native.Id.ToString())
                    return native.Hash;
            }

            return UInt160.Parse(keyword);
        }

        [RpcMethod]
        protected virtual JToken GetRawMemPool(JArray parameters)
        {
            bool shouldGetUnverified = parameters.Count >= 1 && parameters[0].AsBoolean();
            if (!shouldGetUnverified)
                return new JArray(_system.MemPool.GetVerifiedTransactions().Select(p => (JToken)p.Hash.ToString()));

            JObject json = new();
            json["height"] = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            _system.MemPool.GetVerifiedAndUnverifiedTransactions(
                out IEnumerable<Transaction> verifiedTransactions,
                out IEnumerable<Transaction> unverifiedTransactions);
            json["verified"] = new JArray(verifiedTransactions.Select(p => (JToken)p.Hash.ToString()));
            json["unverified"] = new JArray(unverifiedTransactions.Select(p => (JToken)p.Hash.ToString()));
            return json;
        }

        [RpcMethod]
        protected virtual JToken GetRawTransaction(JArray parameters)
        {
            UInt256 hash = UInt256.Parse(parameters[0].AsString());
            bool verbose = parameters.Count >= 2 && parameters[1].AsBoolean();
            if (_system.MemPool.TryGetValue(hash, out Transaction tx) && !verbose)
                return Convert.ToBase64String(tx.ToArray());
            var snapshot = _system.StoreView;
            TransactionState state = NativeContract.Ledger.GetTransactionState(snapshot, hash);
            tx ??= state?.Transaction;
            if (tx is null) throw new RpcException(-100, "Unknown transaction");
            if (!verbose) return Convert.ToBase64String(tx.ToArray());
            JObject json = Utility.TransactionToJson(tx, _system.Settings);
            if (state is not null)
            {
                TrimmedBlock block = NativeContract.Ledger.GetTrimmedBlock(snapshot, NativeContract.Ledger.GetBlockHash(snapshot, state.BlockIndex));
                json["blockhash"] = block.Hash.ToString();
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
                json["blocktime"] = block.Header.Timestamp;
            }
            return json;
        }

        [RpcMethod]
        protected virtual JToken GetStorage(JArray parameters)
        {
            using var snapshot = _system.GetSnapshot();
            if (!int.TryParse(parameters[0].AsString(), out int id))
            {
                UInt160 hash = UInt160.Parse(parameters[0].AsString());
                ContractState contract = NativeContract.ContractManagement.GetContract(snapshot, hash);
                if (contract is null) throw new RpcException(-100, "Unknown contract");
                id = contract.Id;
            }
            byte[] key = Convert.FromBase64String(parameters[1].AsString());
            StorageItem item = snapshot.TryGet(new StorageKey
            {
                Id = id,
                Key = key
            });
            if (item is null) throw new RpcException(-100, "Unknown storage");
            return Convert.ToBase64String(item.Value.Span);
        }

        [RpcMethod]
        protected virtual JToken FindStorage(JArray parameters)
        {
            using var snapshot = _system.GetSnapshot();
            if (!int.TryParse(parameters[0].AsString(), out int id))
            {
                UInt160 hash = UInt160.Parse(parameters[0].AsString());
                ContractState contract = NativeContract.ContractManagement.GetContract(snapshot, hash);
                if (contract is null) throw new RpcException(-100, "Unknown contract");
                id = contract.Id;
            }

            byte[] prefix = Convert.FromBase64String(parameters[1].AsString());
            byte[] prefixKey = StorageKey.CreateSearchPrefix(id, prefix);

            if (!int.TryParse(parameters[2].AsString(), out int start))
            {
                start = 0;
            }

            JObject json = new();
            JArray jarr = new();
            int pageSize = _settings.FindStoragePageSize;
            int i = 0;

            using (var iter = snapshot.Find(prefixKey).Skip(count: start).GetEnumerator())
            {
                var hasMore = false;
                while (iter.MoveNext())
                {
                    if (i == pageSize)
                    {
                        hasMore = true;
                        break;
                    }

                    JObject j = new();
                    j["key"] = Convert.ToBase64String(iter.Current.Key.Key.Span);
                    j["value"] = Convert.ToBase64String(iter.Current.Value.Value.Span);
                    jarr.Add(j);
                    i++;
                }
                json["truncated"] = hasMore;
            }

            json["next"] = start + i;
            json["results"] = jarr;
            return json;
        }

        [RpcMethod]
        protected virtual JToken GetTransactionHeight(JArray parameters)
        {
            UInt256 hash = UInt256.Parse(parameters[0].AsString());
            uint? height = NativeContract.Ledger.GetTransactionState(_system.StoreView, hash)?.BlockIndex;
            if (height.HasValue) return height.Value;
            throw new RpcException(-100, "Unknown transaction");
        }

        [RpcMethod]
        protected virtual JToken GetNextBlockValidators(JArray parameters)
        {
            using var snapshot = _system.GetSnapshot();
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _system.Settings.ValidatorsCount);
            return validators.Select(p => new JObject()
            {
                ["publickey"] = p.ToString(),
                ["votes"] = (int)NativeContract.NEO.GetCandidateVote(snapshot, p)
            }).ToArray();
        }

        [RpcMethod]
        protected virtual JToken GetCandidates(JArray parameters)
        {
            using var snapshot = _system.GetSnapshot();
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                script = sb.EmitDynamicCall(NativeContract.NEO.Hash, "getCandidates", null).ToArray();
            }
            using ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, settings: _system.Settings, gas: _settings.MaxGasInvoke);
            JObject json = new();
            try
            {
                var resultStack = engine.ResultStack.ToArray();
                if (resultStack.Length > 0)
                {
                    JArray jArray = new();
                    var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _system.Settings.ValidatorsCount);

                    foreach (var item in resultStack)
                    {
                        var value = (VM.Types.Array)item;
                        foreach (var stackItem in value)
                        {
                            var ele = (Struct)stackItem;
                            var publicKey = ele[0].GetSpan().ToHexString();
                            json["publickey"] = publicKey;
                            json["votes"] = ele[1].GetInteger().ToString();
                            json["active"] = validators.ToByteArray().ToHexString().Contains(publicKey);
                            jArray.Add(json);
                            json = new JObject();
                        }
                        return jArray;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                json["exception"] = "Invalid result.";
            }
            return json;
        }

        [RpcMethod]
        protected virtual JToken GetCommittee(JArray parameters)
        {
            return new JArray(NativeContract.NEO.GetCommittee(_system.StoreView).Select(p => (JToken)p.ToString()));
        }

        [RpcMethod]
        protected virtual JToken GetNativeContracts(JArray parameters)
        {
            return new JArray(NativeContract.Contracts.Select(p => p.NativeContractToJson(_system.Settings)));
        }
    }
}
