using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Emit;


namespace GUIDEMO
{

    public struct SFrameMaskData
    {
        public int DataLength, KeyIndex, TotalLenght;
        public EOpcodeType Opcode;

        public SFrameMaskData(int DataLength, int KeyIndex, int TotalLenght, EOpcodeType Opcode)
        {
            this.DataLength = DataLength;
            this.KeyIndex = KeyIndex;
            this.TotalLenght = TotalLenght;
            this.Opcode = Opcode;
        }
    }

    /// <summary>
    /// Enum for opcode types
    /// </summary>
    public enum EOpcodeType
    {
        /* Denotes a continuation code */
        Fragment = 0,

        /* Denotes a text code */
        Text = 1,

        /* Denotes a binary code */
        Binary = 2,

        /* Denotes a closed connection */
        ClosedConnection = 8,

        /* Denotes a ping*/
        Ping = 9,

        /* Denotes a pong */
        Pong = 10
    }

    /// <summary>
    /// Helper methods for the Server and Client class
    /// </summary>
    public static class Helpers
    {
        /// <summary>Gets data for a encoded websocket frame message</summary>
        /// <param name="Data">The data to get the info from</param>
        /// <returns>The frame data</returns>
        public static SFrameMaskData GetFrameData(byte[] Data)
        {
            // Get the opcode of the frame
            int opcode = Data[0] - 128;

            // If the length of the message is in the 2 first indexes
            if (Data[1] - 128 <= 125)
            {
                int dataLength = (Data[1] - 128);
                return new SFrameMaskData(dataLength, 2, dataLength + 6, (EOpcodeType)opcode);
            }

            // If the length of the message is in the following two indexes
            if (Data[1] - 128 == 126)
            {
                // Combine the bytes to get the length
                int dataLength = BitConverter.ToInt16(new byte[] { Data[3], Data[2] }, 0);
                return new SFrameMaskData(dataLength, 4, dataLength + 8, (EOpcodeType)opcode);
            }

            // If the data length is in the following 8 indexes
            if (Data[1] - 128 == 127)
            {
                // Get the following 8 bytes to combine to get the data 
                byte[] combine = new byte[8];
                for (int i = 0; i < 8; i++) combine[i] = Data[i + 2];

                // Combine the bytes to get the length
                //int dataLength = (int)BitConverter.ToInt64(new byte[] { Data[9], Data[8], Data[7], Data[6], Data[5], Data[4], Data[3], Data[2] }, 0);
                int dataLength = (int)BitConverter.ToInt64(combine, 0);
                return new SFrameMaskData(dataLength, 10, dataLength + 14, (EOpcodeType)opcode);
            }

            // error
            return new SFrameMaskData(0, 0, 0, 0);
        }

        /// <summary>Gets the opcode of a frame</summary>
        /// <param name="Frame">The frame to get the opcode from</param>
        /// <returns>The opcode of the frame</returns>
        public static EOpcodeType GetFrameOpcode(byte[] Frame)
        { return (EOpcodeType)Frame[0] - 128; }

        /// <summary>Gets the decoded frame data from the given byte array</summary>
        /// <param name="Data">The byte array to decode</param>
        /// <returns>The decoded data</returns>
        public static string GetDataFromFrame(byte[] Data)
        {
            // Get the frame data
            SFrameMaskData frameData = GetFrameData(Data);

            // Get the decode frame key from the frame data
            byte[] decodeKey = new byte[4];
            for (int i = 0; i < 4; i++) decodeKey[i] = Data[frameData.KeyIndex + i];

            int dataIndex = frameData.KeyIndex + 4;
            int count = 0;

            // Decode the data using the key
            for (int i = dataIndex; i < frameData.TotalLenght; i++)
            {
                Data[i] = (byte)(Data[i] ^ decodeKey[count % 4]);
                count++;
            }

            // Return the decoded message 
            return Encoding.Default.GetString(Data, dataIndex, frameData.DataLength);
        }

        /// <summary>Checks if a byte array is valid</summary>
        /// <param name="Buffer">The byte array to check</param>
        /// <returns>'true' if the byte array is valid</returns>
        public static bool GetIsBufferValid(ref byte[] Buffer)
        {
            if (Buffer == null) return false;
            if (Buffer.Length <= 0) return false;
            return true;
        }

        /// <summary>Gets an encoded websocket frame to send to a client from a string</summary>
        /// <param name="Message">The message to encode into the frame</param>
        /// <param name="Opcode">The opcode of the frame</param>
        /// <returns>Byte array in form of a websocket frame</returns>
        public static byte[] GetFrameFromString(string Message, EOpcodeType Opcode = EOpcodeType.Text)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.Default.GetBytes(Message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)(128 + (int)Opcode);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }

        /// <summary>Hash a request key with SHA1 to get the response key</summary>
        /// <param name="Key">The request key</param>
        /// <returns></returns>
        public static string HashKey(string Key)
        {
            const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string longKey = Key + handshakeKey;

            SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(longKey));

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>Gets the http request string to send to the websocket client</summary>
        /// <param name="Key">The SHA1 hashed key to respond with</param>
        /// <returns></returns>
        public static string GetHandshakeResponse(string Key)
        { return string.Format("HTTP/1.1 101 Switching Protocols\nUpgrade: WebSocket\nConnection: Upgrade\nSec-WebSocket-Accept: {0}\r\n\r\n", Key); }

        /// <summary>Gets the WebSocket handshake updgrade key from the http request</summary>
        /// <param name="HttpRequest">The http request string to get the key from</param>
        /// <returns></returns>
        public static string GetHandshakeRequestKey(string HttpRequest)
        {
            int keyStart = HttpRequest.IndexOf("Sec-WebSocket-Key: ") + 19;
            string key = null;

            for (int i = keyStart; i < (keyStart + 24); i++)
            {
                key += HttpRequest[i];
            }
            return key;
        }

        /// <summary>Creates a random guid with a prefix</summary>
        /// <param name="Prefix">The prefix of the id; null = no prefix</param>
        /// <param name="Length">The length of the id to generate</param>
        /// <returns>The random guid. Ex. Prefix-XXXXXXXXXXXXXXXX</returns>
        public static string CreateGuid(string Prefix, int Length = 16)
        {
            string final = null;
            string ids = "0123456789abcdefghijklmnopqrstuvwxyz";

            Random random = new Random();

            // Loop and get a random index in the ids and append to id 
            for (short i = 0; i < Length; i++) final += ids[random.Next(0, ids.Length)];

            // Return the guid without a prefix
            if (Prefix == null) return final;

            // Return the guid with a prefix
            return string.Format("{0}-{1}", Prefix, final);
        }
    }


    public partial class Client
    {

        ///<summary>The socket of the connected client</summary>
        private Socket _socket;
        ///<summary>The guid of the connected client</summary>
        private string _guid;
        /// <summary>The server that the client is connected to</summary>
        private Server _server;
        /// <summary>If the server has sent a ping to the client and is waiting for a pong</summary>
        private bool _bIsWaitingForPong;

        /// <summary>Create a new object for a connected client</summary>
        /// <param name="Server">The server object instance that the client is connected to</param>
        /// <param name="Socket">The socket of the connected client</param>
        public Client(Server Server, Socket Socket)
        {
            this._server = Server;
            this._socket = Socket;
            this._guid = Helpers.CreateGuid("client");
            // Start to detect incomming messages 
            GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);
        }

        /// <summary>Gets the guid of the connected client</summary>
        /// <returns>The GUID of the client</returns>
        public string GetGuid(){ return _guid; }

        ///<summary>Gets the socket of the connected client</summary>
        ///<returns>The socket of the client</return>
        public Socket GetSocket(){ return _socket; }

        /// <summary>The socket that this client is connected to</summary>
        /// <returns>Listen socket</returns>
        public Server GetServer(){ return _server; }

        /// <summary>Gets if the server is waiting for a pong response</summary>
        /// <returns>If the server is waiting for a pong response</returns>
        public bool GetIsWaitingForPong(){ return _bIsWaitingForPong; }

        /// <summary>Sets if the server is waiting for a pong response</summary>
        /// <param name="bIsWaitingForPong">If the server is waiting for a pong response</param>
        public void SetIsWaitingForPong(bool bIsWaitingForPong){ _bIsWaitingForPong = bIsWaitingForPong; }

        /// <summary>Called when a message was received from the client</summary>
        private void messageCallback(IAsyncResult AsyncResult)
        {
            try
            {
                GetSocket().EndReceive(AsyncResult);

                // Read the incomming message 
                byte[] messageBuffer = new byte[8];
                int bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) Array.Resize<byte>(ref messageBuffer, bytesReceived);

                // Get the opcode of the frame
                EOpcodeType opcode = Helpers.GetFrameOpcode(messageBuffer);

                // If the connection was closed
                if (opcode == EOpcodeType.ClosedConnection)
                {
                    GetServer().ClientDisconnect(this);
                    return;
                }

                // Pass the message to the server event to handle the logic
                GetServer().ReceiveMessage(this, Helpers.GetDataFromFrame(messageBuffer));

                // Start to receive messages again
                GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);

            }
            catch (Exception Exception)
            {
                GetSocket().Close();
                GetSocket().Dispose();
                GetServer().ClientDisconnect(this);
            }
        }

      
    }

    public class SocketServer
    {
        private static TcpListener serverSocket;
        public static void StartServer()
        {
            /*            IPHostEntry ipHostEntry = Dns.GetHostEntry("localhost");
            Console.WriteLine(ipHostEntry.ToString());
            IPAddress ipAddress = ipHostEntry.AddressList[0];*/
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 3000);
            serverSocket = new TcpListener(ipEndPoint);
            serverSocket.Start();
            Console.WriteLine("Asynchonous server socket is listening at: " + ipEndPoint.Address.ToString());
            WaitForClients();
        }

        private static void WaitForClients()
        {
            serverSocket.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnected), null);
        }

        private static void OnClientConnected(IAsyncResult asyncResult)
        {
            try
            {
                TcpClient clientSocket = serverSocket.EndAcceptTcpClient(asyncResult);
                if (clientSocket != null)
                    Console.WriteLine("Received connection request from: " + clientSocket.Client.RemoteEndPoint.ToString());
                HandleClientRequest(clientSocket);
            }
            catch
            {
                throw;
            }
            WaitForClients();
        }

        private static void HandleClientRequest(TcpClient clientSocket)
        {
            Console.WriteLine("HANDLING CLIENT REQUEST");
        }

    }


    public class IDGSocketClient
    {
        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        NetworkStream networkStream;
        public void Connect(string ipAddress, int port)
        {
            clientSocket.Connect(ipAddress, port);
        }
        public void Send(string data)
        {
            //Write code here to send data
        }
        public void Close()
        {
            clientSocket.Close();
        }
        public string Receive()
        {
            Console.WriteLine("RECEIVED!");
            return "RECEIVED!";
        }
    }

    public class OnMessageReceivedHandler : EventArgs
    {
        /// <summary>The client that send the message</summary>
        private Client _client;

        /// <summary>The message the client sent</summary>
        private string _message;

        /// <summary>Create a new message received event handler</summary>
        /// <param name="Client">The client that sent the message</param>
        /// <param name="Message">The message the client sent</param>
        public OnMessageReceivedHandler(Client Client, string Message)
        {
            this._client = Client;
            this._message = Message;
        }

        /// <summary>Get the client that sent the received message</summary>
        /// <returns>The client that sent the message</returns>
        public Client GetClient()
        {
            return _client;
        }

        /// <summary>The message that was received from the client</summary>
        /// <returns>The received message</returns>
        public string GetMessage()
        {
            return _message;
        }

    }

    /// <summary>
    /// Handler for when a message was send to a client
    /// </summary>
    public class OnSendMessageHandler : EventArgs
    {
        /// <summary>The client the message was sent to</summary>
        private Client _client;

        /// <summary>The message that was sent to the client</summary>
        private string _message;

        /// <summary>Create a new handler for when a message was sent</summary>
        /// <param name="Client">The client the message was sent to</param>
        /// <param name="Message">The message that was sent to the client</param>
        public OnSendMessageHandler(Client Client, string Message)
        {
            this._client = Client;
            this._message = Message;
        }

        /// <summary>The client the message was sent to</summary>
        /// <returns>The client receiver</returns>
        public Client GetClient()
        {
            return _client;
        }

        /// <summary>The message that was send to the client</summary>
        /// <returns>The sent message</returns>
        public string GetMessage()
        {
            return _message;
        }
    }

    /// <summary>
    /// Handler for when a client connected
    /// </summary>
    public class OnClientConnectedHandler : EventArgs
    {
        /// <summary>The client that connected to the server</summary>
        private Client _client;

        /// <summary>Create a new event handler for when a client connected</summary>
        /// <param name="Client">The client that connected</param>
        public OnClientConnectedHandler(Client Client)
        {
            this._client = Client;
        }

        /// <summary>Get the client that was connected</summary>
        /// <returns>The client that connected </returns>
        public Client GetClient()
        {
            return _client;
        }
    }

    /// <summary>
    /// Handler for when a client disconnects
    /// </summary>
    public class OnClientDisconnectedHandler : EventArgs
    {
        /// <summary>The client that diconnected</summary>
        private Client _client;

        /// <summary>Create a new handler for when a client disconnects</summary>
        /// <param name="Client">The disconnected client</param>
        public OnClientDisconnectedHandler(Client Client)
        {
            this._client = Client;
        }

        /// <summary>Gets the client that disconnected</summary>
        /// <returns>The disconnected client</returns>
        public Client GetClient()
        {
            return _client;
        }
    }

    ///<summary>
    /// Object for all listen servers
    ///</summary>
    public partial class Server
    {

        /// <summary>The listen socket (server socket)</summary>
        private Socket _socket;
        /// <summary>The listen ip end point of the server</summary>
        private IPEndPoint _endPoint;
        /// <summary>The connected clients to the server </summary>
        private List<Client> _clients = new List<Client>();

        /// <summary>Create and start a new listen socket server</summary>
        /// <param name="EndPoint">The listen endpoint of the server</param>
        public Server(IPEndPoint EndPoint)
        {
            // Set the endpoint if the input is valid
            if (EndPoint == null) return;
            this._endPoint = EndPoint;

            // Create a new listen socket
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Copyright © 2017 - MazyModz. Created by Dennis Andersson. All rights reserved.\n\n");
            Console.WriteLine("WebSocket Server Started\nListening on {0}:{1}\n", GetEndPoint().Address.ToString(), GetEndPoint().Port);

            // Start the server
            start();
        }

        /// <summary>Gets the listen socket</summary>
        /// <returns>The listen socket</returns>
        public Socket GetSocket(){ return _socket; }

        /// <summary>Get the listen socket endpoint</summary>
        /// <returns>The listen socket endpoint</returns>
        public IPEndPoint GetEndPoint(){ return _endPoint; }

        /// <summary>Gets a connected client at the given index</summary>
        /// <param name="Index">The connected client array index</param>
        /// <returns>The connected client at the index, returns null if the index is out of bounds</returns>
        public Client GetConnectedClient(int Index)
        {
            if (Index < 0 || Index >= _clients.Count) return null;
            return _clients[Index];
        }

        /// <summary>Gets a connected client with the given guid</summary>
        /// <param name="Guid">The Guid of the client to get</param>
        /// <returns>The client with the given id, return null if no client with the guid could be found</returns>
        public Client GetConnectedClient(string Guid)
        {
            foreach (Client client in _clients){ if (client.GetGuid() == Guid) return client; }
            return null;
        }

        /// <summary>Gets a connected client with the given socket</summary>
        /// <param name="Socket">The socket of the client </param>
        /// <returns>The connected client with the given socket, returns null if no client with the socket was found</returns>
        public Client GetConnectedClient(Socket Socket)
        {
            foreach (Client client in _clients){ if (client.GetSocket() == Socket) return client; }
            return null;
        }

        /// <summary>Get the number of clients that are connected to the server</summary>
        /// <returns>The number of connected clients</returns>
        public int GetConnectedClientCount(){ return _clients.Count; }

        /// <summary>
        /// Starts the listen server when a server object is created
        /// </summary>
        private void start()
        {
            // Bind the socket and start listending
            GetSocket().Bind(GetEndPoint());
            GetSocket().Listen(0);

            // Start to accept clients and accept incomming connections 
            GetSocket().BeginAccept(connectionCallback, null);
        }

        /// <summary>
        /// Stops the listen server 
        /// </summary>
        public void Stop()
        {
            GetSocket().Close();
            GetSocket().Dispose();
        }

        /// <summary>Called when the socket is trying to accept an incomming connection</summary>
        /// <param name="AsyncResult">The async operation state</param>
        private void connectionCallback(IAsyncResult AsyncResult)
        {
            try
            {
                // Gets the client thats trying to connect to the server
                Socket clientSocket = GetSocket().EndAccept(AsyncResult);

                // Read the handshake updgrade request
                byte[] handshakeBuffer = new byte[1024];
                int handshakeReceived = clientSocket.Receive(handshakeBuffer);

                // Get the hanshake request key and get the hanshake response
                string requestKey = Helpers.GetHandshakeRequestKey(Encoding.Default.GetString(handshakeBuffer));
                string hanshakeResponse = Helpers.GetHandshakeResponse(Helpers.HashKey(requestKey));

                // Send the handshake updgrade response to the connecting client 
                clientSocket.Send(Encoding.Default.GetBytes(hanshakeResponse));

                // Create a new client object and add 
                // it to the list of connected clients
                Client client = new Client(this, clientSocket);
                _clients.Add(client);

                // Call the event when a client has connected to the listen server 
                if (OnClientConnected == null) throw new Exception("Server error: event OnClientConnected is not bound!");
                OnClientConnected(this, new OnClientConnectedHandler(client));

                // Start to accept incomming connections again 
                GetSocket().BeginAccept(connectionCallback, null);

            }
            catch (Exception Exception)
            {
                Console.WriteLine("An error has occured while trying to accept a connecting client.\n\n{0}", Exception.Message);
            }
        }

        /// <summary>Called when a message was recived, calls the OnMessageReceived event</summary>
        /// <param name="Client">The client that sent the message</param>
        /// <param name="Message">The message that the client sent</param>
        public void ReceiveMessage(Client Client, string Message)
        {
            if (OnMessageReceived == null) throw new Exception("Server error: event OnMessageReceived is not bound!");
            OnMessageReceived(this, new OnMessageReceivedHandler(Client, Message));
        }

        /// <summary>Called when a client disconnectes, calls event OnClientDisconnected</summary>
        /// <param name="Client">The client that disconnected</param>
        public void ClientDisconnect(Client Client)
        {
            // Remove the client from the connected clients list
            _clients.Remove(Client);

            // Call the OnClientDisconnected event
            if (OnClientDisconnected == null) throw new Exception("Server error: OnClientDisconnected is not bound!");
            OnClientDisconnected(this, new OnClientDisconnectedHandler(Client));
        }

        /// <summary>Send a message to a connected client</summary>
        /// <param name="Client">The client to send the data to</param>
        /// <param name="Data">The data to send the client</param>
        public void SendMessage(Client Client, string Data)
        {
            // Create a websocket frame around the data to send
            byte[] frameMessage = Helpers.GetFrameFromString(Data);

            // Send the framed message to the in client
            Client.GetSocket().Send(frameMessage);

            // Call the on send message callback event 
            if (OnSendMessage == null) throw new Exception("Server error: event OnSendMessage is not bound!");
            OnSendMessage(this, new OnSendMessageHandler(Client, Data));
        }

        /// <summary>Called after a message was sent</summary>
        public event EventHandler<OnSendMessageHandler> OnSendMessage;
        /// <summary>Called when a client was connected to the server (after handshake)</summary>
        public event EventHandler<OnClientConnectedHandler> OnClientConnected;
        /// <summary>Called when a message was received from a connected client</summary>
        public event EventHandler<OnMessageReceivedHandler> OnMessageReceived;
        /// <summary>Called when a client disconnected</summary>
        public event EventHandler<OnClientDisconnectedHandler> OnClientDisconnected;


        public static void StartServer()
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 3000);

            try
            {

                // Create a Socket that will use Tcp protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                Console.WriteLine("Waiting for a connection...");
                Socket handler = listener.Accept();

                // Incoming data from the client.
                string data = null;
                byte[] bytes = null;

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                Console.WriteLine("Text received : {0}", data);

                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }
    }
    

    


public class HexadecimalEncoding
    {
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

    }



    public class Transaction
    {
        public uint TimeStamp { get; set; }
        public string InitiatorAddress { get; set; }
        public string RecipientAddress { get; set; }
        public double Amount { get; set; }
        public enum transactionTypes
        {
            None = 0b_0000_0000,  // 0
            CREATE = 0b_0000_0001,  // 1
            MODIFY = 0b_0000_0010,  // 2
            VOTE = 0b_0000_0100,  // 4
            CANCEL = 0b_0000_1000,  // 8
            Friday = 0b_0001_0000,  // 16
            Saturday = 0b_0010_0000,  // 32
            Sunday = 0b_0100_0000,  // 64
            Weekend = Saturday | None
        }
        public enum transactionSubTypes
        {
            None = 0b_0000_0000,  // 0
            USER = 0b_0000_0001,  // 1
            RULE = 0b_0000_0010,  // 2
            DISPUTE = 0b_0000_0100,  // 4
            ASSET = 0b_0000_1000,  // 8
            ENTITY = 0b_0001_0000,  // 16
            PERMISSION = 0b_0010_0000,  // 32
            Sunday = 0b_0100_0000,  // 64
            Weekend = None | Sunday
        }

        //TRANSACTION RETURNS STRING
        public Transaction(string fromAddress, string toAddress, double amount)
        {
            InitiatorAddress = fromAddress;
            RecipientAddress = toAddress;
            Amount = amount;
            Console.WriteLine(DateTime.UtcNow);
            //DateTimeOffset.FromUnixTimeSeconds(time);
            TimeStamp = ToDosDateTime(DateTime.UtcNow);
            Console.WriteLine(TimeStamp.ToString());
           
        }

        public UInt32 ToDosDateTime(DateTime dateTime)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan currTime = dateTime - startTime;
            UInt32 time_t = Convert.ToUInt32(Math.Abs(currTime.TotalSeconds));
            return time_t;
        }

    }

    public class Block
    {



        /*        public class BlockHeader
        {
            ushort the_miningnodeversion { get; set; }
            string this_block_proof { get; set; }
            uint prev_block_height { get; set; }
            byte[] prev_block_hash { get; set; }
            byte[] this_block_hash { get; set; }
            uint this_block_timestamp { get; set; }

        }*/

        public uint timeStamp;
        public uint TimeStamp
        {
            get { return this.timeStamp; }
            set { this.timeStamp = value; }
        }
        public ushort Version { get; set; }
        public uint BlockHeight { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        public string MerkleRoot { get; set; }
        public IList<Transaction> Transactions { get; set; }
        public string Validator { get; set; }
        public uint NumOfTx { get; set; }
        public double TotalAmount { get; set; }
        public float TotalReward { get; set; }
        public uint Difficulty { get; set; }
        public int Nonce = 0;



        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return HexadecimalEncoding.ByteArrayToString(bytes);
        }

        static string DoubleHash(string leaf1, string leaf2)
        {
            byte[] leaf1Byte = HexadecimalEncoding.StringToByteArray(leaf1);
            byte[] leaf2Byte = HexadecimalEncoding.StringToByteArray(leaf2);

            var concatHash = leaf1Byte.Concat(leaf2Byte).ToArray();
            SHA256 sha256 = SHA256.Create();
            byte[] sendHash = sha256.ComputeHash(sha256.ComputeHash(concatHash));

            return HexadecimalEncoding.ByteArrayToString(sendHash).ToLower();
        }


        public static string CreateMerkleRoot(string[] txsHash)
        {

            while (true)
            {
                if (txsHash.Length == 0)
                {
                    return string.Empty;
                }

                if (txsHash.Length == 1)
                {
                    return txsHash[0];
                }

                List<string> newHashList = new List<string>();

                int len = (txsHash.Length % 2 != 0) ? txsHash.Length - 1 : txsHash.Length;

                for (int i = 0; i < len; i += 2)
                {
                    newHashList.Add(DoubleHash(txsHash[i], txsHash[i + 1]));
                }

                if (len < txsHash.Length)
                {
                    newHashList.Add(DoubleHash(txsHash[1], txsHash[1]));
                }

                txsHash = newHashList.ToArray();
            }
        }

        /*        public static string CreateMerkleRoot(IList<string> trxs)
                {   // if transaction only 1
                    if (trxs.Count == 1)
                    {
                        var firstTrx = trxs.First();
                        return firstTrx;
                    }   // if the number of transaction is odd
                    if (trxs.Count % 2 == 1)
                    {
                        var lastTrx = trxs.Last();
                        trxs.Add(lastTrx);
                    }   // looping to create branches
                    var branches = new List<string>();
                    for (int i = 0; i < trxs.Count; i += 2)
                    {
                        // concat each pair
                        var pair = string.Concat(trxs[i], trxs[i + 1]);
                        Console.Write(trxs[i] + "+" + trxs[i + 1]); Console.Write("   ");
                        branches.Add(pair);
                    }

                    Console.WriteLine();
                    var result = CreateMerkleRoot(branches);
                    return result;
                }*/


        public static string createGenesisBlock()
        {
            Transaction trx1 = new Transaction("12341241", "2141412412", 0.24);
           // MiningNode.PendingTransactions.Add(trx1);
            Transaction trx2 = new Transaction("12341241", "666453343", 0.44);
            var jsonString = JsonConvert.SerializeObject(trx1);
            Console.WriteLine(jsonString);
            return "test";
        }

        public string SendOutCandidateBlock()
        {
            return "trin";
        }

        public string SaveBlockFile(string filename)
        {

            return "true";
        }

    }



    public class MiningNode
    {
        public static string fileName = ".\\blockchaindata\\0.dat";
        public IList<Transaction> PendingTransactions = new List<Transaction>();
        public IList<Block> Chain = new List<Block>();
        public IDictionary<string, string> LegalDefinitions = new Dictionary<string, string>();




        public void SaveBinaryFile()
        {
            // Create a hashtable of values that will eventually be serialized.
            Hashtable addresses = new Hashtable();
            string pathString = "blockchaindata";
            addresses.Add("Jeff", "123 Main Street, Redmond, WA 98052");
            addresses.Add("Fred", "987 Pine Road, Phila., PA 19116");
            addresses.Add("Mary", "PO Box 112233, Palo Alto, CA 94301");
            //Create the stream to add object into it. 
            
            try
            {
                
                pathString = System.IO.Path.Combine(".\\blockchaindata", "0.bin");
                //Format the object as Binary  
                System.IO.Stream ms = File.OpenWrite(pathString);
                BinaryFormatter formatter = new BinaryFormatter();
                //It serialize the employee object  
                formatter.Serialize(ms, addresses);
                ms.Flush();
                ms.Close();
                ms.Dispose();
            }
            catch
            {
                System.IO.Directory.CreateDirectory("blockchaindata");
                pathString = System.IO.Path.Combine(".\\blockchaindata", "0.bin");
                //Format the object as Binary  
                System.IO.Stream ms = File.OpenWrite(pathString);
                BinaryFormatter formatter = new BinaryFormatter();
                //It serialize the employee object  
                formatter.Serialize(ms, addresses);
                ms.Flush();
                ms.Close();
                ms.Dispose();
            }
            finally
            {

            }

        }

        public void LoadBinaryFile()
        {
            // Declare the hashtable reference.
            Hashtable addresses = null;

            // Open the file containing the data that you want to deserialize.
            FileStream fs = new FileStream(".\\blockchaindata\\0.bin", FileMode.Open);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                // Deserialize the hashtable from the file and
                // assign the reference to the local variable.
                addresses = (Hashtable)formatter.Deserialize(fs);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

            // To prove that the table deserialized correctly,
            // display the key/value pairs.
            foreach (DictionaryEntry de in addresses)
            {
                Console.WriteLine("{0} lives at {1}.", de.Key, de.Value);
            }
        }


    }


   


    public class Blockchain
    {

        public int Difficulty = 2;
        public int Reward = 1; //1 cryptocurrency
        string SourceData;
        byte[] tmpSource;
        byte[] tmpHash;
        public ushort miningnodeversion;
        public uint blockheight;

    }




    internal static class Program
    {



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Form1 theForm = new Form1();
            Console.WriteLine("PROGRAM MAIN");
            //Block thisBlock = new Block();
            MiningNode currentMiningNode = new MiningNode();
            //public string path = currentMiningNode.fileName; 
            Console.WriteLine("CHECK NETWORK FOR ACTIVE NODES");
            //IF NODES FOUND
            if(1 > 2)
            {
                Console.WriteLine("NODES FOUND");
                Console.WriteLine("GET LATEST BLOCKHEIGHT");
                uint currentBlockheight = 0;
                for (int i = 0; i < currentBlockheight; i++)
                {
                    if (File.Exists(".\\blockchaindata\\" + i.ToString() + ".dat"))
                    {
                        Console.WriteLine("BLOCK FILE " + i.ToString() + " EXISTS");
                        using (BinaryReader b = new BinaryReader(File.Open(".\\blockchaindata\\0.dat", FileMode.Open)));
                    }
                    else
                    {
                        Console.WriteLine("BLOCK FILE " + i.ToString() + " DOESNT EXIST");
                        Console.WriteLine("DOWNLOADING FILE");
                        // generate hash of str
                        var str = "TESTING";
                        var hash = Block.GenHash(str); Console.WriteLine("Hash: {0}", hash);
                        Console.WriteLine("Prev Length: {0}", str.Length);
                        Console.WriteLine("Length: {0}", hash.Length); str = "A hash function is any function that can be used to map data of arbitrary size to a fixed size value. The value that the hash function returns is called the hash value, hash code, digest, or simply hash.";// generate hash of str
                        hash = Block.GenHash(str); Console.WriteLine("Hash: {0}", hash);
                        Console.WriteLine("Prev Length: {0}", str.Length);
                        Console.WriteLine("Length: {0}", hash.Length);

                    }
                }
            }
            else
            {
                theForm.SetLabel3Text = "NO NODES FOUND";
                //Form1().label3.Text = "NO NODES FOUND";
                Console.WriteLine("NO NODES FOUND");
                //if blockchain doesn't exist yet, create new one
                theForm.SetLabel3Text = "CREATING NODE";
                Console.WriteLine("CREATING NODE");

                //Server.StartServer();
                SocketServer.StartServer();
                // Create a listen server on localhost with port 80
                /*                Server server = new Server(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 80));
                //bind event functions
                server.OnClientConnected += (object sender, OnClientConnectedHandler e) => { Console.WriteLine("Client with GUID: {0} Connected!", e.GetClient().GetGuid()); };
                server.OnClientDisconnected += (object sender, OnClientDisconnectedHandler e) => { Console.WriteLine("Client {0} Disconnected", e.GetClient().GetGuid()); };
                server.OnMessageReceived += (object sender, OnMessageReceivedHandler e) => { Console.WriteLine("Received Message: '{1}' from client: {0}", e.GetClient().GetGuid(), e.GetMessage()); };
                server.OnSendMessage += (object sender, OnSendMessageHandler e) => { Console.WriteLine("Sent message: '{0}' to client {1}", e.GetMessage(), e.GetClient().GetGuid()); };*/

                Console.WriteLine("CREATING GENESIS BLOCK");
                Block.createGenesisBlock();
                Console.WriteLine("SAVING GENESIS BINARY FILE");
                currentMiningNode.SaveBinaryFile();
            }


            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            
            Application.Run(theForm);

        }
    }
}
