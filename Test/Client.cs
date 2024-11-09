using YLCommon;

public class Client : ITCPClient<NetMsg>
{
    public Client(ClientConfig config) : base(config) {}

    public override void Connected()
    {
        Logger.Info("Connected OK");
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
        Logger.Info(msg.name);
    }
}