using MessagePack;

namespace YLCommon
{
    public enum Cmd
    {
        Do,
        Te,
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public partial class NetHeader : TCPHeader
    {
        public Cmd cmd;
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public partial class NetBody
    {
        public string name;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public partial class NetBody2
    {
        public float age;
    }
}