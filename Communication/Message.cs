using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Communication
{
    [Serializable]
    public class Message
    {
        public string Type { get; set; }
        public string SenderName { get; set; }
        public string NewName {  get; set; }
        public string ReceiverName {  get; set; }
        public string ChatRoomName { get; set; }
        public string Content { get; set; }
        public string Password { get; set; }    
    }
}
