using Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class Receiver
    {
        //创建receiver对象的时候, 把server里面写的Broadcast方法作为参数传入
        //就相当于 BroadcastHandler b = Broadcast; 用这个语句将广播方法添加到delegate里面
        public delegate void Login(Message msg, TcpClient client, Action<string> logHandler);
        public delegate void BroadcastHandler(Message msg);//定义委托
        public delegate void PrivateHandler(Message msg, TcpClient client);
        public delegate void ChangeUsernameHandler(Message msg, TcpClient client);
        public delegate void ShowConnectedUsers(Message msg, TcpClient client);
        public delegate void Quit(Message msg, TcpClient client); 

        
        

        private TcpClient client;
        private List<User> listUsers;//把在server中创建的用户列表传进来
        private Login login;
        private BroadcastHandler broadcastHandler;//添加对应的委托参数是为了创建Receiver对象的时候把方法参数写进去,会自动将方法传入对应delegate列表,之后调用delegate就可以触发对应的方法了
        private PrivateHandler privateHandler;
        private ChangeUsernameHandler changeUsernameHandler;
        private ShowConnectedUsers showConnectedUsers;
        private Quit quit;
        private Action<string> logHandler;//传server里面写的log函数用

        //构造器
        public Receiver(TcpClient client, List<User> listUsers,Login login, BroadcastHandler broadcastHandler, PrivateHandler privateHandler, 
            ChangeUsernameHandler changeUsernameHandler, ShowConnectedUsers showConnectedUsers, Quit quit, Action<string> logHandler)
        {
            this.client = client;
            this.listUsers = listUsers;
            this.login = login;
            this.broadcastHandler = broadcastHandler;
            this.privateHandler = privateHandler;
            this.changeUsernameHandler = changeUsernameHandler;
            this.showConnectedUsers = showConnectedUsers;
            this.quit = quit;
            this.logHandler = logHandler;   
        }




        public void DoOperation()
        {
            try
            {
                while (true)
                {
                    logHandler("doing operation...");
                    // read expression
                    Message msg = Net.rcvMsg(client.GetStream()); //2,receive接收发送过来的 用户名登录

                    switch (msg.Type)
                    {
                        case "login":
                            login(msg, client, logHandler);
                            break;
                        case "broadcast"://触发委托, 广播消息, 判断是否为广播类型, 并触发委托, 广播消息
                            broadcastHandler(msg);
                            break;
                        case "private":
                            privateHandler(msg, client);
                            break;
                        case "changeUsername":
                            changeUsernameHandler(msg, client);
                            break;
                        case "showConnectedUsers":
                            showConnectedUsers(msg, client);
                            break;
                        case "quit":
                            quit(msg, client);//触发委托
                            return;//结束对接某个客户端的 服务器的DoOperation子线程
                    }
                }
            }
            catch (Exception ex)
            {
                logHandler("Receiver error: " + ex.Message);
            }

        }

        

        
    }   
}
