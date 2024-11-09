using YLCommon;

public class Server : ITCPServer<NetMsg>
{
    public Server(short port, bool startImmediately = false, int connectionPoolSize = 100, int backlog = 10) : base(port, startImmediately, connectionPoolSize, backlog)
    {
    }

    public override void ClientConnected(ulong ID)
    {
        Logger.Info($"Client [{ID}] Connected, Has {ClientCount} Clients");
    }

    public override void ClientDisconnected(ulong ID)
    {
        Logger.Info($"Client [{ID}] DisConnected, Has {ClientCount} Clients");
    }

    public override void Message(ulong ID, NetMsg msg)
    {
        Logger.Info($"Message from {ID}, {msg.name}");
        msg.name = $"{msg.name} from [{ID}]";
        SendAll(msg, ID);
    }
}