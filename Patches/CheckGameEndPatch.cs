﻿using HarmonyLib;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;

namespace MoreGamemodes
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class CheckEndCriteriaPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!GameData.Instance) return false;
            if (DestroyableSingleton<TutorialManager>.InstanceExists) return true;
            if (Options.NoGameEnd.GetBool()) return false;

            if (CustomGamemode.Instance.Gamemode == Gamemodes.Classic || CustomGamemode.Instance.Gamemode == Gamemodes.RandomItems)
            {
                return true;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.HideAndSeek)
            {
                if (CheckAndEndGameForEveryoneDied()) return false;
                if (CheckAndEndGameForHideAndSeek()) return false;
                if (CheckAndEndGameForTaskWin()) return false;
                if (CheckAndEndGameForCrewmateWin()) return false;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.ShiftAndSeek)
            {
                if (CheckAndEndGameForEveryoneDied()) return false;
                if (CheckAndEndGameForHideAndSeek()) return false;
                if (CheckAndEndGameForTaskWin()) return false;
                if (CheckAndEndGameForCrewmateWin()) return false;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.BombTag)
            {
                if (CheckAndEndGameForEveryoneDied()) return false;
                if (CheckAndEndGameForBattleRoyale()) return false;          
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.BattleRoyale)
            {
                if (CheckAndEndGameForEveryoneDied()) return false;
                if (CheckAndEndGameForBattleRoyale()) return false;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.Speedrun)
            {
                if (CheckAndEndGameForSpeedrun()) return false;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.KillOrDie)
            {
                if (CheckAndEndGameForEveryoneDied()) return false;
                if (CheckAndEndGameForBattleRoyale()) return false;          
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.Zombies)
            {
                if (CheckAndEndGameForZombiesImpostorWin()) return false;
                if (CheckAndEndGameForZombiesCrewmateWin()) return false;
                if (CheckAndEndGameForZombiesTaskWin()) return false;
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.Jailbreak)
            {
                if (CheckAndEndGameForEveryoneEscape()) return false;
                if (CheckAndEndGameForTimeEnd()) return false;          
            }
            else if (CustomGamemode.Instance.Gamemode == Gamemodes.Deathrun)
            {
                if (Options.DisableMeetings.GetBool())
                {
                    if (CheckAndEndGameForHideAndSeek()) return false;
                    return false;
                }
                return true;    
            }
            return false;
        }

        private static bool CheckAndEndGameForTaskWin()
        {
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Data.Role.IsImpostor)
                        winners.Add(pc.PlayerId);
                }
                StartEndGame(GameOverReason.HumansByTask, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForCrewmateWin()
        {
            var numImpostorAlive = 0;
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor && !pc.Data.IsDead) ++numImpostorAlive;
                if (!pc.Data.IsDead) AllAlivePlayers.Add(pc);
            }
            if (numImpostorAlive == 0)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Data.Role.IsImpostor)
                        winners.Add(pc.PlayerId);
                }
                var reason = GameOverReason.HumansByVote;
                if (GameData.LastDeathReason == DeathReason.Disconnect)
                    reason = GameOverReason.ImpostorDisconnect;
                StartEndGame(reason, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForHideAndSeek()
        {
            var numImpostorAlive = 0;
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor && !pc.Data.IsDead) ++numImpostorAlive;
                if (!pc.Data.IsDead) AllAlivePlayers.Add(pc);
            }
            if (AllAlivePlayers.Count - numImpostorAlive == 0)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.IsImpostor)
                        winners.Add(pc.PlayerId);
                }
                StartEndGame(GameOverReason.ImpostorByKill, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForBattleRoyale()
        {
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.Data.IsDead) AllAlivePlayers.Add(pc);
            }
            if (AllAlivePlayers.Count == 1)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Data.IsDead)
                        winners.Add(pc.PlayerId);
                }
                StartEndGame(GameOverReason.ImpostorByKill, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForEveryoneDied()
        {
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.Data.IsDead) AllAlivePlayers.Add(pc);
            }
            if (AllAlivePlayers.Count == 0)
            {
                List<byte> winners = new();
                StartEndGame(GameOverReason.ImpostorByKill, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForSpeedrun()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var tasksCompleted = 0;
                var totalTasks = 0;
                foreach (var task in pc.myTasks)
                {
                    ++totalTasks;
                    if (task.IsComplete)
                        ++tasksCompleted;
                }
                if (Options.CurrentBodyType == SpeedrunBodyTypes.Ghost)
                    --totalTasks;
                if (tasksCompleted >= totalTasks)
                {
                    List<byte> winners = new();
                    winners.Add(pc.PlayerId);
                    StartEndGame(GameOverReason.HumansByTask, winners);
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForZombiesImpostorWin()
        {
            var numImpostorAlive = 0;
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (Main.StandardRoles[pc.PlayerId].IsImpostor() && !pc.Data.IsDead) ++numImpostorAlive;
                if (!pc.Data.IsDead && !pc.IsZombie()) AllAlivePlayers.Add(pc);
            }
            if (numImpostorAlive * 2 >= AllAlivePlayers.Count)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (Main.StandardRoles[pc.PlayerId].IsImpostor() || pc.IsZombie())
                        winners.Add(pc.PlayerId);
                }
                var reason = GameOverReason.ImpostorByKill;
                if (GameData.LastDeathReason == DeathReason.Exile)
                    reason = GameOverReason.ImpostorByVote;
                if (GameData.LastDeathReason == DeathReason.Disconnect)
                    reason = GameOverReason.HumansDisconnect;
                StartEndGame(reason, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForZombiesCrewmateWin()
        {
            var numImpostorAlive = 0;
            List<PlayerControl> AllAlivePlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (Main.StandardRoles[pc.PlayerId].IsImpostor() && !pc.Data.IsDead) ++numImpostorAlive;
                if (!pc.Data.IsDead && !pc.IsZombie()) AllAlivePlayers.Add(pc);
            }
            if (numImpostorAlive == 0)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!Main.StandardRoles[pc.PlayerId].IsImpostor() && !pc.IsZombie())
                        winners.Add(pc.PlayerId);
                }
                var reason = GameOverReason.HumansByVote;
                if (GameData.LastDeathReason == DeathReason.Disconnect)
                    reason = GameOverReason.ImpostorDisconnect;
                StartEndGame(reason, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForZombiesTaskWin()
        {
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!Main.StandardRoles[pc.PlayerId].IsImpostor() && !pc.IsZombie())
                        winners.Add(pc.PlayerId);
                }
                StartEndGame(GameOverReason.HumansByTask, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForEveryoneEscape()
        {
            bool someoneRemain = false;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.IsGuard() && !pc.HasEscaped())
                    someoneRemain = true;
            }
            if (!someoneRemain)
            {
                List<byte> winners = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.IsGuard())
                        winners.Add(pc.PlayerId);
                }
                StartEndGame(GameOverReason.ImpostorByKill, winners);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForTimeEnd()
        {
            if (Main.Timer >= Options.GameTime.GetFloat())
            {
                List<byte> winners = new();
                int prisoners = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    ++prisoners;
                    if (!pc.IsGuard() && pc.HasEscaped())
                        winners.Add(pc.PlayerId);
                }
                if (winners.Count * 2 < prisoners)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.IsGuard())
                            winners.Add(pc.PlayerId);
                    }
                }
                StartEndGame(GameOverReason.ImpostorByKill, winners);
                return true;
            }
            return false;
        }

        public static void StartEndGame(GameOverReason reason, List<byte> winners)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.None);
            sender.StartMessage(-1);
            MessageWriter writer = sender.stream;

            List<byte> ReviveReqiredPlayerIds = new();
            var ImpostorWin = false;
            switch (reason)
            {
                case GameOverReason.HumansDisconnect: ImpostorWin = true; break;
                case GameOverReason.ImpostorByVote: ImpostorWin = true; break;
                case GameOverReason.ImpostorByKill: ImpostorWin = true; break;
                case GameOverReason.ImpostorBySabotage: ImpostorWin = true; break;
                case GameOverReason.HideAndSeek_ByKills: ImpostorWin = true; break;
                default: ImpostorWin = false; break;
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.Data.IsDead) ReviveReqiredPlayerIds.Add(pc.PlayerId);
                if (winners.Contains(pc.PlayerId))
                {
                    if (ImpostorWin)
                        pc.SetRole(RoleTypes.ImpostorGhost, false);
                    else
                        pc.SetRole(RoleTypes.CrewmateGhost, false);
                }
                else
                {
                    if (ImpostorWin)
                        pc.SetRole(RoleTypes.CrewmateGhost, false);
                    else
                        pc.SetRole(RoleTypes.ImpostorGhost, false);
                }
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (winners.Contains(pc.PlayerId))
                {
                    if (ImpostorWin)
                        SetGhostRole(ToGhostImpostor: true);
                    else
                        SetGhostRole(ToGhostImpostor: false);
                }
                else
                {
                    if (ImpostorWin)
                        SetGhostRole(ToGhostImpostor: false);
                    else
                        SetGhostRole(ToGhostImpostor: true);
                }

                void SetGhostRole(bool ToGhostImpostor)
                {
                    if (!pc.Data.IsDead) ReviveReqiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.ImpostorGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.ImpostorGhost, false);
                    }
                    else
                    {
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.CrewmateGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.CrewmateGhost, false);
                    }
                }
            }
            writer.StartMessage(1);
            {
                NetworkedPlayerInfo data = null;
                writer.WritePacked(data.NetId);
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (ReviveReqiredPlayerIds.Contains(info.PlayerId))
                    {
                        info.IsDead = false;
                        writer.StartMessage(info.PlayerId);
                        info.Serialize(writer, true);
                        writer.EndMessage();
                    }
                }
                writer.EndMessage();
            }
            sender.EndMessage();

            writer.StartMessage(8);
            {
                writer.Write(AmongUsClient.Instance.GameId);
                writer.Write((byte)reason);
                writer.Write(false);
            }
            writer.EndMessage();

            sender.SendMessage();
        }
    }
}