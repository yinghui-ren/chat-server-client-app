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
        private List<User> listUsers;
        private object userLock;

        
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
                    Message msg = Net.rcvMsg(client.GetStream());

                    messageHandler?.Invoke(msg, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[" + DateTime.Now + "] [WARN] Receiver error: " + ex.Message);
            }

        }

        

        
    }   
}
