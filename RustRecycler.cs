using Facepunch.Extend;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustRecycler", "Discord: abhajus432", "1.0.1")]
    [Description("A team-shared Recycler plugin for Rust, using Claude Code connected to dnSpy for finding methods quicker, and small code suggestions.")]
    public class RustRecycler : RustPlugin
    {

        Dictionary<Recycler, List<BasePlayer>> recyclerPlayers = new Dictionary<Recycler, List<BasePlayer>>();

        string message;
        string message2;
        int MaxUsers = 1;

        protected override void LoadDefaultConfig()
        {
            Config["MaxUsers"] = MaxUsers;
            Config["Message"] = "Hey! The recycler is in use by a different team.";
            Config["Message2"] = "Sorry, the recycler only allow's: {0} teammates per Recycler, from the same team!";
        }

        void Init()
        {
            message = Config["Message"].ToString();
            message2 = Config["Message2"].ToString();
            MaxUsers = Config["MaxUsers"].ToString().ToInt();
        }

        object CanLootEntity(BasePlayer player, Recycler container)
        {

            if (container is Recycler)
            {
                // team code has to go first, because only the first person accessing the Recycler needs to "Open" it
                if (player.InATeam()) // else would be if player is not team, nothing happens. If he joins a team while on recycler, has to re-open it.
                {
                    // need this, so i could compare between the player that is accessing and the player that is already using, to understand whether they are on the same team
                    var team = RelationshipManager.ServerInstance.FindPlayersTeam(player.userID);
                    var teamid = team.teamID;
                    //

                    if (recyclerPlayers.ContainsKey(container)) // if recycler is in use
                    {
                        var first_player = recyclerPlayers[container].ElementAt(0);
                        var playersInUse = recyclerPlayers[container].Count;
                        var temp_teamid = RelationshipManager.ServerInstance.FindPlayersTeam(first_player.userID).teamID;

                        Puts("if (recyclerPlayers.ContainsKey(container))");

                        string text = string.Format(message2, MaxUsers);
                        if (playersInUse >= MaxUsers) // recycler is already full
                        {
                            SendReply(player, text);
                            player.ShowToast(GameTip.Styles.Red_Normal, new Translate.Phrase("message2", text), false);
                            return true; // deny the usage
                        }

                        if (teamid == temp_teamid)
                        {
                            Puts("teamid == temp_teamid");

                            container.SetFlagLocal(BaseEntity.Flags.Open, false, false);

                            // AI suggested to add this, I thought why? Because I enable this for a second and then a second or two later my coroutine works in place and it sets to false for the entirety.
                            // but without the SetFlagLocal the code would also definetely work, this is just fun thing I added cause I was like alr claude u got me XD

                            ServerMgr.Instance.StartCoroutine(
                                OpenRecycler(container, result =>
                                {
                                    if (result == true)
                                    {
                                        recyclerPlayers[container].Add(player);
                                    } else
                                    {
                                        player.inventory.loot.Clear();
                                    }
                                }, player)
                            );

                            // the list will have several people but its so because if first person left, there was a second person using so
                            // the first person could access again or the third person in the team or anybody else.
                        }
                        else
                        {
                            SendReply(player, message);
                            player.ShowToast(GameTip.Styles.Red_Normal, new Translate.Phrase("recycler_team_in_use", message), false);
                            return true; // do not open the recycler
                        }

                    } 
                    else // if it is not yet (save the recycler and the first person accessing it)
                    {
                        // only the first person accessing the recycler needs to "Open" it.

                        Puts("ServerMgr.Instance.StartCoroutine(OpenRecycler(container, result =>");

                        ServerMgr.Instance.StartCoroutine(
                            OpenRecycler(container, result =>
                            {
                                if (result == true)
                                {
                                    recyclerPlayers[container] = new List<BasePlayer>();
                                    recyclerPlayers[container].Add(player);
                                } else // just in case something wrong happened with the coroutine, force kick the Player out of the Recycler
                                {
                                    player.inventory.loot.Clear(); // force close the Recycler
                                }
                            }, player)
                        );
                    } // !player.InATeam() && 
                } else if (recyclerPlayers.ContainsKey(container)) // checks if the person trying to open recycler is not on a team and whether the recycler is also open
                {
                    SendReply(player, message);
                    player.ShowToast(GameTip.Styles.Red_Normal, new Translate.Phrase("recycler_team_in_use", message), false);
                    return true; // do not open the recycler
                }
            }

            return null;
        }

        IEnumerator OpenRecycler(Recycler container, Action<bool> callback, BasePlayer player) // change recycler state from "Occupied" to "Open", for everybody
        {
            bool RecyclerOpened = false;
            bool TimerStop = false;
            bool timerStarted = false;
            int counter = 0;

            Puts("OpenRecyclerTriggered");

            while (true)
            {
                if (timerStarted == false) 
                {
                    timer.Repeat(0.1f, 20, () => // 2 seconds in total
                    {
                        counter++;

                        if (counter == 19 && !player.inventory.loot.IsLooting())
                        {
                            TimerStop = true;
                        }
                    });

                    timerStarted = true;
                }

                if (TimerStop) // just an edge case in case something bug's with the recycler
                {
                    callback?.Invoke(RecyclerOpened);

                    yield break;
                }

                if (container.IsOpen()) // check if Recycler's Open flag has changed from False to True
                {
                    using (BaseEntity.FlagsUpdateScope flagsUpdateScope = container.StartSetFlags(BaseEntity.FlagsUpdateMode.SendNetworkUpdate))
                    {
                        flagsUpdateScope.Set(BaseEntity.Flags.Open, false, false);
                    }

                    RecyclerOpened = true;

                    callback?.Invoke(RecyclerOpened);

                    yield break;
                }

                yield return new WaitForSeconds(0.01f); // make it run as fast as possible
            }
        }

        void OnLootEntityEnd(BasePlayer player, Recycler entity)
        {
            if (recyclerPlayers.ContainsKey(entity)) // if the recycler is in use by anyone, so it doesn't throw an error out
            {
                if (recyclerPlayers[entity].Contains(player)) // if the player was saved in the dictionary previously as a "Recycler User"
                {

                    var PlayersOnRecycler = recyclerPlayers[entity].Count; // get the amount of players using recycler

                    if (PlayersOnRecycler > 1) // if more than 1 player on recycler, remove the player that has left
                    {
                        recyclerPlayers[entity].Remove(player);
                    }
                    else // if only 1 player, remove the player and the entry (or could just remove the key entry and then it would also remove the value but eh)
                    {
                        recyclerPlayers[entity].Remove(player);
                        recyclerPlayers.Remove(entity);
                    }
                }
            }
        }

        object OnTeamLeave(RelationshipManager.PlayerTeam _, BasePlayer player) // once player leaves the team, remove him from the dictionary and kick from the recycler UI
        {

            if (player.inventory.loot.entitySource is Recycler recycler) // if player is interacting with a recycler
            {
                if (recyclerPlayers.ContainsKey(recycler)) // if that recycler is saved in the dictionary (have to keep on checking because so far, the player could be looting other recycler and not this one)

                {
                    TeamKick(player, recycler);
                }
            }

            return null;
        }

        object OnTeamKick(RelationshipManager.PlayerTeam _, BasePlayer player, ulong target) // once player gets kicked from team, remove him from the dictionary and kick from the recycler UI
        {
            if (player.inventory.loot.entitySource is Recycler recycler) // if player is interacting with a recycler
            {
                if (recyclerPlayers.ContainsKey(recycler)) // if that recycler is saved in the dictionary (have to keep on checking because so far, the player could be looting other recycler and not this one)

                {
                    TeamKick(RelationshipManager.FindByID(target), recycler);
                }
            }

            return null;
        }

        void TeamKick(BasePlayer player, Recycler recycler)
        {
            if (recyclerPlayers[recycler].Contains(player)) // if the player is actually using the recycler
            {
                var playersUsingRecycler = recyclerPlayers[recycler].Count;

                if (playersUsingRecycler > 1) // if there is more than 1 person using Recycler
                {
                    recyclerPlayers[recycler].Remove(player);
                }
                else // if there is 1 person using recycler
                {
                    recyclerPlayers.Remove(recycler);
                }

                player.inventory.loot.Clear(); // force close the Recycler
            }

        }
    }
}