# Server Basics and Login System
On the server we:
- Create a GameObject called Server in the Main scene.
- Create a ServerManager script in Scripts.
- Add a XmlUnityServer component to the Server gameobject.
- Add the ServerManager to the server gameobject.

In the ServerManager script add the following code:

```csharp
 public static ServerManager Instance;

    [Header("References")]
    public XmlUnityServer XmlServer;

    public DarkRiftServer Server;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }
```
pretty similar to the GlobalManager of the client.

(and you will need the following directives at the top of the file)
```csharp
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;
```

Your scene should look now like this:\
![](https://i.imgur.com/5qcXtSz.png)


As a next step will add functionality to the ServerManager to receive messages from clients. To do that we have to subscribe to events of the Darkrift Server.
```csharp
void Start()
    {
        Server = XmlServer.Server;
        Server.ClientManager.ClientConnected += OnClientConnect;
        Server.ClientManager.ClientDisconnected += OnClientDisconnect;
    }

    void OnDestroy()
    {
        Server.ClientManager.ClientConnected -= OnClientConnect;
        Server.ClientManager.ClientDisconnected -= OnClientDisconnect;
    }

    private void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
    {
        e.Client.MessageReceived -= OnMessage;
    }

    private void OnClientConnect(object sender, ClientConnectedEventArgs e)
    {
        e.Client.MessageReceived += OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
    }
```

The code first subscribes to the ClientConnected and ClientDisconnected events. These events get called when clients connect and disconnect. We also subscribe to the IClient.MessageRecieved event when a client connects so that we receive messages from all clients in OnMessage(). This lets us process our LoginRequests from the clients by adding code in the OnMessage function:

```csharp
  private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient) sender;
        using (Message m = e.GetMessage())
        {
            switch ((Tags) m.Tag)
            {
                case Tags.LoginRequest:
                    OnclientLogin(client, m.Deserialize<LoginRequestData>());
                    break;
            }
        }
    }
```
This code just gets the Client which sent the message and the message itself from the MessageReceived event and then uses a switch to run a function depending on the Tag of the message, it should be pretty much self explaining.


We also have to create the function that gets called:
```csharp
    private void OnclientLogin(IClient client, LoginRequestData data)
    {
        //check if player is already logged in (name already chosen in our case) and if not create a new object to represent a logged in client.
    }
```

::: danger 
Usually you would create a LoginManager on the server which talks to a backend to verify the login request and then you generate a session token, but we will keep it simple here and just check that no duplicate users are logged in(Each player has a unique name).
:::

Now we have to create an object to represent a logged in player, so create a new script "ClientConnection" in the Scripts folder. And change its code to this:
```csharp
using DarkRift;
using DarkRift.Server;

[System.Serializable]
public class ClientConnection
{

    [Header("Public Fields")]
    public string Name;
    public IClient Client;

    public ClientConnection(IClient client , LoginRequestData data)
    {
        Client = client;
        Name = data.Name;

        ServerManager.Instance.Players.Add(client.ID, this);
        ServerManager.Instance.PlayersByName.Add(Name, this);
     
    }
```

And add Dictionaries in the ServerManager to store the logged in players:
```csharp
    public Dictionary<ushort, ClientConnection> Players = new Dictionary<ushort, ClientConnection>();
    public Dictionary<string, ClientConnection> PlayersByName = new Dictionary<string, ClientConnection>();
```

Now we can also fill in the OnclientLogin function in the ServerManager:
```csharp
 private void OnclientLogin(IClient client, LoginRequestData data)
    {
        if (PlayersByName.ContainsKey(data.Name))
        {
            using (Message m = Message.CreateEmpty((ushort)Tags.LoginRequestDenied))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        //In the future the ClientConnection will handle its messages
        client.MessageReceived -= OnMessage;

        new ClientConnection(client, data);
    }
```
The function will first check if the name is already in use and if it is, it sends back a LoginRequestDenied data.
If the name hasn't been used yet we create a new ClientConnection and unsubscribe from client.MessageRecieved, because in the future we can use the ClientConnection to handle messages from that client.

Finally we have to send an LoginRequestAccepted message when the client has logged in successfully. This message should also contain additional information. In our case we want to let the client know his personal id on the server and information about the lobby and game rooms. (We will create a room system in the next section)

So open the Networking Data **in the Client Project (Remember the junction only works in 1 direction)** and add:
```csharp
public struct LoginInfoData : IDarkRiftSerializable
{
    public ushort Id;
    public LobbyInfoData Data;

    public LoginInfoData(ushort id, LobbyInfoData data)
    {
        Id = id;
        Data = data;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
        Data = e.Reader.ReadSerializable<LobbyInfoData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
        e.Writer.Write(Data);
    }
}

public struct LobbyInfoData : IDarkRiftSerializable
{
    public LobbyInfoData()
    {
    }

    public void Deserialize(DeserializeEvent e)
    {
    }

    public void Serialize(SerializeEvent e)
    {
    }
}
```

LoginInfoData is the information that the client will receive after logging in and it includes LobbyInfoData which contains information about the lobby(currently empty).

now lets send that message at the end of the constructor of the ClientConnection script, it should look like this:
```csharp
    public ClientConnection(IClient client , LoginRequestData data)
    {
        Client = client;
        Name = data.Name;

        ServerManager.Instance.Players.Add(client.ID, this);
        ServerManager.Instance.PlayersByName.Add(Name, this);

       
        using (Message m = Message.Create((ushort)Tags.LoginRequestAccepted, new LoginInfoData(client.ID, new LobbyInfoData())))
        {
            client.SendMessage(m, SendMode.Reliable);
        }
    }
```

The scripts should now look like this:
- [ServerManager](https://pastebin.com/V4eKfd3Q)
- [ClientConnection](https://pastebin.com/yRxZ49u4)
- [Networking Data](https://pastebin.com/2vvSRGVB)

In the next section we will create a room system and add information about rooms to the LobbyInfoData.