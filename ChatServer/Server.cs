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

                new Thread(new Receiver(client, listUsers, Login, Broadcast, Private, ChangeUsername, ShowConnectedUsers, Quit, Log).DoOperation).Start();//创建的是客户端的子线程,不是客户端的主线程
                //客户端连接成功时, 新建一个子线程执行Receiver里面的DoOperation方法
                //启动客户端时,生成一个客户端进程,此进程里面默认有一个main函数为主线程,主线程为start()
                //注意进程和线程的不同
                //------
                //创建receiver对象的时候, 把server里面写的Broadcast方法作为参数传入
                //就相当于 BroadcastHandler b = Broadcast; 用这个语句将广播方法添加到delegate里面

            }

        }

        public void Login(Message msg, TcpClient client, Action<string> logHandler)
        {
            logHandler("reading username...");
            if (listUsers.Count == 0)//如果list为空,直接将用户添加进list
            {
                logHandler("Login success: " + msg.SenderName);
                User user = new User(msg.SenderName, true, client);//创建一个user并把正确的用户名和client添加进去
                listUsers.Add(user);//将user添加到list中
                Message reply = new Message
                {
                    SenderName = msg.SenderName,
                    Type = "login_ok",
                    Content = "Login successful"
                };
                Net.sendMsg(client.GetStream(), reply);//3,send给客户端回消息登录成功
                return;
            }
            else//如果不为空, 则判断要添加的用户名是否存在, 已存在就重复,存储失败, 不存在就储存成功
            {
                if (CheckExist(listUsers, msg.SenderName))
                {
                    logHandler("Login failed: " + msg.SenderName);
                    Message reply = new Message
                    {
                        SenderName = msg.SenderName,
                        Type = "login_failed",
                        Content = "Login failed"
                    };
                    Net.sendMsg(client.GetStream(), reply);
                    return;
                }
                else
                {
                    logHandler("Login success: " + msg.SenderName);
                    User user = new User(msg.SenderName, true, client);//创建一个user并把正确的用户名和client添加进去
                    listUsers.Add(user);//将user添加到list中
                    Message reply = new Message
                    {
                        SenderName = msg.SenderName,
                        Type = "login_ok",
                        Content = "Login successful"
                    };
                    Net.sendMsg(client.GetStream(), reply);//3,send给客户端回消息登录成功
                    return;
                }
            }
        }

        public void Broadcast(Message msg) //委托里面的广播方法
        {
            Log("[INFO]" + "Broadcast message send");
            foreach (User user in listUsers)
            {
                Net.sendMsg(user.TcpClient.GetStream(), msg);
            }
        }

        public void Private(Message msg, TcpClient client)
        {
            bool found = false;
            string senderNameByClient = "";
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
                if (user.Username == msg.PrivateName)
                {
                    msg.SenderName = senderNameByClient;
                    Net.sendMsg(user.TcpClient.GetStream(), msg);
                    Log("[INFO]" + " private message from " + senderNameByClient + " to " + msg.PrivateName + " successfully send");
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Log("[WARN] username does not exist");
            }
        }

        public void ChangeUsername(Message msg, TcpClient client)
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

            Log("[WARN] User not found");
        }

        public void ShowConnectedUsers(Message msg, TcpClient client)
        {
            User targetUser = null;
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
            Log("[INFO] Connected users successfully send to user: " + targetUser.Username);
            return;
        }

        public void Quit(Message msg, TcpClient client)
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
            Log("[WARN] User not found");
        }
            
        public void Log(string message) //打印各种系统警告和系统消息用
        {
            Console.WriteLine("[" + DateTime.Now + "] " + message);
        }

        public bool CheckExist(List<User> listUsers, string senderName)
        {
            foreach (User user in listUsers)
            {
                if (user.Username == senderName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
