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
        //constructors
        public User() { }
        public User(string username,bool isOnline, TcpClient tcpClient)
        {
            this.Username = username;
            this.IsOnline = isOnline;
            this.TcpClient = tcpClient;
        }

        //properties
        //注意这种写法,默认就已经创建了username tcpclient两个变量, 但是我们不能直接访问, 只能通过property访问,
        //所以前面的构造器那里,this后面你要写成大写字母开头的Username TcpClient
        public string Username { get; set; }
        public bool IsOnline { get; set; }
        public TcpClient TcpClient { get; set; }
    }
}
