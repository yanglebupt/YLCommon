using YLCommon;

public class Server : ITCPServer<NetMsg>
{
    public Server(short port, int connectionPoolSize = 100, int backlog = 10) : base(port, connectionPoolSize, backlog)
    {
    }

    public override void ClientConnected(ulong ID)
    {
        Logger.ColorLog(LogColor.Green, $"Client [{ID}] Connected, Has {ClientCount} Clients");
    }

    public override void ClientDisconnected(ulong ID)
    {
        Logger.ColorLog(LogColor.Green, $"Client [{ID}] DisConnected, Has {ClientCount} Clients");
    }

    public override void Message(ulong ID, NetMsg msg)
    {
        Logger.ColorLog(LogColor.Green, $"Message from {ID}, {msg.name}");
        msg.name = $"{msg.name} from [{ID}]";
        Thread.Sleep(3000);
        SendTo(ID, msg);
    }
}