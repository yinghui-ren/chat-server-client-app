using Communication;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;

namespace ChatClient
{
    public class Client
    {
        private string serverIp;
        private int port;

        public Client(string serverIp, int port)
        {
            this.serverIp = serverIp;
            this.port = port;
        }

        public void Start()
        {

            User user = new User("", true, new TcpClient(serverIp, port));
            TcpClient client = user.TcpClient;//创建client对象就表示已经连接上服务器
            Console.WriteLine("Connection established");

            string senderName = Login(client);//登录login


            //创建新线程去持续接收消息,一定要在登录之后创建接收消息的新线程
            new Thread(() => ReceiveLoop(client)).Start();
            //给ReceiveLoop方法创建新线程,可以持续请求接收消息
            //注意新建线程这件事不要写进循环里面, 否则每次循环都会新创建一个线程, 创建很多线程最后崩溃
            //只有线程里面的方法结束,线程才会结束.比如这里线程里面的方法是一个无限循环, 那就永远不会结束



            while (true)
            {
                Console.WriteLine("----------------welcome to chat room----------------");
                Console.WriteLine("press /chat to chat");
                Console.WriteLine("press /name to change username");
                Console.WriteLine("press /users to list connected users");
                Console.WriteLine("press /quit to leave the chat ");

                Console.WriteLine("please enter the instruction");
                string chooseInstruction = Console.ReadLine();

                switch (chooseInstruction)
                {
                    case "/chat":
                        Chat(client);
                        break;
                    case "/name":
                        ChangeUsername(client);
                        break;
                    case "/users":
                        ShowConnectedUsers(client);
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

                    if (rcvMessage.Type == "broadcast")
                    {
                        Console.WriteLine("[" + rcvMessage.Type + "] " + rcvMessage.SenderName + ": " + rcvMessage.Content);
                    }
                    else if (rcvMessage.Type == "private")
                    {
                        Console.WriteLine("[" + rcvMessage.Type + "] " + rcvMessage.SenderName + ": " + rcvMessage.Content);
                    }  
                    else if (rcvMessage.Type == "changeUsername")
                    {
                        Console.WriteLine("Username changed: " + rcvMessage.SenderName + " -> " + rcvMessage.NewName);
                        Console.WriteLine("enter new username to change or press exit to exit");
                    }
                    else if (rcvMessage.Type == "showConnectedUsers")
                    {
                        Console.WriteLine("Name: " + rcvMessage.SenderName + "  Status: online");
                    }

                }
            }
            catch (Exception) //有错误时执行这个
            {
                Console.WriteLine("Disconnected from server.");
                //我的程序结束这个子线程, 全靠捕获错误, 所以上面判断null的if语句没用, 但是先暂时保留一下
            }

        }

        public string Login(TcpClient client)
        {
            //login
            while (true)
            {
                Console.WriteLine("please enter username to login");
                string senderName = Console.ReadLine();//键盘录入用户名
                Message msgSend = new Message
                {
                    Type = "login",
                    SenderName = senderName
                };
                Net.sendMsg(client.GetStream(), msgSend);//1,send把登录的用户名发送给服务器端

                Message msgRcv = Net.rcvMsg(client.GetStream());//4,receive接收服务器发送过来的登录成功的消息
                if (msgRcv.Type == "login_ok")
                {
                    msgRcv.SenderName = senderName;
                    Console.WriteLine(msgRcv.Content + " welcome Dear user " + msgRcv.SenderName);
                    return senderName;
                }
                else
                {
                    Console.WriteLine(msgRcv.Content);
                    Console.WriteLine("this username already exist, please enter your username again");
                }
            }
        }

        public void Chat(TcpClient client)
        {
            

            while (true)
            {
                Console.WriteLine("press /private to send private message");
                Console.WriteLine("press /broadcast to broadcast");
                Console.WriteLine("press exit to exit");

                string choose = Console.ReadLine();

                if (choose == "exit")
                {
                    break;
                }

                switch (choose)
                {
                    case "/broadcast"://broadcast
                        Broadcast(client);
                        break;
                    case "/private":
                        Private(client);
                        break;    
                }

            }
        }

        public void Broadcast(TcpClient client)
        {
            Console.WriteLine("please enter broadcast message");
            while (true)
            {
                string content = Console.ReadLine();
                if (content == "exit")
                {
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

        public void Private(TcpClient client)
        {
            Console.WriteLine("please enter the username you want to talk to in private(press exit to exit)");
            string targetUsername = Console.ReadLine();
            if (targetUsername == "exit")
            {
                return;
            }
            while (true)
            {
                Console.WriteLine("please enter your private message(press exit to exit)");
                string contentPri = Console.ReadLine();
                if (contentPri == "exit")
                {
                    break;
                }
                Message msgPri = new Message
                {
                    Type = "private",
                    PrivateName = targetUsername,
                    Content = contentPri
                };
                Net.sendMsg(client.GetStream(), msgPri);
            }
        }

        public void ChangeUsername(TcpClient client)
        {
            Console.WriteLine("please enter the new username");
            while (true)
            {
                string newUsername = Console.ReadLine();
                if (newUsername == "exit")
                {
                    break;
                }
                Message msgSend = new Message
                {
                    Type = "changeUsername",
                    NewName = newUsername
                };
                Net.sendMsg(client.GetStream(), msgSend);//1,send把登录的用户名发送给服务器端
            }
            
        }

        public void ShowConnectedUsers(TcpClient client)
        {
            Message msgSend = new Message
            {
                Type = "showConnectedUsers",
            };
            Net.sendMsg(client.GetStream(), msgSend);
            Console.WriteLine("Here are the connected users (press ant button to exit) :");
            Console.ReadLine();

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
