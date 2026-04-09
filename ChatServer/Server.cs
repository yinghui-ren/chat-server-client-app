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
        List<User> listUsers = new List<User>();
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
            listener.Start();
            Log("Server started...");
            Log("Waiting for a client...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Log("[INFO]" + " A client connected: [" + client + "]");
                Receiver receiver = new Receiver(client, listUsers, userLock);
                receiver.messageHandler += HandleMessage;
                new Thread(receiver.DoOperation).Start();
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
                case "broadcast":
                    Broadcast(msg, client);
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
                    Quit(msg, client);
                    return;
            }
        }

        public void SignUp(Message msg, TcpClient client)
        {
            lock (userLock)
            {
                if (listUsers.Count == 0)
                {
                    Log("[INFO] Sign up success: [" + msg.SenderName + "]");
                    User user = new User(msg.SenderName, msg.Password, false, null);
                    listUsers.Add(user);
                    msg.Content = "signUpOK";
                    Net.sendMsg(client.GetStream(), msg);
                    return;
                }
                else
                {
                    if (CheckExistUser(listUsers, msg.SenderName) != null)
                    {
                        Log("[WARN] Sign up failed: [" + msg.SenderName + "]");
                        msg.Content = "signUpFailed";
                        Net.sendMsg(client.GetStream(), msg);
                        return;
                    }
                    else
                    {
                        Log("[INFO] Sign up success: [" + msg.SenderName + "]");
                        User user = new User(msg.SenderName, msg.Password, false, null);
                        listUsers.Add(user);
                        msg.Content = "signUpOK";
                        Net.sendMsg(client.GetStream(), msg);
                        return;
                    }
                }
            }
        }

        public void Login(Message msg, TcpClient client)
        {
            Log("To login, Reading username...");
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
                            Log("[INFO] Login success: [" + msg.SenderName + "]");
                            msg.Content = "login_ok";
                            Net.sendMsg(client.GetStream(), msg);
                            return;
                        }
                        else
                        {
                            Log("[WARN] Login failed(wrong password): [" + msg.SenderName + "]");
                            msg.Content = "wrongPassword";
                            Net.sendMsg(client.GetStream(), msg);
                            return;
                        }
                    }
                    else
                    {
                        Log("[INFO] User already online, do not log in again !");
                        msg.Content = "alreadyOnline";
                        Net.sendMsg(client.GetStream(), msg);
                    }
                }
                else
                {

                    Log("[WARN] Login failed: [" + msg.SenderName + "]");
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
                if (user.TcpClient == client)
                {
                    user.IsOnline = false;
                    msg.SenderName = user.Username;
                    Log("[INFO] User: [" + user.Username + "] log out !");
                    Net.sendMsg(client.GetStream(), msg);
                    user.TcpClient = null;
                    break;
                }
            }
        }

        public void Broadcast(Message msg, TcpClient client)
        {
            
            lock (userLock)
            {
                foreach(User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        msg.SenderName = user.Username;
                        Log("[INFO] Broadcast message sent from user: [" + user.Username + "]");
                    }
                }
                
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
                        senderNameByClient = user.Username;
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
                            Net.sendMsg(client.GetStream(), msgPriClient);
                            Log("[INFO] Private message from [" + senderNameByClient + "] to [" + msg.ReceiverName + "] successfully sent");
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
                        Log("[INFO] Username changed: [" + user.Username + "] -->> [" + msg.NewName + "]");

                        msg.SenderName = user.Username;

                        user.Username = msg.NewName;

                        Net.sendMsg(user.TcpClient.GetStream(), msg);
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
                        targetUser = user;
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
            Log("[INFO] User: [" + targetUser.Username + "] just checked the connected users.");
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
            lock (userLock)
            {
                foreach (User userTemp in listUsers)
                {
                    if (userTemp.TcpClient == client)
                    {
                        username = userTemp.Username;
                        user = userTemp;
                        break;
                    }
                }
                if (CheckExistChatRoom(listChatRooms, msg.ChatRoomName) != null)
                {
                    ChatRoom chatRoom = CheckExistChatRoom(listChatRooms, msg.ChatRoomName);
                    if (CheckExistUser(chatRoom.ListUsers, username) != null)
                    {
                        msg.Content = "userAlreadyInRoom";
                        Net.sendMsg(client.GetStream(), msg);
                        Log("[WARN] This user is already in chat room, don't join again !");
                    }
                    else
                    {
                        chatRoom.ListUsers.Add(user);
                        msg.Content = "joinOK";
                        Net.sendMsg(client.GetStream(), msg);
                        Log("[INFO] User: [" + username + "] joined room: [" + chatRoom.RoomName + "] successfully !");

                    }
                }
                else
                {
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

                }

            }
            Log("[INFO] Message sent from user: [" + msg.SenderName + "] to all users in chat room : [" + msg.ChatRoomName + "]");
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
                Log("[INFO] User: [" + userToRemove.Username + "] left room: [" + chatRoom.RoomName + "]");
                chatRoom.ListUsers.Remove(userToRemove);
                msg.Content = "leaveroomOK";
                Net.sendMsg(client.GetStream(), msg);
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
                foreach (User user in listUsers)
                {
                    if (user.TcpClient == client)
                    {
                        Log("[INFO] User: [" + user.Username + "] just checked all the rooms.");
                    }
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
                        Log("[WARN] Chat Room: [" + chatRoom.RoomName + "] just have been deleted !");
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
                        Log("[INFO] User: [" + user.Username + "] has left the chat");
                        try
                        {
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            Log("[WARN] Error closing client: " + ex.Message);
                        }

                        return;
                    }
                }
            }
            Log("[WARN] User not found");
        }

        public void Log(string message)
        {
            Console.WriteLine("[" + DateTime.Now + "] " + message);
        }

        public User CheckExistUser(List<User> listUsers, string senderName)
        {
            foreach (User user in listUsers)
            {
                if (user.Username == senderName)
                {
                    return user;
                }
            }
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
