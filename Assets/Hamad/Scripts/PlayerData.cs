namespace Hamad.Scripts
{
    public class PlayerData
    {
        public string name { get; private set; }
        public int tag { get; private set; }

        public PlayerData(string name, int tag)
        {
            this.name = name;
            this.tag = tag;

        }
    }


}