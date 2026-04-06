using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public class ChatRoom
    {
        public ChatRoom() { }
        public ChatRoom(string roomName, List<User> listUsers)
        {
            this.RoomName = roomName;
            this.ListUsers = listUsers;
        }

        public string RoomName { get; set; }
        public List<User> ListUsers { get; set; }
    }
}
