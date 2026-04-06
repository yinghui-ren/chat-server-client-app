using Communication;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace ChatServer
{
    public class Server
    {
        List<User> listUsers = new List<User>();//创建用户列表, 非变量
        List<ChatRoom> listChatRooms = new List<ChatRoom>();
        private readonly object userLock = new object();
        private int port;

        public Server(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            TcpListener listener = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), port);
            listener.Start();//启动监听相当于门卫, 看有没有客户端来
            Log("Server started...");
            Log("Waiting for a client...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();// 阻塞，直到有客户端连接. 有客户端连接就创建client对象
                //accept成功就能拿到client
                Log("[INFO]" + " A client connected: " + client);
                Receiver receiver = new Receiver(client, listUsers, userLock);

                receiver.messageHandler += HandleMessage;

                new Thread(receiver.DoOperation).Start();//创建的是客户端的子线程,不是客户端的主线程
                //客户端连接成功时, 新建一个子线程执行Receiver里面的DoOperation方法
                //启动客户端时,生成一个客户端进程,此进程里面默认有一个main函数为主线程,主线程为start()
                //注意进程和线程的不同
                //------
                //创建receiver对象的时候, 把server里面写的Broadcast方法作为参数传入
                //就相当于 BroadcastHandler b = Broadcast; 用这个语句将广播方法添加到delegate里面

            }

        }

        public void HandleMessage(Message msg, TcpClient client)
        {
            switch (msg.Type)
            {
                case "signup":
                    SignUp(msg, client);
                    break;
                case "login":
                    Login(msg, client);
                    break;
                case "logout":
                    LogOut(msg, client);
                    break;
                case "broadcast"://触发委托, 广播消息, 判断是否为广播类型, 并触发委托, 广播消息
                    Broadcast(msg);
                    break;
                case "private":
                    Private(msg, client);
                    break;
                case "changeUsername":
                    ChangeUsername(msg, client);
                    break;
                case "showConnectedUsers":
                    ShowConnectedUsers(msg, client);
                    break;
                case "createroom":
                    CreateRoom(msg, client);
                    break;
                case "join":
                    JoinRoom(msg, client);
                    break;
                case "sendMessageInRoom":
                    SendMessageInRoom(msg, client);
                    break;
                case "leaveroom":
                    LeaveChatRoom(msg, client);
                    break;
                case "roomscheck":
                    RoomsCheck(msg, client);
                    break;
                case "deleteroom":
                    DeleteChatRoom(msg, client);
                    break;
                case "quit":
                    Quit(msg, client);//触发委托
                    return;//结束对接某个客户端的 服务器的DoOperation子线程
            }
        }

        public void SignUp(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                if (listUsers.Count == 0)//如果list为空,直接将用户添加进list
                {
                    Log("[INFO] Sign up success: " + msg.SenderName);
                    User user = new User(msg.SenderName, msg.Password, false, null);//创建一个user并把正确的用户名和client添加进去
                    listUsers.Add(user);//将user添加到list中
                    msg.Content = "signUpOK";
                    Net.sendMsg(client.GetStream(), msg);//3,send给客户端回消息登录成功
                    return;
                }
                else//如果不为空, 则判断要添加的用户名是否存在, 已存在就重复,存储失败, 不存在就储存成功
                {
                    if (CheckExistUser(listUsers, msg.SenderName) != null)
                    {
                        Log("[WARN] Sign up failed: " + msg.SenderName);
                        msg.Content = "signUpFailed";
                        Net.sendMsg(client.GetStream(), msg);
                        return;
                    }
                    else
                    {
                        Log("[INFO] Sign up success: " + msg.SenderName);
                        User user = new User(msg.SenderName, msg.Password, false, null);//创建一个user并把正确的用户名和client添加进去
                        listUsers.Add(user);//将user添加到list中
                        msg.Content = "signUpOK";
                        Net.sendMsg(client.GetStream(), msg);//3,send给客户端回消息登录成功
                        return;
                    }
                }
            }
        }

        public void Login(Message msg, TcpClient client)
        {
            Log("reading username...");
            lock (userLock)
            {
                if (CheckExistUser(listUsers, msg.SenderName) != null)
                {
                    User user = CheckExistUser(listUsers, msg.SenderName);
                    if (!user.IsOnline)
                    {
                        if (user.Password == msg.Password)
                        {
                            user.IsOnline = true;
                            user.TcpClient = client;
                            Log("Login success: " + msg.SenderName);
                            //User user = new User(msg.SenderName, true, client);//创建一个user并把正确的用户名和client添加进去
                            //User user = new User(msg.SenderName, msg.Password, true, client);
                            //listUsers.Add(user);//将user添加到list中
                            msg.Content = "login_ok";
                            Net.sendMsg(client.GetStream(), msg);//3,send给客户端回消息登录成功
                            return;
                        }
                        else
                        {
                            Log("Login failed(wrong password): " + msg.SenderName);
                            msg.Content = "wrongPassword";
                            Net.sendMsg(client.GetStream(), msg);
                            return;
                        }
                    }
                    else
                    {
                        Log("User already online, do not log in again !");
                        msg.Content = "alreadyOnline";
                        Net.sendMsg(client.GetStream(), msg);
                    }
                }
                else
                {

                    Log("Login failed: " + msg.SenderName);
                    msg.Content = "login_failed";
                    Net.sendMsg(client.GetStream(), msg);
                    return;
                }
            }
        }

        public void LogOut(Message msg, TcpClient client)
        {
            foreach (User user in listUsers)
            {
                if(user.TcpClient == client)
                {
                    user.IsOnline = false;
                    msg.SenderName = user.Username;
                    Log("[INFO] User log out !");
                    Net.sendMsg(client.GetStream(), msg);
                    user.TcpClient = null;
                    break;
                }
            }
        }

        public void Broadcast(Message msg) //委托里面的广播方法
        {
            Log("[INFO]" + "Broadcast message send");
            lock (userLock)
            {
                foreach (User user in listUsers)
                {
                    if (user.IsOnline)
                    {
                        Net.sendMsg(user.TcpClient.GetStream(), msg);
                    }
                }
            }
        }

        public void Private(Message msg, TcpClient client)
        {
            bool found = false;
            string senderNameByClient = "";

            lock (userLock)
            {
                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        senderNameByClient = user.Username;//从list获得最新发送者的名字,下面打印语句用
                        break;
                    }
                }

                foreach (User user in listUsers)
                {
                    if (user.Username == msg.ReceiverName)
                    {
                        if (user.IsOnline)
                        {
                            msg.SenderName = senderNameByClient;
                            Net.sendMsg(user.TcpClient.GetStream(), msg);
                            Message msgPriClient = new Message
                            {
                                SenderName = senderNameByClient,
                                Type = "private",
                                Content = "senderSide"
                            };
                            Net.sendMsg(client.GetStream(), msgPriClient);//给发送者也发一份, 这样client主线程才能读到更改后的iswaiting和receivernamefound
                            Log("[INFO] private message from " + senderNameByClient + " to " + msg.ReceiverName + " successfully send");
                            found = true;
                            return;
                        }
                        else
                        {
                            msg.Content = "userOffline";
                            Net.sendMsg(client.GetStream(), msg);
                            Log("[WARN] User offline, message send failed");
                            return;
                        }
                    }
                }
                msg.Content = "userNotFound";
                Net.sendMsg(client.GetStream(), msg);
                Log("[WARN] User not found, message send failed");
            }

            if (!found)
            {
                Log("[WARN] username does not exist");
            }
        }

        public void ChangeUsername(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        Log("[INFO]" + "Username changed: " + user.Username + " -> " + msg.NewName);//此时username为旧名字, newname为新名字

                        msg.SenderName = user.Username;//将在list中存储的username传入msg,以保证最新,等下发送

                        user.Username = msg.NewName;//将list中存的username更新

                        Net.sendMsg(user.TcpClient.GetStream(), msg);
                        //此时sendername为发送者名字, newname还是新名字
                        return;
                    }
                }
            }

            Log("[WARN] User not found");
        }

        public void ShowConnectedUsers(Message msg, TcpClient client)
        {
            User targetUser = null;

            lock (userLock)
            {
                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        targetUser = user;//找到当前发送者并存下来, 待会儿名单发给他一个人
                        break;
                    }
                }
                foreach (User user in listUsers)
                {
                    if (user.IsOnline == true)
                    {
                        Message msgReply = new Message
                        {
                            Type = "showConnectedUsers",
                            SenderName = user.Username
                        };
                        Net.sendMsg(targetUser.TcpClient.GetStream(), msgReply);
                    }
                }
            }
            Log("[INFO] User: " + targetUser.Username + " just checked the connected users.");
            return;
        }

        public void CreateRoom(Message msg, TcpClient client)
        {
            List<User> listUsersOfEachRoom = new List<User>();
            ChatRoom room = new ChatRoom();

            lock (userLock)
            {
                if (listChatRooms.Count == 0)
                {
                    foreach (User user in listUsers)
                    {
                        if (user.TcpClient == client)
                        {
                            user.CurrentChatRoom = msg.ChatRoomName;
                            //listUsersOfEachRoom.Add(user);
                            break;
                        }
                    }
                    room.RoomName = msg.ChatRoomName;
                    room.ListUsers = listUsersOfEachRoom;
                    listChatRooms.Add(room);
                    msg.Content = "roomCreatedOK";
                    Log("[INFO] Chat room created successfully !");

                }
                else
                {
                    if (CheckExistChatRoom(listChatRooms, msg.ChatRoomName) != null)
                    {
                        msg.Content = "roomCreatedFailed";
                        Log("[WARN] Chat room name already exist");
                    }
                    else
                    {
                        //foreach (User user in listUsers)
                        //{
                        //    if (user.TcpClient == client)
                        //    {
                        //        listUsersOfEachRoom.Add(user);
                        //        break;
                        //    }
                        //}
                        room.RoomName = msg.ChatRoomName;
                        room.ListUsers = listUsersOfEachRoom;
                        listChatRooms.Add(room);
                        msg.Content = "roomCreatedOK";
                        Log("[INFO] Chat room created successfully !");
                    }
                }
            }
            Net.sendMsg(client.GetStream(), msg);
        }

        public void JoinRoom(Message msg, TcpClient client)
        {
            string username = null;
            User user = null;
            Console.WriteLine("test1");
            lock (userLock)
            {
                foreach (User userTemp in listUsers)
                {
                    Console.WriteLine("test2");
                    if (userTemp.TcpClient == client)
                    {
                        username = userTemp.Username;
                        user = userTemp;
                        break;
                    }
                }
                Console.WriteLine("test3");
                if (CheckExistChatRoom(listChatRooms, msg.ChatRoomName) != null)
                {
                    Console.WriteLine("test4");
                    ChatRoom chatRoom = CheckExistChatRoom(listChatRooms, msg.ChatRoomName);
                    Console.WriteLine("test224");
                    if (CheckExistUser(chatRoom.ListUsers, username) != null)
                    {
                        Console.WriteLine("test5");
                        msg.Content = "userAlreadyInRoom";
                        Net.sendMsg(client.GetStream(), msg);
                        Log("[WARN] This user is already in chat room, don't join again !");
                    }
                    else
                    {
                        Console.WriteLine("test6");
                        chatRoom.ListUsers.Add(user);
                        msg.Content = "joinOK";
                        Net.sendMsg(client.GetStream(), msg);
                        Log("[INFO] User joined room successfully !");

                    }
                }
                else
                {
                    Console.WriteLine("test7");
                    msg.Content = "roomNotFound";
                    Net.sendMsg(client.GetStream(), msg);
                    Log("[WARN] Join failed, room not found !");
                }
            }
        }

        public void SendMessageInRoom(Message msg, TcpClient client)
        {
            string senderName = null;
            lock (userLock)
            {
                ChatRoom room = CheckExistChatRoom(listChatRooms, msg.ChatRoomName);

                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        senderName = user.Username;
                    }
                }
                foreach (User user in room.ListUsers)
                {
                    msg.SenderName = senderName;
                    Net.sendMsg(user.TcpClient.GetStream(), msg);
                    Log("[INFO] Message sent to all users in chat room : " + msg.ChatRoomName);
                }
            }
        }

        public void LeaveChatRoom(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                ChatRoom chatRoom = CheckExistChatRoom(listChatRooms, msg.ChatRoomName);
                User userToRemove = null;
                foreach (User user in chatRoom.ListUsers)
                {
                    if (user.TcpClient == client)
                    {
                        userToRemove = user;
                        break;
                    }
                }
                chatRoom.ListUsers.Remove(userToRemove);
                msg.Content = "leaveroomOK";
                Net.sendMsg(client.GetStream(), msg);
                Log("[INFO] User left room: " + chatRoom.RoomName);
                foreach (User user in chatRoom.ListUsers)
                {
                    msg.Content = "notificationsToOtherClients";
                    msg.SenderName = userToRemove.Username;
                    Net.sendMsg(user.TcpClient.GetStream(), msg);
                }
            }
        }

        public void RoomsCheck(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                foreach (ChatRoom chatRoom in listChatRooms)
                {
                    string content = chatRoom.RoomName;
                    msg.Content = content;
                    Net.sendMsg(client.GetStream(), msg);
                }
            }
        }

        public void DeleteChatRoom(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                foreach (ChatRoom chatRoom in listChatRooms)
                {
                    if (chatRoom.RoomName == msg.ChatRoomName)
                    {
                        listChatRooms.Remove(chatRoom);
                        Net.sendMsg(client.GetStream(), msg);
                        break;
                    }
                }
            }

        }

        public void Quit(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {

                        user.IsOnline = false;
                        Log("[INFO]" + " user: " + user.Username + " has left the chat");
                        try
                        {
                            client.Close();//关闭服务器端的socket, 关闭网络连接TCP STREAM
                        }
                        catch (Exception ex)
                        {
                            Log("[WARN] Error closing client: " + ex.Message);
                        }

                        return;//结束Quit方法
                    }
                }
            }
            Log("[WARN] User not found");
        }

        public void Log(string message) //打印各种系统警告和系统消息用
        {
            Console.WriteLine("[" + DateTime.Now + "] " + message);
        }

        public User CheckExistUser(List<User> listUsers, string senderName)
        {
            foreach (User user in listUsers)
            {
                if (user.Username == senderName)
                {
                    Console.WriteLine("TEST222");
                    return user;
                }
            }
            Console.WriteLine("TEST223");
            return null;
        }

        public ChatRoom CheckExistChatRoom(List<ChatRoom> listChatRooms, string chatRoomName)
        {
            foreach (ChatRoom chatRoom in listChatRooms)
            {
                if (chatRoom.RoomName == chatRoomName)
                {
                    return chatRoom;
                }
            }
            return null;
        }

    }
}
