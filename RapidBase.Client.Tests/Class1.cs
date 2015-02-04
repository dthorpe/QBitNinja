﻿using NBitcoin;
using RapidBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RapidBase.Client.Tests
{
    public class Class1
    {
        [Fact]
        public void CanGetBalance()
        {
            var client = CreateClient();
            var balance = client.GetBalance(new BitcoinAddress("15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe")).Result;
            Assert.NotNull(balance);
            Assert.True(balance.Operations.Any(o => o.Amount == Money.Coins(0.02m)));

            var balanceSummary = client.GetBalanceSummary(new BitcoinAddress("15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe")).Result;
            Assert.True(balanceSummary.Confirmed.TransactionCount > 60);
        }

        [Fact]
        public void CanManageWallet()
        {
            var client = CreateClient();
            var walletName = "temp-Nicolas Dorier";
            client.CreateWalletIfNotExists(walletName).Wait();
            client.AddAddressIfNotExists(walletName, BitcoinAddress.Create("15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe")).Wait();
            client.AddAddressIfNotExists(walletName, BitcoinAddress.Create("1KF8kUVHK42XzgcmJF4Lxz4wcL5WDL97PB ")).Wait();

            var balance = client.GetBalance(walletName).Result;
            Assert.True(balance.Operations.Count > 70);

            client.AddKeySetIfNotExists(walletName, new HDKeySet()
            {
                Name = "main",
                ExtPubKeys = new BitcoinExtPubKey[] { new ExtKey().Neuter().GetWif(Network.Main) },
                Path = new KeyPath("44'/0'/0'"),
                SignatureCount = 1
            }).Wait();

        }

        [Fact]
        public void CanGetBlock()
        {
            var client = CreateClient();
            var block = client.GetBlock(new BlockFeature(SpecialFeature.Last), true).Result;
            Assert.NotNull(block);
            Assert.Null(block.Block);
            var height = block.AdditionalInformation.Height;
            block = client.GetBlock(new BlockFeature(SpecialFeature.Last)
            {
                Offset = -1
            }, true).Result;
            Assert.NotNull(block);
            Assert.Null(block.Block);
            Assert.True(block.AdditionalInformation.Height < height);
            block = client.GetBlock(new BlockFeature(SpecialFeature.Last)
            {
                Offset = 1
            }, true).Result;
            Assert.Null(block);
        }

        [Fact]
        public void CanGetTransaction()
        {
            var client = CreateClient();
            var tx = client.GetTransaction(new uint256("ce530f95b2b7f559292c60cefa340eaf7c83cde3e063c59bc43c108a3bd24360")).Result;
            Assert.NotNull(tx);
        }

        private RapidBaseClient CreateClient()
        {
            return new RapidBaseClient(new Uri("http://rapidbase-test.azurewebsites.net/"));
        }

    }
}