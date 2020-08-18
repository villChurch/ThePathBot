namespace ThePathBot.Models
{
    public class Tip
    {
        public ulong SenderId { get; private set; }
        public string Message { get; private set; }
        public string Timestamp { get; private set; }

        public Tip(ulong SenderId, string Message, string Timestamp)
        {
            this.SenderId = SenderId;
            this.Message = Message;
            this.Timestamp = Timestamp;
        }
    }
}
