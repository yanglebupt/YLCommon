namespace YLCommon
{
    [Serializable]
    public enum Cmd
    {
        Do,
        Te,
    }

    [Serializable] 
    public class NetHeader: TCPHeader
    {
        public Cmd cmd;
    }

    [Serializable]
    public class NetBody
    {
        public string name;
    }

    [Serializable]
    public class NetBody2
    {
        public float age;
    }
}