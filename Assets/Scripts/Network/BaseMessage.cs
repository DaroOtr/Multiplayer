using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;

// Hacer los mensajes de ping que se tiene que mandar siempre
// El primero en mandar el mensaje es el cliente y no se vuelve a mandar hasta obtener la respuesta del servver
// Es un tipo de mensaje unico


public enum MessageType
{
    ServerToClientHS = -1,
    ClientToServerHS = -2,
    Ping = 0,
    Console = 1,
    Position = 2,
    PlayerList = 3
}

public enum ObjectType
{
    Bullet = 0,
    Player = 1
}

public abstract class BaseMessage<PayloadType>
{
    protected PayloadType data;

    public static Action<PayloadType> OnDispatch;

    public abstract MessageType GetMessageType();
    public abstract byte[] Serialize();
    public abstract PayloadType Deserialize(byte[] message);
    public abstract PayloadType GetData();

    public virtual void EncryptMessage(List<byte> message)
    {
        uint checksum1 = 0;
        uint checksum2 = 0;
        int messageLenght = message.Count;
        for (int i = 0; i < messageLenght; i++)
        {
            int temp = message[i] % 4;
            switch (temp)
            {
                case 0:
                    checksum1 += message[i];
                    checksum2 <<= message[i];
                    break;
                case 1:
                    checksum1 -= message[i];
                    checksum2 -= message[i];
                    break;
                case 2:
                    checksum1 >>= message[i];
                    checksum2 >>= message[i];
                    break;
                case 3:
                    checksum1 <<= message[i];
                    checksum2 += message[i];
                    break;
            }
        }

        message.AddRange(BitConverter.GetBytes(checksum1));
        message.AddRange(BitConverter.GetBytes(checksum2));
    }

    public virtual void DecryptMessage(List<byte> message, out uint cs1, out uint cs2)
    {
        uint checksum1 = 0;
        uint checksum2 = 0;
        int messageLenght = message.Count - sizeof(uint) * 2;

        for (int i = 0; i < messageLenght; i++)
        {
            int temp = message[i] % 4;
            switch (temp)
            {
                case 0:
                    checksum1 += message[i];
                    checksum2 <<= message[i];
                    break;
                case 1:
                    checksum1 -= message[i];
                    checksum2 -= message[i];
                    break;
                case 2:
                    checksum1 >>= message[i];
                    checksum2 >>= message[i];
                    break;
                case 3:
                    checksum1 <<= message[i];
                    checksum2 += message[i];
                    break;
            }
        }

        cs1 = checksum1;
        cs2 = checksum2;
    }

    public virtual bool CheckMessage(byte[] message)
    {
        DecryptMessage(message.ToList(), out uint cs1, out uint cs2);
        if (cs1 == BitConverter.ToUInt32(message, message.Length - sizeof(uint) * 2) &&
            cs2 == BitConverter.ToUInt32(message, message.Length - sizeof(uint)))
        {
            return true;
        }
        return false;
    }
}

public abstract class BaseOrderableMessage<PayloadType> : BaseMessage<PayloadType>
{
    protected static ulong LastMsgID = 0;

    protected ulong MsgID = 0;
    protected static Dictionary<PayloadType, ulong> lastExecutedMsgID = new Dictionary<PayloadType, ulong>();

    protected BaseOrderableMessage(byte[] msg)
    {
        MsgID = BitConverter.ToUInt64(msg, 4);
    }
}

[Serializable]
public struct Player
{
    public string playerName;
    public int playerID;
    public int hp;
    public const int maxHP = 3;
    public Vector3 playerPos;
    public Vector3 playerRot;

    public Player(string playerName, int playerID)
    {
        this.playerName = playerName;
        this.playerID = playerID;
        playerPos = Vector3.zero;
        playerRot = Vector3.zero;
        hp = maxHP;
    }

    public void SetPosition(Vector3 newPos)
    {
        playerPos = newPos;
    }
}

public class NetServerToClientHS : BaseMessage<List<Player>>
{
    public NetServerToClientHS(byte[] dataToDeserialize)
    {
        data = Deserialize(dataToDeserialize);
    }

    public NetServerToClientHS(List<Player> players)
    {
        data = players;
    }

    public override MessageType GetMessageType()
    {
        return MessageType.ServerToClientHS;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(data.Count));

        foreach (var player in data)
        {
            outData.AddRange(BitConverter.GetBytes(player.playerID));
            outData.AddRange(BitConverter.GetBytes(player.hp));
            outData.AddRange(BitConverter.GetBytes(player.playerPos.x));
            outData.AddRange(BitConverter.GetBytes(player.playerPos.y));
            outData.AddRange(BitConverter.GetBytes(player.playerPos.z));
            outData.AddRange(BitConverter.GetBytes(player.playerRot.x));
            outData.AddRange(BitConverter.GetBytes(player.playerRot.y));
            outData.AddRange(BitConverter.GetBytes(player.playerRot.z));
            outData.AddRange(BitConverter.GetBytes(player.playerName.Length));
            outData.AddRange(Encoding.UTF8.GetBytes(player.playerName));
        }
        
        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    public override List<Player> Deserialize(byte[] message)
    {
        List<Player> aux = new List<Player>();

        int playersAmmount = BitConverter.ToInt32(message, 4);
        int offset = 0;
        for (int i = 0; i < playersAmmount; i++)
        {
            Player temp = new Player();
            temp.playerID = BitConverter.ToInt32(message, offset + 8);
            temp.hp = BitConverter.ToInt32(message, offset + 12);
            temp.playerPos.x = BitConverter.ToSingle(message, offset + 16);
            temp.playerPos.y = BitConverter.ToSingle(message, offset + 20);
            temp.playerPos.z = BitConverter.ToSingle(message, offset + 24);
            temp.playerRot.x = BitConverter.ToSingle(message, offset + 28);
            temp.playerRot.y = BitConverter.ToSingle(message, offset + 32);
            temp.playerRot.z = BitConverter.ToSingle(message, offset + 36);
            int stringlength = BitConverter.ToInt32(message, offset + 40);
            temp.playerName = Encoding.UTF8.GetString(message, offset + 44, stringlength);
            aux.Add(temp);
            offset += 36 + stringlength;
        }

        return aux;
    }

    public override List<Player> GetData()
    {
        return data;
    }
}

public class NetClientToServerHS : BaseMessage<Player>
{
    public NetClientToServerHS(byte[] dataToDeserialize)
    {
        data = Deserialize(dataToDeserialize);
    }

    public NetClientToServerHS(Player player)
    {
        data = player;
    }

    public override MessageType GetMessageType()
    {
        return MessageType.ClientToServerHS;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

        outData.AddRange(BitConverter.GetBytes(data.playerID));
        outData.AddRange(BitConverter.GetBytes(data.playerName.Length));
        outData.AddRange(Encoding.UTF8.GetBytes(data.playerName));
        
        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    public override Player Deserialize(byte[] message)
    {
        Player aux = new Player();
        aux.playerID = BitConverter.ToInt32(message, 4);
        int stringlength = BitConverter.ToInt32(message, 8);
        aux.playerName = Encoding.UTF8.GetString(message, 12, stringlength);
        return aux;
    }

    public override Player GetData()
    {
        return data;
    }
}

public class NetPing : BaseMessage<object>
{
    public NetPing()
    {
        data = GetMessageType();
    }

    public override MessageType GetMessageType()
    {
        return MessageType.Ping;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        
        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    public override object Deserialize(byte[] message)
    {
        object aux = BitConverter.ToInt32(message, 0);
        return aux;
    }

    public override object GetData()
    {
        return data;
    }
}

public class NetVector3 : BaseMessage<Vector3>
{
    private static ulong lastMsgID = 0;

    public NetVector3(Vector3 data)
    {
        this.data = data;
    }
    public NetVector3(byte[] dataToDeserialize)
    {
        data = Deserialize(dataToDeserialize);
    }

    public override Vector3 Deserialize(byte[] message)
    {
        Vector3 outData;

        outData.x = BitConverter.ToSingle(message, 4);
        outData.y = BitConverter.ToSingle(message, 8);
        outData.z = BitConverter.ToSingle(message, 12);

        return outData;
    }

    public override Vector3 GetData()
    {
        return data;
    }

    public override MessageType GetMessageType()
    {
        return MessageType.Position;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(data.x));
        outData.AddRange(BitConverter.GetBytes(data.y));
        outData.AddRange(BitConverter.GetBytes(data.z));
        
        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    //Dictionary<Client,Dictionary<msgType,int>>
}

public class NetPosition : BaseMessage<(int,int,Vector3)>
{
    public NetPosition(byte[] dataToDeserialize)
    {
        data = Deserialize(dataToDeserialize);
    }

    public NetPosition((int,int,Vector3) data)
    {
        this.data = data;
    }
    public override MessageType GetMessageType()
    {
        return MessageType.Position;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(data.Item1));
        outData.AddRange(BitConverter.GetBytes(data.Item2));
        outData.AddRange(BitConverter.GetBytes(data.Item3.x));
        outData.AddRange(BitConverter.GetBytes(data.Item3.y));
        outData.AddRange(BitConverter.GetBytes(data.Item3.z));
        
        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    public override (int,int,Vector3) Deserialize(byte[] message)
    {
        (int,int,Vector3) outData;

        outData.Item1 = BitConverter.ToInt32(message, 4);
        outData.Item2 = BitConverter.ToInt32(message, 8);
        outData.Item3.x = BitConverter.ToSingle(message, 12);
        outData.Item3.y = BitConverter.ToSingle(message, 16);
        outData.Item3.z = BitConverter.ToSingle(message, 20);

        return outData;
    }

    public override (int,int,Vector3) GetData() { return data; }
}

public class NetConsole : BaseMessage<String>
{
    private static ulong lastMsgID = 0;

    public NetConsole(byte[] dataToDeserialize)
    {
        data = Deserialize(dataToDeserialize);
    }

    public NetConsole(string data)
    {
        this.data = data;
    }

    public override MessageType GetMessageType()
    {
        return MessageType.Console;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(data.Length));
        outData.AddRange(Encoding.UTF8.GetBytes(data));

        base.EncryptMessage(outData);

        return outData.ToArray();
    }

    public override string Deserialize(byte[] message)
    {
        int stringlength = BitConverter.ToInt32(message, 4);
        return Encoding.UTF8.GetString(message, 8, stringlength);
    }

    public override string GetData()
    {
        return data;
    }
}
    public class NetPlayerList : BaseMessage<List<Player>>
    {
        public NetPlayerList(List<Player> updatedPlayerList)
        {
            data = updatedPlayerList;
        }
        public NetPlayerList(byte[] message)
        {
            data = Deserialize(message);
        }

        public override MessageType GetMessageType()
        {
            return MessageType.PlayerList;
        }

        public override byte[] Serialize()
        {
            List<byte> outData = new List<byte>();

            outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
            outData.AddRange(BitConverter.GetBytes(data.Count));

            foreach (var player in data)
            {
                outData.AddRange(BitConverter.GetBytes(player.playerID));
                outData.AddRange(BitConverter.GetBytes(player.hp));
                outData.AddRange(BitConverter.GetBytes(player.playerPos.x));
                outData.AddRange(BitConverter.GetBytes(player.playerPos.y));
                outData.AddRange(BitConverter.GetBytes(player.playerPos.z));
                outData.AddRange(BitConverter.GetBytes(player.playerRot.x));
                outData.AddRange(BitConverter.GetBytes(player.playerRot.y));
                outData.AddRange(BitConverter.GetBytes(player.playerRot.z));
                outData.AddRange(BitConverter.GetBytes(player.playerName.Length));
                outData.AddRange(Encoding.UTF8.GetBytes(player.playerName));
            }
        
            base.EncryptMessage(outData);

            return outData.ToArray();
        }

        public override List<Player> Deserialize(byte[] message)
        {
            List<Player> aux = new List<Player>();

            int playersAmmount = BitConverter.ToInt32(message, 4);
            int offset = 0;
            for (int i = 0; i < playersAmmount; i++)
            {
                Player temp = new Player();
                temp.playerID = BitConverter.ToInt32(message, offset + 8);
                temp.hp = BitConverter.ToInt32(message, offset + 12);
                temp.playerPos.x = BitConverter.ToSingle(message, offset + 16);
                temp.playerPos.y = BitConverter.ToSingle(message, offset + 20);
                temp.playerPos.z = BitConverter.ToSingle(message, offset + 24);
                temp.playerRot.x = BitConverter.ToSingle(message, offset + 28);
                temp.playerRot.y = BitConverter.ToSingle(message, offset + 32);
                temp.playerRot.z = BitConverter.ToSingle(message, offset + 36);
                int stringlength = BitConverter.ToInt32(message, offset + 40);
                temp.playerName = Encoding.UTF8.GetString(message, offset + 44, stringlength);
                aux.Add(temp);
                offset += 36 + stringlength;
            }

            return aux;
        }

        public override List<Player> GetData() { return data; }
    }