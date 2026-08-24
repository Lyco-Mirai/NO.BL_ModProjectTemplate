using System;
using System.Collections.Generic;
using Mirage;
using Mirage.Authentication;
using Mirage.SocketLayer;
using Mirage.Sockets.Udp;

public class MirageRemote : IDataHandler, IDisposable
{
	private const int PORT = 7071;

	private readonly Peer peer;

	private readonly Dictionary<IConnection, NetworkPlayer> players = new Dictionary<IConnection, NetworkPlayer>();

	public readonly MessageHandler MessageHandler;

	private bool active;

	public void Dispose()
	{
		peer.Close();
		active = false;
	}

	public MirageRemote()
	{
		peer = new Peer(new NanoSocket(262144), 1200, this);
		MessageHandler = new MessageHandler(null, disconnectOnException: false);
		peer.OnConnected += delegate(IConnection conn)
		{
			NetworkPlayer networkPlayer = new NetworkPlayer(conn, isHost: false, null, null);
			networkPlayer.SetAuthentication(new PlayerAuthentication(null, null), allowReplace: true);
			players.Add(conn, networkPlayer);
		};
		peer.OnDisconnected += delegate(IConnection conn, DisconnectReason _)
		{
			players.Remove(conn);
		};
	}

	public void Bind()
	{
		NanoConnectionHandle endPoint = new NanoConnectionHandle("::0", 7071);
		peer.Bind(endPoint);
		active = true;
	}

	public void Connect()
	{
		NanoConnectionHandle endPoint = new NanoConnectionHandle("127.0.0.1", 7071);
		peer.Connect(endPoint);
		active = true;
	}

	public void Poll()
	{
		if (active)
		{
			peer.UpdateReceive();
			peer.UpdateSent();
		}
	}

	void IDataHandler.ReceiveMessage(IConnection connection, ArraySegment<byte> message)
	{
		MessageHandler.HandleMessage(players[connection], message);
	}

	public void Send<T>(T message)
	{
		foreach (NetworkPlayer value in players.Values)
		{
			value.Send(message);
		}
	}
}
