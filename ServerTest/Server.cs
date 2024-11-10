using YLCommon;

public class Server : ITCPServer<NetHeader>
{
    public Server(ServerConfig config) : base(config)
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

    public override void Message(ulong ID, TCPMessage<NetHeader> msg)
    {
        NetBody? body = msg.GetBody<NetBody>();
        if(body != null) { 
            Logger.Info($"Message from {ID}, cmd: {msg.header.cmd}, age: {body.name}");
            body.name = $"{body.name} from [{ID}]";
            msg.SetBody(body);
            SendAll(msg, ID);
        }
    }
}