using PENet;
using AOIProtocol;

public class ClientSession : AsyncSession<Package>
{
    protected override void OnConnected(bool result)
    {
        this.LogGreen("Connected to server: {0}.", result);
    }

    protected override void OnDisConnected()
    {
        this.Warn("Disconnected from server.");
    }

    protected override void OnReceiveMsg(Package msg)
    {
        GameRoot.Instance.AddMsgPackage(msg);
        // this.LogGreen($"Received message from server: {msg}.");
    }
}

