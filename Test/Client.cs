using YLCommon;

public class Client : ITCPClient<NetMsg>
{
    public Client(string ip, short port) : base(ip, port) {}

    public override void Connected()
    {
        Logger.ColorLog(LogColor.Green, "Connected OK");
    }

    public override void ConnectionFailed()
    {
        Logger.Error("Connection Failed");
    }

    public override void Disconnected()
    {
        Logger.Warn("Disconnect");
    }

    public override void Message(NetMsg msg)
    {
        Logger.ColorLog(LogColor.Green, msg.name);
    }
}