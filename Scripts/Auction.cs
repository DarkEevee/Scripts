/*
This file is a part of Pokemon Mystery Universe.

Copyright (C) 2015 PMU Team

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server;
using Server.Players;
using Server.Network;
using Server.Maps;

namespace Script {
    class Auction {
        public enum AuctionState {
            NotStarted,
            Started,
            Pending
        }

        public static readonly string AUCTION_MAP = MapManager.GenerateMapID(363);

        public static string AuctionMaster { get; set; }
        public static string LastAuctionMaster { get; set; }
        public static string SetItem { get; set; }

        public static int MinBid { get; set; }
        public static int BidIncrement { get; set; }

        public static AuctionState State { get; set; }

        public static bool StaffAuction = false;

        static string highestBidder;
        public static string HighestBidder {
            get { return highestBidder; }
        }

        static int highestBid;
        public static int HighestBid {
            get { return highestBid; }
        }


        public static void Initialize() {
            AuctionMaster = null;
        }

        public static void CreateAuction(Client auctionMaster) {
            //if (Ranks.IsAllowed(auctionMaster, Enums.Rank.Developer)) {
            if (string.IsNullOrEmpty(AuctionMaster)) {
                AuctionMaster = auctionMaster.Player.CharID;
                State = AuctionState.Pending; // modified for testing
                Messenger.PlayerMsg(auctionMaster, "You have created a new auction. Say /auctionadminhelp for commands on what to do next.", Text.Yellow);
            } else {
                Messenger.PlayerMsg(auctionMaster, "An auction is already in progress!", Text.BrightRed);
            }
            //} else {
            //    Messenger.PlayerMsg(auctionMaster, "You do not have a high enough access to start an auction!", Text.BrightRed);
            //}
        }

        public static void SetAuctionItem(Client client, string newItem) {
            if (client.Player.CharID == AuctionMaster) {
                SetItem = newItem;
                Messenger.PlayerMsg(client, "Item has been set to " + SetItem + "!", Text.White);
            }
        }

        public static void SetBidIncrement(Client client, int newIncrement) {
            if (client.Player.CharID == AuctionMaster) {
                if (client.Player.MapID == AUCTION_MAP) {
                    BidIncrement = newIncrement;
                    Messenger.PlayerMsg(client, "Bid increment set to: " + newIncrement, Text.White);
                }
            }
        }

        public static void SetAuctionMinBid(Client client, int minBid) {
            if (client.Player.CharID == AuctionMaster) {
                if (client.Player.MapID == AUCTION_MAP) {
                    MinBid = minBid;
                    Messenger.PlayerMsg(client, "Minimum bid set to: " + minBid, Text.White);
                }
            }
        }

        public static void StartAuction(Client client) {
            if (client.Player.CharID == AuctionMaster) {
                if (client.Player.MapID == AUCTION_MAP) {
                    State = AuctionState.Started;
                    highestBidder = null;
                    highestBid = -1;
                    IMap playerMap = client.Player.Map;
                    foreach (Client i in playerMap.GetClients()) {
                        Messenger.BattleMsg(i, "The auction for a " + SetItem + " has begun! Say /bid bidinpoke to make a bid!", Text.Yellow);
                        Messenger.PlayerMsg(i, "The auction for a " + SetItem + " has begun! Say /bid bidinpoke to make a bid!", Text.Yellow);
                    }
                }
            }
        }

        public static void EndAuction(Client client) {
            if (client.Player.CharID == AuctionMaster || Ranks.IsAllowed(client, Enums.Rank.Developer)) {
                if (client.Player.MapID == AUCTION_MAP) {
                    if (State == AuctionState.Pending) {
                        State = AuctionState.NotStarted;
                        AuctionMaster = null;

                        IMap playerMap = client.Player.Map;
                        foreach (Client i in playerMap.GetClients()) {
                            Messenger.PlayerMsg(i, "The auction has ended.", Text.Yellow);
                            //Messenger.SendBattleDivider(i);
                            //Messenger.PlayerMsg(client, "The auction has been ended!", Text.Yellow);
                        }
                    } else if (State == AuctionState.Started) {
                        State = AuctionState.NotStarted;
                        //LastAuctionMaster = AuctionMaster;
                        AuctionMaster = null;
                        // if (Ranks.IsDisallowed(client, Enums.Rank.Developer)) {
                        // 	AuctionMaster = null;
                        // }
                        IMap playerMap = client.Player.Map;
                        foreach (Client i in playerMap.GetClients()) {
                            Messenger.PlayerMsg(i, "The auction has ended. The winner was " + ClientManager.FindClient(highestBidder).Player.Name + " with a bid of " + highestBid.ToString(), Text.Yellow);
                            Messenger.SendBattleDivider(i);
                            //Messenger.PlayerMsg(client, "The auction has been ended!", Text.Yellow);
                        }
                    }
                } else {
                    Messenger.PlayerMsg(client, "No auction has been started yet!", Text.BrightRed);
                }
            }
        }

        public static void UpdateBids(Client client, int newBid) {
            if (client.Player.MapID == AUCTION_MAP) {
                if (State == AuctionState.Started) {
                    if (newBid > MinBid) {
                        if (newBid > highestBid + BidIncrement) {
                            highestBid = newBid;
                            highestBidder = client.Player.Name;
                            IMap map = client.Player.Map;
                            foreach (Client i in map.GetClients()) {
                                Messenger.BattleMsg(i, client.Player.Name + " has made a bid of " + newBid.ToString() + " Poké for " + SetItem, Text.Yellow);
                            }
                        } else {
                            Messenger.PlayerMsg(client, "You must make a bid of at least " + (highestBid + BidIncrement).ToString() + " Poké!", Text.BrightRed);
                        }
                    } else {
                        Messenger.PlayerMsg(client, "Your bid must be higher than " + MinBid.ToString() + " Poké!", Text.BrightRed);
                    }
                } else {
                    Messenger.PlayerMsg(client, "The auction hasn't started yet!", Text.BrightRed);
                }
            }
        }

        public static void CheckBidder(Client client) {
            //if (client.Player.CharID == AuctionMaster) {
            Messenger.BattleMsg(client, "The highest bidder is " + ClientManager.FindClient(highestBidder).Player.Name + ", with a bid of " + highestBid.ToString() + " Poké! (" + SetItem + ")", Text.BrightGreen);
            //}
        }

        public static void SayHelp(Client client) {
            if (client.Player.CharID == AuctionMaster) {
                Messenger.PlayerMsg(client, "The auction commands are: \n/setauctionitem itemname \n/setauctionminbid minbidinpoke \n/setbidincrement bidincrement \n/startauction \n/endauction \n/checkbidder", Text.White);
            }
        }

        public static void SayPlayerHelp(Client client) {
            Messenger.PlayerMsg(client, "When an auction has started, say /bid bidinpoke to place a bid.", Text.Yellow);
        }
    }
}
