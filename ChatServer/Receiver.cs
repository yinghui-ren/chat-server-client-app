using Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChatServer
    
{
    class Receiver
    {
        public delegate void MessageHandler(Message msg, TcpClient client);
        public MessageHandler messageHandler;

        private TcpClient client;
        private List<User> listUsers;//把在server中创建的用户列表传进来
        private object userLock;

        //构造器
        public Receiver(TcpClient client, List<User> listUsers, object userLock)
        {
            this.client = client;
            this.listUsers = listUsers;
            this.userLock = userLock; 
        }




        public void DoOperation()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("doing operation...");
                    // read expression
                    Message msg = Net.rcvMsg(client.GetStream()); //2,receive接收发送过来的 用户名登录

                    messageHandler?.Invoke(msg, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Receiver error: " + ex.Message);
            }

        }

        

        
    }   
}
