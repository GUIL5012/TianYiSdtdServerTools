﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using IceCoffee.Common;
using IceCoffee.Common.LogManager;
using IceCoffee.Network.Sockets;
using TianYiSdtdServerTools.Client.Models.Chat;
using TianYiSdtdServerTools.Client.Models.Players;
using TianYiSdtdServerTools.Client.Models.SdtdServerInfo;

namespace TianYiSdtdServerTools.Client.TelnetClient.Internal
{
    internal class TcpSession : BaseSession<TcpSession>
    {
        #region 字段
        /// <summary>
        /// 数据缓冲区
        /// </summary>
        private string _line;      

        /// <summary>
        /// 数据缓冲区列表
        /// </summary>
        private readonly List<string> _lineList = new List<string>();

        /// <summary>
        /// 正在执行的命令
        /// </summary>
        private string _executingCmd;

        /// <summary>
        /// INF 开始坐标
        /// </summary>
        private int _startIndexINF;

        /// <summary>
        /// 是否正在往数据缓冲区列表中写入数据
        /// </summary>
        private bool _isWritingToBufferList;

        /// <summary>
        /// 是否正在执行本机请求的命令
        /// </summary>
        private bool _isExecutingCmd;

        /// <summary>
        /// 服务器部分首选项暂存
        /// </summary>
        private readonly ServerPartialPref _serverPartialPref = new ServerPartialPref();

        /// <summary>
        /// 在线游戏玩家
        /// </summary>
        private Dictionary<string, PlayerInfo> _onlinePlayers = new Dictionary<string, PlayerInfo>();

        /// <summary>
        /// 获取服务器游戏在线玩家等数据时钟
        /// </summary>
        private readonly Timer _requestDataTimer = new Timer() { AutoReset = true, Enabled = false, Interval = 10000 };

        #endregion

        #region 属性
        /// <summary>
        /// 在线游戏玩家
        /// </summary>
        public Dictionary<string, PlayerInfo> OnlinePlayers { get { return _onlinePlayers; } }
        #endregion

        #region 方法

        #region 构造方法
        public TcpSession()
        {
            _requestDataTimer.Elapsed += RequestData;
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 请求数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void RequestData(object sender, ElapsedEventArgs e)
        {
            RequestData();
        }
        private void RequestData()
        {
            SendCmd("gt");
            RequestOnlinePlayerInfo();
        }
        /// <summary>
        /// 请求在线玩家信息
        /// </summary>
        private void RequestOnlinePlayerInfo()
        {
            SendCmd("lp");
        }
        /// <summary>
        /// 发送密码
        /// </summary>
        private void SendPassword()
        {
            // "0\r\n"确保密码发送成功，被服务端接受
            this.Send(("0\r\n" + SdtdConsole.Instance.Password + Environment.NewLine).ToUtf8());
        }

        /// <summary>
        /// 读取一行到缓冲区
        /// </summary>
        private void ReadLineToBuffer()
        {
            _line = Encoding.UTF8.GetString(ReadBuffer.ReadLine());
            System.Diagnostics.Debug.Write(_line);
            SdtdConsole.Instance.RaiseRecvLineEvent(_line);
        }

        /// <summary>
        /// 处理正在执行的命令
        /// </summary>
        private void HandleExecutingCommand()
        {
            if (_isWritingToBufferList == false)
            {
                _lineList.Clear();
            }
            if (_executingCmd == "lp")    //列表在线玩家
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.Contains(". id="))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("Total"))
                    {
                        ListDataHandler.ParseOnlinePlayers(_lineList,ref _onlinePlayers);

                        if (_lineList.Count == 0)
                        {
                            _requestDataTimer.Stop();
                            SdtdConsole.Instance.RaiseServerNonePlayerEvent();
                        }

                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "lkp")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.Contains(" id="))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("Total"))
                    {
                        //ControlPanel::Instance()->tempListData.setOfflinePlayers(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "admin list")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.StartsWith("Defined") || _line.StartsWith("  Level"))
                    {
                        continue;
                    }
                    else if (_line.StartsWith("   "))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("***"))
                    {
                        //ControlPanel::Instance()->tempListData.setAdmins(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "cp list")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.StartsWith("Defined") || _line.StartsWith("  Level"))
                    {
                        continue;
                    }
                    else if (_line.StartsWith("   "))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("***"))
                    {
                        //ControlPanel::Instance()->tempListData.setPermissions(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "whitelist list")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.StartsWith("Whitelist"))
                    {
                        continue;
                    }
                    else if (_line.StartsWith("  "))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("***"))
                    {
                        //ControlPanel::Instance()->tempListData.setWhitelist(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "ban list")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.StartsWith("Ban list") || _line.StartsWith("  Banned"))
                    {
                        continue;
                    }
                    else if (_line.StartsWith("  "))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("***"))
                    {
                        //ControlPanel::Instance()->tempListData.setBanlist(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "llp")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.StartsWith("Player") || _line.StartsWith("   "))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("Total"))
                    {
                        //ControlPanel::Instance()->tempListData.setKeystoneBlockList(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "le")
            {
                do
                {
                    ReadLineToBuffer();
                    if (_line.Contains(". id="))
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (_line.StartsWith("Total"))
                    {
                        //ControlPanel::Instance()->tempListData.setActiveEntityList(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else if (_executingCmd == "se")
            {
                do
                {
                    ReadLineToBuffer();
                    string StartsWith2 = _line.Substring(0, 2);
                    if (StartsWith2 == "1s" || StartsWith2 == "2n" || StartsWith2 == "  ")
                    {
                        _lineList.Add(_line);
                        _isWritingToBufferList = true;
                        continue;
                    }
                    else if (StartsWith2 == "**")
                    {
                        //ControlPanel::Instance()->tempListData.setCanUseEntityList(cmdBufferList);
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        return;
                    }
                    else
                    {
                        _isWritingToBufferList = false;
                        _isExecutingCmd = false;
                        InternalHandleData();
                        return;
                    }
                } while (ReadBuffer.CanReadLine);
            }
            else
            {
                _isExecutingCmd = false;
                return;
            }
        }

        /// <summary>
        /// 应该在内部处理的数据
        /// </summary>
        private void InternalHandleData()
        {
            if (_line.Length > 36)
            {
                _startIndexINF = _line.IndexOf(' ', 25); // 带有时间的最小命令长度 25
                if (_startIndexINF != -1 && _line.Length > _startIndexINF + 5 && _line.Substring(_startIndexINF, 5) == " INF ")
                {
                    HandleINF();
                    return;
                }
            }

            if (_line.StartsWith("Day"))
            {
                int outEnd = 0;
                SdtdConsole.Instance.RaiseGameDateTimeChangedEvent(new GameDateTime()
                {
                    Day = _line.GetMidStr("Day ", ",", out outEnd, outEnd).ToInt(),
                    Hour = _line.GetMidStr(", ", ":", out outEnd, outEnd).ToInt(),
                    Minute = _line.GetMidStr(":", Environment.NewLine, outEnd).ToInt(),
                });
            }

            if (SdtdConsole.Instance.IsConnected == false)
            {
                if (_line.StartsWith("Please enter password:"))
                {
                    SdtdConsole.Instance.RaiseConnectionStateChangedEvent(ConnectionState.PasswordVerifying);
                    SendPassword();
                }
                else if (_line.StartsWith("Press 'help' to get a list of all commands."))
                {                    
                    SdtdConsole.Instance.RaiseConnectionStateChangedEvent(ConnectionState.Connected);                    
                    RequestData();
                    _requestDataTimer.Start();
                }
                else if (_line.StartsWith("Password incorrect, please enter password:"))
                {
                    SdtdConsole.Instance.RaiseConnectionStateChangedEvent(ConnectionState.PasswordIncorrect);
                }
                else if (_line.StartsWith("*** Server version: "))
                {
                    _serverPartialPref.VersionStr = _line.GetMidStr("*** Server version: ", " Compatibility");
                    CheckServerVersion(_serverPartialPref.VersionStr.GetMidStr("Alpha ", ".").ToInt());
                }
                else if (_line.StartsWith("*** Dedicated server only build"))
                {
                    _serverPartialPref.DedicatedServer = true;
                }
                else if (_line.StartsWith("Server port: "))
                {
                    _serverPartialPref.GamePort = (ushort)_line.GetMidStr("Server port: ", "\r\n").ToInt();
                }
                else if (_line.StartsWith("Max players: "))
                {
                    _serverPartialPref.MaxPlayerCount = _line.GetMidStr("Max players: ", "\r\n").ToInt();
                }
                else if (_line.StartsWith("Game mode:   "))
                {
                    _serverPartialPref.GameMode = _line.GetMidStr("Game mode:   ", "\r\n");
                }
                else if (_line.StartsWith("World:       "))
                {
                    _serverPartialPref.GameWorld = _line.GetMidStr("World:       ", "\r\n");
                }
                else if (_line.StartsWith("Game name:   "))
                {
                    _serverPartialPref.GameName = _line.GetMidStr("Game name:   ", "\r\n");
                }
                else if (_line.StartsWith("Difficulty:  "))
                {
                    _serverPartialPref.GameDifficulty = _line.GetMidStr("Difficulty:  ", "\r\n").ToInt();
                    SdtdConsole.Instance.RaiseReceivedServerPartialPrefEvent(_serverPartialPref);
                }
            }

        }

        /// <summary>
        /// 处理INF信息
        /// </summary>
        private void HandleINF()
        {
            if(_line.Length > _startIndexINF + 25)
            {
                // 定位到信息真实起点
                _startIndexINF += 5;

                string cmd = _line.Substring(_startIndexINF, 10);

                if (cmd == "Chat (from")// 聊天信息
                {
                    HandleChatMessage();
                }
                else if (cmd == "Executing ")// 执行命令
                {
                    _executingCmd = _line.GetMidStr("Executing command '", "' by", _startIndexINF);
                    _isExecutingCmd = true;
                }
                else
                {
                    cmd = _line.Substring(_startIndexINF, 6);
                    if (cmd == "Time: ")// 服务器状态信息
                    {
                        int outEnd = 0;
                        SdtdConsole.Instance.RaiseReceivedServerPartialStateEvent(new ServerPartialState()
                        {
                            OnlinePlayerCount = _line.GetMidStr("Ply: ", " ", out outEnd, _startIndexINF).ToInt(),
                            ZombieCount = _line.GetMidStr("Zom: ", " ", out outEnd, outEnd).ToInt(),
                            EntityCount = _line.GetMidStr("Ent: ", " ", outEnd).ToInt()
                        });
                    }
                    else if(cmd == "Entity")// 实体被击杀
                    {
                        int killerEntityID = -1,  deadEntityID = -1;
                        if (SdtdConsole.Instance.ServerVersion < ServerVersion.A18)
                        {
                            deadEntityID = _line.GetMidStr("Entity ", " ", _startIndexINF).ToInt();
                            killerEntityID = _line.GetMidStr("killed by ", ".", _startIndexINF).ToInt();                            
                        }
                        else
                        {
                            deadEntityID = _line.GetMidStr(" ", " ", _startIndexINF + 7).ToInt();
                            int index = _line.LastIndexOf(" ") + 1;
                            killerEntityID = _line.Substring(index, _line.Length - index - 2).ToInt();
                        }
                        SdtdConsole.Instance.RaiseEntityKilledEvent(killerEntityID, deadEntityID);
                    }
                    else
                    {
                        cmd = _line.Substring(_startIndexINF, 20);

                        if (cmd == "RequestToSpawnPlayer")// 玩家请求进入游戏
                        {
                            RequestOnlinePlayerInfo();
                            if(_requestDataTimer.Enabled == false)
                            {
                                _requestDataTimer.Start();
                                SdtdConsole.Instance.RaiseServerHavePlayerAgainEvent();
                            }
                        }
                        else if (cmd == "PlayerSpawnedInWorld")// 玩家生成
                        {
                            string reason = _line.GetMidStr("reason: ", ",", _startIndexINF);
                            string steamID = _line.GetMidStr("PlayerID='", "',", _startIndexINF);

                            if (reason == "JoinMultiplayer")// 玩家进入游戏而不是复活重生
                            {
                                if (_onlinePlayers.ContainsKey(steamID) == false)
                                {
                                    Log.Warn("玩家进入游戏但未处于在线玩家列表中 steamID：" + steamID);
                                }
                                else
                                {
                                    SdtdConsole.Instance.RaisePlayerEnterGameEvent(_onlinePlayers[steamID]);
                                }
                            }
                            else if (reason == "Died")// 玩家死亡
                            {
                                if (_onlinePlayers.ContainsKey(steamID) == false)
                                {
                                    Log.Warn("玩家死亡但未处于在线玩家列表中 steamID: " + steamID);
                                }
                                else
                                {
                                    SdtdConsole.Instance.RaisePlayerDiedEvent(_onlinePlayers[steamID]);
                                }
                            }
                        }
                        else if (cmd == "Player disconnected:")// 玩家退出游戏
                        {
                            string steamID = _line.GetMidStr("PlayerID='", "',", _startIndexINF);
                            if (_onlinePlayers.ContainsKey(steamID) == false)
                            {
                                Log.Warn("玩家退出游戏但未处于在线玩家列表中 steamID: " + steamID);
                            }
                            else
                            {
                                SdtdConsole.Instance.RaisePlayerLeftGameEvent(_onlinePlayers[steamID]);
                                RequestOnlinePlayerInfo();
                            }
                        }
                    }                    
                }
            }         
        }

        /// <summary>
        /// 处理聊天信息
        /// </summary>
        private void HandleChatMessage()
        {
            int outEnd = 0;
            string steamID = _line.GetMidStr("(from '", "'", out outEnd, _startIndexINF);// SteamID

            string chatTypeStr = _line.GetMidStr("to '", "'", out outEnd, outEnd);// 聊天类型

            string message = _line.GetMidStr("': ", Environment.NewLine, outEnd);// 聊天信息                                                                                    

            ChatType chatType = ChatType.Global;
            if (chatTypeStr == "Global")
            {
                chatType = ChatType.Global; // 公屏
            }
            else if (chatTypeStr == "Friends")
            {
                chatType = ChatType.Friends;// 好友
            }
            else if (chatTypeStr == "Party")
            {
                chatType = ChatType.Party;  // 队伍
            }
            else if (chatTypeStr == "Whisper")
            {
                chatType = ChatType.Whisper;// 私聊
            }

            PlayerInfo playerInfo = null;

            SenderType senderType = SenderType.Player;
            if (steamID == "-non-player-")// 如果消息来源系统
            {
                senderType = SenderType.Server;
            }
            else
            {
                if (_onlinePlayers.ContainsKey(steamID) == false)// 如果在线列表中不存在该玩家
                {
                    Log.Warn("无法处理此玩家的聊天信息 steamID: " + steamID + " 原因: 在线列表中不存在该玩家");
                }
                else
                {
                    playerInfo = _onlinePlayers[steamID];
                }
            }            
            
            SdtdConsole.Instance.RaiseChatHookEvent(playerInfo, message, chatType, senderType);
        }

        /// <summary>
        /// 检查服务器版本
        /// </summary>
        /// <param name="version"></param>
        private void CheckServerVersion(int version)
        {
            if(version < 14 && version > 1)
            {
                SdtdConsole.Instance.ServerVersion = ServerVersion.EarlierVersion;
            }
            else if(version < 20)
            {
                SdtdConsole.Instance.ServerVersion = (ServerVersion)version;
            }
            else
            {
                Log.Error("服务器版本获取失败 VersionStr: " + _serverPartialPref.VersionStr);
            }
        }
        #endregion

        #region 保护方法
        protected override void OnReceived()
        {
            while (ReadBuffer.CanReadLine)
            {
                if (_isExecutingCmd)
                {
                    HandleExecutingCommand();
                }
                else
                {
                    ReadLineToBuffer();
                    InternalHandleData();
                }
            }
        }

        protected override void OnStarted()
        {
            base.OnStarted();
        }

        protected override void OnClosed(SocketError closedReason)
        {
            _requestDataTimer.Stop();
            base.OnClosed(closedReason);
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCmd(string cmd)
        {
            this.Send((cmd + Environment.NewLine).ToUtf8());
        }
        #endregion

        #region 其他方法

        #endregion

        #endregion


    }
}