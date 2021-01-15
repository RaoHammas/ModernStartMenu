using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace ModernStartMenu_MVVM.Models
{
    public class Message : ValueChangedMessage<Message>
    {
        public Message(Message value) : base(value)
        {
        }

        public string Caption { get; set; }
        public string MessageText { get; set; }
    }
}
