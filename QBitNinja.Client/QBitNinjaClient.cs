﻿using NBitcoin;
using System.Linq;
using Newtonsoft.Json;
using QBitNinja.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NBitcoin.DataEncoders;

namespace QBitNinja.Client
{
    public class KeySetClient
    {
        private readonly QBitNinjaClient _Client;
        public QBitNinjaClient Client
        {
            get
            {
                return _Client;
            }
        }

        private readonly WalletClient _Wallet;
        public WalletClient Wallet
        {
            get
            {
                return _Wallet;
            }
        }

        private readonly string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
        }
        public KeySetClient(WalletClient walletClient, string keySet)
        {
            if (keySet == null)
                throw new ArgumentNullException("keySet");
            if (walletClient == null)
                throw new ArgumentNullException("walletClient");
            _Name = keySet;
            _Wallet = walletClient;
            _Client = _Wallet.Client;
        }

        public Task<HDKeySet> Create(ExtPubKey[] keys, int signatureCount = 1, KeyPath path = null)
        {
            return Client.CreateKeySet(Wallet.Name, Name, keys, signatureCount, path);
        }

        public Task<HDKeySet> Create(HDKeySet keyset)
        {
            keyset.Name = Name;
            return Client.CreateKeySet(Wallet.Name, keyset);
        }

        public Task<bool> CreateIfNotExists(ExtPubKey[] keys, int signatureCount = 1, KeyPath path = null)
        {
            return Client.CreateKeySetIfNotExists(Wallet.Name, Name, keys, signatureCount, path);
        }

        public Task<bool> CreateIfNotExists(HDKeySet keyset)
        {
            keyset.Name = Name;
            return Client.CreateKeySetIfNotExists(Wallet.Name, keyset);
        }

        public Task<HDKeyData> GenerateKey()
        {
            return Client.GenerateKey(Wallet.Name, Name);
        }

        public Task<bool> Delete()
        {
            return Client.DeleteKeySet(Wallet.Name, Name);
        }
    }
    public class WalletClient
    {
        public WalletClient(QBitNinjaClient client, string walletName)
        {
            if (walletName == null)
                throw new ArgumentNullException("walletName");
            if (client == null)
                throw new ArgumentNullException("client");
            Client = client;
            Name = walletName;
        }

        public string Name
        {
            get;
            private set;
        }
        public QBitNinjaClient Client
        {
            get;
            private set;
        }

        public Task<WalletModel> Create()
        {
            return Client.CreateWallet(Name);
        }
        public Task<bool> CreateIfNotExists()
        {
            return Client.CreateWalletIfNotExists(Name);
        }
        public Task<BalanceModel> GetBalance()
        {
            return Client.GetBalance(Name);
        }

        public Task<BalanceSummary> GetBalanceSummary()
        {
            return Client.GetBalanceSummary(Name);
        }

        public Task<bool> CreateAddressIfNotExists(Script redeemScript, bool mergePast = true)
        {
            return Client.CreateAddressIfNotExists(Name, redeemScript, mergePast);
        }
        public Task<bool> CreateAddressIfNotExists(IDestination dest, Script redeem = null, bool mergePast = true)
        {
            return Client.CreateAddressIfNotExists(Name, dest, redeem, mergePast);
        }
        public Task<bool> CreateAddressIfNotExists(InsertWalletAddress address)
        {
            return Client.CreateAddressIfNotExists(Name, address);
        }

        public Task<WalletAddress> CreateAddress(Script redeemScript, bool mergePast = true)
        {
            return Client.CreateAddress(Name, redeemScript, mergePast);
        }
        public Task<WalletAddress> CreateAddress(IDestination dest, Script redeem = null, bool mergePast = true)
        {
            return Client.CreateAddress(Name, dest, redeem, mergePast);
        }
        public Task<WalletAddress> CreateAddress(InsertWalletAddress address)
        {
            return Client.CreateAddress(Name, address);
        }

        public KeySetClient GetKeySetClient(string keySet)
        {
            return new KeySetClient(this, keySet);
        }

        public Task<KeySetData[]> GetKeySets()
        {
            return Client.GetKeySets(Name);
        }

        public Task<WalletModel> Get()
        {
            return Client.GetWallet(Name);
        }

        public Task<WalletAddress[]> GetAddresses()
        {
            return Client.GetAddresses(Name);
        }
    }
    public class QBitNinjaClient
    {
        /// <summary>
        /// Use qbit ninja public servers (api.qbit.ninja / tapi.qbit.ninja)
        /// </summary>
        /// <param name="network">The bitcoin network to use</param>
        public QBitNinjaClient(Network network)
        {
            if (network == null)
                throw new ArgumentNullException("network");
            Network = network;
            if (network == Network.Main)
                BaseAddress = new Uri("http://api.qbit.ninja/", UriKind.Absolute);
            if (network == Network.TestNet)
                BaseAddress = new Uri("http://tapi.qbit.ninja/", UriKind.Absolute);
            if (BaseAddress == null)
                throw new NotSupportedException("Network not supported");
        }
        public QBitNinjaClient(string baseAddress, Network network = null)
            : this(new Uri(baseAddress, UriKind.Absolute), network)
        {

        }
        public QBitNinjaClient(Uri baseAddress, Network network = null)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");
            Network = network ?? Network.Main;
            BaseAddress = baseAddress;
        }
        public Network Network
        {
            get;
            set;
        }

        /// <summary>
        /// If true, requested balance will show colored coins. If null, only colored addresses will show colored balances. If false, no colored coin will be shown.
        /// </summary>
        public bool? Colored
        {
            get;
            set;
        }

        public Uri BaseAddress
        {
            get;
            private set;
        }

        public WalletClient GetWalletClient(string wallet)
        {
            return new WalletClient(this, wallet);
        }

        public Task Broadcast(Transaction transaction)
        {
            return Post<string>("transactions", Encoders.Hex.EncodeData(transaction.ToBytes()));
        }

        public Task<BalanceModel> GetBalance(IDestination dest, bool unspentOnly = false)
        {
            var address = AssertAddress(dest);
            return Get<BalanceModel>("balances/" + EscapeUrlPart(address.ToString()) + CreateParameters("unspentOnly", unspentOnly));
        }
        public Task<BalanceSummary> GetBalanceSummary(IDestination dest)
        {
            var address = AssertAddress(dest);
            return Get<BalanceSummary>("balances/" + EscapeUrlPart(address.ToString()) + "/summary" + CreateParameters());
        }

        private string CreateParameters(params object[] parameters)
        {
            if (Colored != null)
            {
                parameters = parameters.Concat(new object[] { "colored", Colored.Value }).ToArray();
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0 ; i < parameters.Length - 1 ; i += 2)
            {
                builder.Append(parameters[i].ToString() + "=" + parameters[i + 1].ToString() + "&");
            }
            if (builder.Length == 0)
                return "";
            var result = builder.ToString();
            return "?" + result.Substring(0, result.Length - 1);
        }

        public Task<BalanceModel> GetBalance(string wallet, bool unspentOnly = false)
        {
            if (wallet == null)
                throw new ArgumentNullException("wallet");
            return Get<BalanceModel>("wallets/" + EscapeUrlPart(wallet) + "/balance" + CreateParameters("unspentOnly", unspentOnly));
        }
        public Task<BalanceSummary> GetBalanceSummary(string wallet)
        {
            if (wallet == null)
                throw new ArgumentNullException("wallet");
            return Get<BalanceSummary>("wallets/" + EscapeUrlPart(wallet) + "/summary" + CreateParameters());
        }

        public Task<WalletModel> CreateWallet(string wallet)
        {
            if (wallet == null)
                throw new ArgumentNullException("wallet");
            return Post<WalletModel>("wallets", new WalletModel()
            {
                Name = wallet
            });
        }

        private Base58Data AssertAddress(IDestination dest)
        {
            if (dest == null)
                throw new ArgumentNullException("address");
            var address = dest.ScriptPubKey.GetDestinationAddress(Network);
            if (address == null)
                throw new ArgumentException("address does not represent a valid bitcoin address", "address");
            if (dest is BitcoinColoredAddress)
                return (BitcoinColoredAddress)dest;
            return address;
        }

        public Task<GetBlockResponse> GetBlock(BlockFeature blockFeature, bool headerOnly = false)
        {
            return Get<GetBlockResponse>("blocks/" + EscapeUrlPart(blockFeature.ToString()) + "?headerOnly=" + headerOnly);
        }

        private string GetFullUri(string relativePath, params object[] parameters)
        {
            relativePath = String.Format(relativePath, parameters ?? new object[0]);
            var uri = BaseAddress.AbsoluteUri;
            if (!uri.EndsWith("/"))
                uri += "/";
            uri += relativePath;
            return uri;
        }

        public Task<T> Get<T>(string relativePath, params object[] parameters)
        {
            return Send<T>(HttpMethod.Get, null, relativePath, parameters);
        }

        public async Task<T> Send<T>(HttpMethod method, object body, string relativePath, params object[] parameters)
        {
            var uri = GetFullUri(relativePath, parameters);
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, uri);
                if (body != null)
                {
                    message.Content = new StringContent(Serializer.ToString(body, Network), Encoding.UTF8, "application/json");
                }
                var result = await client.SendAsync(message).ConfigureAwait(false);
                if (result.StatusCode == HttpStatusCode.NotFound)
                    return default(T);
                if (!result.IsSuccessStatusCode)
                {
                    string error = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(error))
                    {
                        try
                        {
                            var errorObject = Serializer.ToObject<QBitNinjaError>(error, Network);
                            if (errorObject.StatusCode != 0)
                                throw new QBitNinjaException(errorObject);
                        }
                        catch (JsonSerializationException)
                        {
                        }
                        catch (JsonReaderException)
                        {
                        }
                    }
                }
                result.EnsureSuccessStatusCode();
                if (typeof(T) == typeof(byte[]))
                    return (T)(object)await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var str = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (typeof(T) == typeof(string))
                    return (T)(object)str;
                return Serializer.ToObject<T>(str, Network);
            }
        }

        public Task<T> Post<T>(string relativePath, object content)
        {
            return Send<T>(HttpMethod.Post, content, relativePath);
        }


        public Task<GetTransactionResponse> GetTransaction(uint256 transactionId)
        {
            return Get<GetTransactionResponse>("transactions/" + EscapeUrlPart(transactionId.ToString()));
        }

        public async Task<bool> CreateWalletIfNotExists(string name)
        {
            try
            {
                await CreateWallet(name).ConfigureAwait(false);
                return true;
            }
            catch (QBitNinjaException ex)
            {
                if (ex.StatusCode == 409)
                    return false;
                throw;
            }
        }


        public Task<WalletAddress> CreateAddress(string walletName, InsertWalletAddress address)
        {
            return Post<WalletAddress>("wallets/" + EscapeUrlPart(walletName) + "/addresses", address);
        }

        public Task<WalletAddress> CreateAddress(string walletName, IDestination dest, Script redeem, bool mergePast = false)
        {
            return CreateAddress(walletName, new InsertWalletAddress()
            {
                Address = AssertAddress(dest),
                RedeemScript = redeem,
                MergePast = mergePast
            });
        }

        public Task<WalletAddress> CreateAddress(string walletName, Script redeemScript, bool mergePast = false)
        {
            return CreateAddress(walletName, redeemScript.Hash, redeemScript, mergePast);
        }

        public Task<bool> CreateAddressIfNotExists(string walletName, Script redeemScript, bool mergePast = true)
        {
            return CreateAddressIfNotExists(walletName, redeemScript.Hash, redeemScript, mergePast);
        }
        public Task<bool> CreateAddressIfNotExists(string walletName, IDestination dest, Script redeem = null, bool mergePast = true)
        {
            var address = AssertAddress(dest);
            return CreateAddressIfNotExists(walletName, new InsertWalletAddress()
            {
                Address = address,
                RedeemScript = redeem,
                MergePast = mergePast
            });
        }

        public async Task<bool> CreateAddressIfNotExists(string walletName, InsertWalletAddress address)
        {
            try
            {
                await CreateAddress(walletName, address).ConfigureAwait(false);
                return true;
            }
            catch (QBitNinjaException ex)
            {
                if (ex.StatusCode == 409)
                    return false;
                throw;
            }
        }

        public Task<HDKeySet> CreateKeySet(string walletName, HDKeySet keyset)
        {
            return Post<HDKeySet>("wallets/" + EscapeUrlPart(walletName) + "/keysets", keyset);
        }

        public async Task<bool> CreateKeySetIfNotExists(string walletName, HDKeySet keyset)
        {
            try
            {
                await Post<HDKeySet>("wallets/" + EscapeUrlPart(walletName) + "/keysets", keyset).ConfigureAwait(false);
                return true;
            }
            catch (QBitNinjaException ex)
            {
                if (ex.StatusCode == 409)
                    return false;
                throw;
            }
        }

        public Task<bool> CreateKeySetIfNotExists(string wallet, string keyset, ExtPubKey[] keys, int signatureCount, KeyPath path)
        {
            return CreateKeySetIfNotExists(wallet, new HDKeySet()
            {
                Name = keyset,
                ExtPubKeys = keys.Select(k => k.GetWif(Network)).ToArray(),
                SignatureCount = signatureCount,
                Path = path
            });
        }

        public Task<HDKeySet> CreateKeySet(string wallet, string keyset, ExtPubKey[] keys, int signatureCount, KeyPath path)
        {
            return CreateKeySet(wallet, new HDKeySet()
            {
                Name = keyset,
                ExtPubKeys = keys.Select(k => k.GetWif(Network)).ToArray(),
                SignatureCount = signatureCount,
                Path = path
            });
        }

        public Task<HDKeyData> GenerateKey(string wallet, string keyset)
        {
            return Post<HDKeyData>(BuildPath(wallet, keyset) + "/keys", null);
        }

        private static string BuildPath(string wallet, string keyset)
        {
            return "wallets/" + EscapeUrlPart(wallet) + "/keysets/" + EscapeUrlPart(keyset);
        }

        public async Task<bool> DeleteKeySet(string wallet, string keyset)
        {
            var result = await Send<string>(HttpMethod.Delete, null, BuildPath(wallet, keyset), null).ConfigureAwait(false);
            return result != null;
        }

        public Task<KeySetData[]> GetKeySets(string wallet)
        {
            return Get<KeySetData[]>("wallets/" + EscapeUrlPart(wallet) + "/keysets");
        }

        public Task<WalletModel> GetWallet(string walletName)
        {
            return Get<WalletModel>("wallets/" + EscapeUrlPart(walletName));
        }

        public static string EscapeUrlPart(string str)
        {
            var path = System.Web.NBitcoin.HttpUtility.UrlEncode(str);
            if (path.Contains("?") || path.Contains("/"))
                throw new ArgumentException("Invalid character found in the path of the request ('?' or '/')");
            return path;
        }

        public Task<WalletAddress[]> GetAddresses(string walletName)
        {
            return Get<WalletAddress[]>("wallets/" + EscapeUrlPart(walletName) + "/addresses");
        }
    }
}
