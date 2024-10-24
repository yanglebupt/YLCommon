using System.Net.Sockets;
using YLCommon;

Logger.cfg.enableSave = false;
Logger.cfg.saveOverride = true;
Logger.cfg.showTrace = false;
Logger.EnableSetting();

NetworkConfig.logger.warn = Logger.Warn;
NetworkConfig.logger.error = Logger.Error;
NetworkConfig.logger.info = Logger.Log;
NetworkConfig.logger.ok = (string m) => Logger.ColorLog(LogColor.Green, m);


Server server = new(3000);

/*
TCPServer<NetMsg> server = new(3000);
server.OnClientConnected += (ulong ID) =>
{
    Logger.ColorLog(LogColor.Green, $"Client [{ID}] Connected, Has {server.ClientCount} Clients");
};

server.OnClientDisconnected += (ulong ID) =>
{
    Logger.ColorLog(LogColor.Green, $"Client [{ID}] DisConnected, Has {server.ClientCount} Clients");
};

server.OnMessage += (ulong ID, NetMsg msg) => {
    Logger.ColorLog(LogColor.Green, $"Message from {ID}, {msg.name}");
    msg.name = $"{msg.name} from [{ID}]";
    server.SendAll(msg, ID);
};
*/

while (true)
{
    string? ipt = Console.ReadLine();
    if (ipt == null) continue;
    if (ipt == "del") server.Disconnect(101);
}


