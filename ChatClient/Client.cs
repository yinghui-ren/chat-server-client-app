using Communication;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Linq;

namespace ChatClient
{
    public class Client
    {
        private string serverIp;
        private int port;
        private bool isLoggedIn = false;
        private volatile bool privateReceiverNameFound = false;
        private volatile bool isWaiting = true;
        private volatile bool isInRoom = false;
        private volatile bool waitReplyRoom = true;
        private volatile string currentRoom = null;
        public Client(string serverIp, int port)
        {
            this.serverIp = serverIp;
            this.port = port;
        }

        public void Start()
        {

            User user = new User(null, null, true, new TcpClient(serverIp, port));
            TcpClient client = user.TcpClient;//创建client对象就表示已经连接上服务器
            Console.WriteLine("[INFO] Connection established");
            Console.WriteLine();



            //创建新线程去持续接收消息,一定要在登录之后创建接收消息的新线程
            new Thread(() => ReceiveLoop(client)).Start();
            //给ReceiveLoop方法创建新线程,可以持续请求接收消息
            //注意新建线程这件事不要写进循环里面, 否则每次循环都会新创建一个线程, 创建很多线程最后崩溃
            //只有线程里面的方法结束,线程才会结束.比如这里线程里面的方法是一个无限循环, 那就永远不会结束

            Console.WriteLine("----------------welcome to chat room----------------");
            Console.WriteLine();





            while (true)
            {
                Console.WriteLine("Back to main menu.");
                string input = Console.ReadLine();
                string[] parts = input.Split(' ');
                string command = parts[0];

                if (!isLoggedIn && command != "/signup" && command != "/login")
                {
                    Console.WriteLine("Please login first.");
                    continue;
                }

                switch (command)
                {
                    case "/signup":
                        SignUp(client, parts);
                        break;
                    case "/login":
                        Login(client, parts);
                        break;
                    case "/logout":
                        LogOut(client, parts);
                        break;
                    case "/broadcast":
                        Broadcast(client, parts, input);
                        break;
                    case "/private":
                        Private(client, parts, input);
                        break;
                    case "/name":
                        ChangeUsername(client, parts);
                        break;
                    case "/users":
                        ShowConnectedUsers(client);
                        break;
                    case "/createroom":
                        CreateRoom(client, parts);
                        break;
                    case "/join":
                        JoinRoom(client, parts);
                        break;
                    case "/roomscheck":
                        RoomsCheck(client);
                        break;
                    case "/deleteroom":
                        DeleteChatRoom(client, parts);
                        break;
                    case "/quit":
                        Quit(client);
                        return;//return直接结束start()方法, 结束此客户端的主线程.
                }


            }




        }

        public void ReceiveLoop(TcpClient client)
        {
            try//试着执行下面的语句, 如果有错误就执行catch, 这样程序不会崩溃退出
            {
                while (true)
                {
                    Message rcvMessage = Net.rcvMsg(client.GetStream());

                    if (rcvMessage == null)//必须先判断不是null才能执行下面的.type, 否则会报错
                    {
                        Console.WriteLine("Disconnected from server.");
                        break;
                    }
                    switch (rcvMessage.Type)
                    {
                        case "signup":
                            if(rcvMessage.Content == "signUpOK")
                            {
                                Console.WriteLine("You successfully sign up !");
                            }else if(rcvMessage.Content == "signUpFailed")
                            {
                                Console.WriteLine("This username already exist, please enter your username again");
                            }
                            break;
                        case "login":
                            if (rcvMessage.Content == "login_ok")
                            {
                                isLoggedIn = true;
                                Console.WriteLine("Log in successful, Welcome Dear User " + rcvMessage.SenderName);
                            }
                            else if (rcvMessage.Content == "login_failed")
                            {
                                Console.WriteLine("This user does not exist, please sign up first.");
                            }else if(rcvMessage.Content == "wrongPassword")
                            {
                                Console.WriteLine("Wrong password, login failed !");
                            }else if(rcvMessage.Content == "alreadyOnline")
                            {
                                Console.WriteLine("User: [" + rcvMessage.SenderName + "] is already online, please do not sign in again.");
                            }
                            break;
                        case "logout":
                            isLoggedIn = false;
                            Console.WriteLine("User: " + rcvMessage.SenderName + " log out !");
                            break;
                        case "broadcast":
                            Console.WriteLine("[" + rcvMessage.Type + "]" + rcvMessage.SenderName + ": " + rcvMessage.Content);
                            break;
                        case "private":
                            if (rcvMessage.Content == "userOffline")
                            {
                                Console.WriteLine("User offline, message send failed, please enter another target username");
                            }
                            else if (rcvMessage.Content == "userNotFound")
                            {
                                isWaiting = false;
                                Console.WriteLine("User not found, message send failed, please enter another target username");
                            }
                            else
                            {
                                isWaiting = false;
                                privateReceiverNameFound = true;
                                if (rcvMessage.Content != "senderSide")
                                {

                                    Console.WriteLine("[" + rcvMessage.Type + "] " + rcvMessage.SenderName + ": " + rcvMessage.Content);
                                }
                            }
                            break;
                        case "changeUsername":
                            Console.WriteLine("Username changed: " + rcvMessage.SenderName + " -> " + rcvMessage.NewName);
                            Console.WriteLine("enter new username to change or press exit to exit");
                            break;
                        case "showConnectedUsers":
                            Console.WriteLine("Name: " + rcvMessage.SenderName + "  Status: online");
                            break;
                        case "createroom":
                            if (rcvMessage.Content == "roomCreatedOK")
                            {
                                Console.WriteLine("Chat room created successfully !");
                            }
                            else if (rcvMessage.Content == "roomCreatedFailed")
                            {
                                Console.WriteLine("Chat room created failed !");
                            }
                            break;
                        case "join":
                            if (rcvMessage.Content == "userAlreadyInRoom")
                            {
                                waitReplyRoom = false;
                                isInRoom = true;
                                currentRoom = rcvMessage.ChatRoomName;
                                Console.WriteLine("This user is already in chat room, don't join again !");
                            }
                            else if (rcvMessage.Content == "roomNotFound")
                            {
                                waitReplyRoom = false;
                                Console.WriteLine("Join failed, room not found !");
                            }
                            else if (rcvMessage.Content == "joinOK")
                            {
                                waitReplyRoom = false;
                                isInRoom = true;
                                currentRoom = rcvMessage.ChatRoomName;
                                Console.WriteLine("You have successfully joined the room !");
                            }
                            break;
                        case "sendMessageInRoom":
                            Console.WriteLine("[Room:" + rcvMessage.ChatRoomName + "] " + rcvMessage.SenderName + ": " + rcvMessage.Content);
                            break;
                        case "leaveroom":
                            if (rcvMessage.Content == "leaveroomOK")
                            {
                                Console.WriteLine("You have left the room.");
                            }else if(rcvMessage.Content == "notificationsToOtherClients")
                            {
                                Console.WriteLine(rcvMessage.SenderName + " has left the room.");
                            }
                            break;
                        case "roomscheck":
                            Console.WriteLine("Room name: " + rcvMessage.Content);
                            break;
                        case "deleteroom":
                            Console.WriteLine("Room : " + rcvMessage.ChatRoomName + " deleted !");
                            break;
                    }
                }
            }
            catch (Exception) //有错误时执行这个
            {
                Console.WriteLine("Disconnected from server.");
                //我的程序结束这个子线程, 全靠捕获错误, 所以上面判断null的if语句没用, 但是先暂时保留一下
            }

        }//创建子线程持续接收消息

        public void SignUp(TcpClient client, string[] parts)
        {
            string senderName = parts[1];
            string password = parts[2];
            Message msg = new Message { 
            Type = "signup",
            SenderName = senderName,
            Password = password
            };
            Net.sendMsg(client.GetStream(), msg);
        }

        public void Login(TcpClient client, string[] parts)
        {
            //login

            string senderName = parts[1];
            string password = parts[2];
            Message msgSend = new Message
            {
                Type = "login",
                SenderName = senderName,
                Password = password
            };
            Net.sendMsg(client.GetStream(), msgSend);//1,send把登录的用户名发送给服务器端
        }

        public void LogOut(TcpClient client, string[] parts)
        {
            Message msgSend = new Message
            {
                Type = "logout"
            };
            Net.sendMsg(client.GetStream(), msgSend);
        }

        public void Broadcast(TcpClient client, string[] parts, string input)
        {
            //第一次发送需要截掉command
            Message msg = new Message
            {
                Type = "broadcast",
                Content = input.Substring(parts[0].Length + 1)
            };
            Net.sendMsg(client.GetStream(), msg);

            //之后就发送全句就可以
            Console.WriteLine("you entered broadcast mode, press /exit to exit");
            while (true)
            {
                string content = Console.ReadLine();
                if (content == "/exit")
                {
                    Console.WriteLine("broadcast mode out");
                    break;
                }
                Message msgBroadcast = new Message
                {
                    Type = "broadcast",
                    Content = content
                };
                Net.sendMsg(client.GetStream(), msgBroadcast);
            }
        }

        public void Private(TcpClient client, string[] parts, string input)
        {

            string targetUsername = parts[1];

            //第一次发送的前两个数组元素需要截掉
            Message msg = new Message
            {
                Type = "private",
                ReceiverName = targetUsername,
                Content = input.Substring(parts[0].Length + 1 + parts[1].Length + 1)
            };
            Net.sendMsg(client.GetStream(), msg);

            while (true)
            {
                if (isWaiting)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else if (!privateReceiverNameFound)
                {
                    isWaiting = true;
                    return;
                }

                Console.WriteLine("you entered private mode, press /exit to exit");

                while (true)
                {
                    string contentPri = Console.ReadLine();
                    if (contentPri == "/exit")
                    {
                        isWaiting = true;
                        privateReceiverNameFound = false;
                        Console.WriteLine("private mode out");
                        return;
                    }
                    Message msgPri = new Message
                    {
                        Type = "private",
                        ReceiverName = targetUsername,
                        Content = contentPri
                    };
                    Net.sendMsg(client.GetStream(), msgPri);
                }
            }
        }

        public void ChangeUsername(TcpClient client, string[] parts)
        {
            string newUsername = parts[1];
            Message msgSend = new Message
            {
                Type = "changeUsername",
                NewName = newUsername
            };
            Net.sendMsg(client.GetStream(), msgSend);//1,send把登录的用户名发送给服务器端
        }

        public void ShowConnectedUsers(TcpClient client)
        {
            Message msgSend = new Message
            {
                Type = "showConnectedUsers",
            };
            Net.sendMsg(client.GetStream(), msgSend);
            Console.WriteLine("Here are the connected users :");

        }

        public void CreateRoom(TcpClient client, string[] parts)
        {
            string chatRoomName = parts[1];
            Console.WriteLine("test create room");
            Message msg = new Message
            {
                Type = "createroom",
                ChatRoomName = chatRoomName

            };
            Net.sendMsg(client.GetStream(), msg);
        }

        public void JoinRoom(TcpClient client, string[] parts)
        {
            string currentRoom = parts[1];
            Message msg = new Message
            {
                Type = "join",
                ChatRoomName = currentRoom
            };
            Net.sendMsg(client.GetStream(), msg);
            Console.WriteLine("testtttt join room");

            SendMessageInRoom(client, parts);
        }

        public void SendMessageInRoom(TcpClient client, string[] parts)
        {
            Console.WriteLine(waitReplyRoom);
            Console.WriteLine(isInRoom);
            while (true)
            {
                if (waitReplyRoom)
                {
                    Thread.Sleep(500);
                    Console.WriteLine("TEST 227");
                    continue;
                }
                else if (!isInRoom)
                {
                    waitReplyRoom = true;
                    Console.WriteLine("TEST 226");
                    return;
                }
                break;
            }

            Console.WriteLine("You've entered the chat room.");

            while (true)
            {
                string content = Console.ReadLine();
                if (content == "/exit")
                {
                    waitReplyRoom = true;
                    isInRoom = false;
                    LeaveChatRoom(client);
                    return;
                }
                Message msg = new Message
                {
                    Type = "sendMessageInRoom",
                    Content = content,
                    ChatRoomName = currentRoom
                };
                Net.sendMsg(client.GetStream(), msg);
            }
        }

        public void LeaveChatRoom(TcpClient client)
        {
            Message msg = new Message
            {
                Type = "leaveroom",
                ChatRoomName= currentRoom
            };
            currentRoom = null;
            Net.sendMsg(client.GetStream(), msg);
        }

        public void RoomsCheck(TcpClient client)
        {
            Message msg = new Message
            {
                Type = "roomscheck"
            };
            Net.sendMsg(client.GetStream(), msg);
        }

        public void DeleteChatRoom(TcpClient client, string[] parts)
        {
            string roomName = parts[1];
            Message msg = new Message
            {
                Type = "deleteroom",
                ChatRoomName = roomName
            };
            Net.sendMsg(client.GetStream(), msg);
        }

        public void Quit(TcpClient client)
        {
            try
            {
                Message msgSend = new Message
                {
                    Type = "quit"
                };
                Net.sendMsg(client.GetStream(), msgSend);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send quit message: " + ex.Message);
            }

            try
            {
                client.Close();//关闭客户端的socket, 关闭网络连接TCP STREAM
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to close client: " + ex.Message);
            }

            Console.WriteLine("You left the chat room");
        }




    }
}
