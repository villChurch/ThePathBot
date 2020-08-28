namespace ThePathBot.Models
{
    public class QueueMember
    {
        public ulong DiscordID { get; set; }
        public bool OnIsland { get; set; }
        public int GroupNumber { get; set; }
        public int PlaceInGroup { get; set; }
        public QueueMember(ulong DiscordID, bool OnIsland, int GroupNumber, int PlaceInGroup)
        {
            this.DiscordID = DiscordID;
            this.OnIsland = OnIsland;
            this.GroupNumber = GroupNumber;
            this.PlaceInGroup = PlaceInGroup;
        }
    }
}