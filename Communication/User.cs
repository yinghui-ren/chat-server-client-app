using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public class User
    {
        public User() { }
        public User(string username, string password, bool isOnline, TcpClient client)
        {
            this.Username = username;
            this.Password = password;
            this.IsOnline = isOnline;
            this.TcpClient = client;
        }


        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsOnline { get; set; }
        public TcpClient TcpClient { get; set; }
        public string CurrentChatRoom { get; set; }
    }
}
