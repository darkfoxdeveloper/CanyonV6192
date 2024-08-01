using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Canyon.Network.Sockets
{
    /// <summary>
    ///     TcpServerListener implements an asynchronous TCP streaming socket server for high
    ///     performance server logic. Socket operations are processed in value tasks using
    ///     Socket Task Extensions. Inherits from a base class for providing socket operation
    ///     event handling to the non-abstract derived class of TcpServerListener.
    /// </summary>
    /// <typeparam name="TActor">Type of actor passed by the parent project</typeparam>
    public abstract class TcpServerListener<TActor> : TcpServerEvents<TActor>
        where TActor : TcpServerActor
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<TcpServerListener<TActor>>();

        // Fields and properties
        private readonly Semaphore acceptanceSemaphore;
        private readonly ConcurrentStack<Memory<byte>> bufferPool;
        private readonly bool enableKeyExchange;
        private readonly int footerLength;
        private readonly TaskFactory receiveTasks;
        private readonly CancellationTokenSource shutdownToken;
        private readonly Socket socket;
        private readonly TcpServerRegistry registry;

        protected int ExchangeStartPosition = 7;

        /// <summary>
        ///     Instantiates a new instance of <see cref="TcpServerListener{TActor}" /> with a new server
        ///     socket for accepting remote or local client connections. Creates pre-allocated
        ///     buffers for receiving data from clients without expensive allocations per receive
        ///     operation.
        /// </summary>
        /// <param name="maxConn">Maximum number of clients connected</param>
        /// <param name="bufferSize">pre-allocated buffer size in bytes</param>
        /// <param name="delay">Use Nagel's algorithm to delay sending smaller packets</param>
        /// <param name="exchange">Use a key exchange before receiving packets</param>
        /// <param name="footerLength">Length of the packet footer</param>
        protected TcpServerListener(
            int maxConn = 1000,
            int bufferSize = 4096,
            bool delay = false,
            bool exchange = false,
            int footerLength = 0)
        {
            // Initialize and configure server socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = new LingerOption(false, 0),
                NoDelay = !delay
            };
            shutdownToken = new CancellationTokenSource();

            // Initialize management mechanisms
            acceptanceSemaphore = new Semaphore(maxConn, maxConn);
            bufferPool = new ConcurrentStack<Memory<byte>>();
            enableKeyExchange = exchange;
            this.footerLength = footerLength;
            receiveTasks = new TaskFactory(shutdownToken.Token);
            registry = new TcpServerRegistry();

            // Initialize pre-allocated buffer pool
            for (var i = 0; i < maxConn; i++)
            {
                bufferPool.Push(new Memory<byte>(new byte[bufferSize]));
            }
        }

        /// <summary>
        ///     Binds the server listener to a port and network interface. Specify "0.0.0.0"
        ///     as the endpoint address to bind to all interfaces on the host machine. Starts
        ///     the server listener and accepts new connections in a new task.
        /// </summary>
        /// <param name="port">Port number the server will bind to</param>
        /// <param name="address">Interface IPv4 address the server will bind to</param>
        /// <param name="backlog">Maximum connections backlogged for acceptance</param>
        /// <returns>Returns a new task for accepting new connections.</returns>
        public async Task StartAsync(int port, string address = "0.0.0.0", int backlog = 100)
        {
            socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            socket.Listen(backlog);

            logger.LogInformation("Listening on {}:{}", address, port);

            // Start the background registry cleaner and accepting clients
            await registry.StartAsync(shutdownToken.Token);
            await AcceptingAsync();
        }

        /// <summary>
        ///     Accepting accepts client connections asynchronously as a new task. As a client
        ///     connection is accepted, it will be associated with a pre-allocated buffer and
        ///     a receive task. The accepted socket event will be called after accept.
        /// </summary>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private async Task AcceptingAsync()
        {
            while (socket.IsBound && !shutdownToken.IsCancellationRequested)
            {
                // Block if the maximum connections has been reached. Holds all connection
                // attempts in the backlog of the server socket until a client disconnects
                // and a new client can be accepted. Check shutdown every 5 seconds.
                if (acceptanceSemaphore.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    // Pop a pre-allocated buffer and check the connection
                    Socket socket = await this.socket.AcceptAsync();
                    var ip = (socket.RemoteEndPoint as IPEndPoint).Address.MapToIPv4().ToString();
                    var blockingReason = registry.AddActiveClient(ip);
                    if (blockingReason != TcpServerRegistry.BlockReason.None)
                    {
                        await socket.DisconnectAsync(false);
                        logger.LogWarning("Login denied due to [{Reason}] login attempts [IP: {Ip}]", blockingReason, ip);
                        acceptanceSemaphore.Release();
                        continue;
                    }

                    // Construct the client before receiving data
                    bufferPool.TryPop(out Memory<byte> buffer);
                    TActor actor = await AcceptedAsync(socket, buffer);

                    // Start receiving data from the client connection
                    if (enableKeyExchange)
                    {
                        actor.ConnectionStage = TcpServerActor.Stage.Exchange;
                        ConfiguredTaskAwaitable<Task> task = receiveTasks
                                                             .StartNew(ExchangingAsync, actor, shutdownToken.Token)
                                                             .ConfigureAwait(false);
                    }
                    else
                    {
                        actor.ConnectionStage = TcpServerActor.Stage.Receiving;
                        ConfiguredTaskAwaitable<Task> task = receiveTasks
                                                             .StartNew(ReceivingAsync, actor, shutdownToken.Token)
                                                             .ConfigureAwait(false);
                    }
                }
            }

            logger.LogInformation("Server has stopped listening!");
        }

        /// <summary>
        ///     Exchanging receives bytes from the accepted client socket when bytes become
        ///     available as a raw buffer of bytes. This method is called once and then invokes
        ///     <see cref="ExchangingAsync(object)" />.
        /// </summary>
        /// <param name="state">Created actor around the accepted client socket</param>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private async Task ExchangingAsync(object state)
        {
            // Initialize multiple receive variables
            var actor = state as TActor;
            var timeout = new CancellationTokenSource();
            int examined = 0, remaining = 0;

            if (actor.Socket.Connected && !shutdownToken.IsCancellationRequested)
            {
                try
                {
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        timeout.Token, shutdownToken.Token);
                    // Receive data from the client socket
                    ValueTask<int> receiveOperation = actor.Socket.ReceiveAsync(
                        actor.Buffer[..],
                        SocketFlags.None,
                        cancellation.Token);

                    timeout.CancelAfter(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                    examined = await receiveOperation;

                    if (examined == 0)
                    {
                        actor.Disconnect();
                        Disconnecting(actor);
                        return;
                    }

                    if (examined < ExchangeStartPosition + 2)
                    {
                        throw new Exception("Invalid length");
                    }
                }
                catch (Exception e)
                {
                    if (e is SocketException socketEx)
                    {
                        if (socketEx.SocketErrorCode < SocketError.ConnectionAborted ||
                            socketEx.SocketErrorCode > SocketError.Shutdown)
                        {
                            logger.LogError(socketEx, "ExchangingAsync Socket Exception");
                        }
                    }
                    else
                    {
                        logger.LogError(e, "ExchangingAsync Exception");
                    }

                    actor.Disconnect();
                    Disconnecting(actor);
                    return;
                }

                int consumed;
                if (ExchangeStartPosition == 7) // for TQ
                {
                    // Decrypt traffic by first discarding the first 7 bytes, as per TQ Digital's
                    // exchange protocol, then decrypting only what is necessary for the exchange.
                    // This is to prevent the next packet from being decrypted with the wrong key.
                    actor.Cipher.Decrypt(
                        actor.Buffer[..9].Span,
                        actor.Buffer[..9].Span);
                    consumed = BitConverter.ToUInt16(actor.Buffer.Span.Slice(7, 2)) + 7;
                }
                else
                {
                    consumed = examined;
                }

                if (consumed > examined)
                {
                    logger.LogError($"Exchange error length [{consumed},{examined}] for [IP: {actor.IpAddress}]");
                    actor.Disconnect();
                    Disconnecting(actor);
                    return;
                }

                if (ExchangeStartPosition == 7)
                {
                    actor.Cipher?.Decrypt(
                        actor.Buffer[9..consumed].Span,
                        actor.Buffer[9..consumed].Span);

                }
                else
                {
                    actor.Cipher?.Decrypt(
                        actor.Buffer[..consumed].Span,
                        actor.Buffer[..consumed].Span);
                }

                // Process the exchange now that bytes are decrypted
                if (!Exchanged(actor, actor.Buffer[..consumed].Span))
                {
                    logger.LogError("Exchange error for [IP: {IpAddress}]", actor.IpAddress);
                    actor.Disconnect();
                    Disconnecting(actor);
                    return;
                }

                // Now that the key has changed, decrypt the rest of the bytes in the buffer
                // and prepare to start receiving packets on a standard receive loop.
                if (consumed < examined)
                {
                    actor.Cipher?.Decrypt(
                        actor.Buffer[consumed..examined].Span,
                        actor.Buffer[consumed..examined].Span);

                    if (!Splitting(actor, examined, ref consumed))
                    {
                        logger.LogError("[Exchange] Client disconnected due to invalid packet.");
                        actor.Disconnect();
                        Disconnecting(actor);
                        return;
                    }

                    remaining = examined - consumed;
                    actor.Buffer[consumed..examined].CopyTo(actor.Buffer);
                }
            }

            // Start receiving packets
            await ReceivingAsync(state, remaining);
        }

        /// <summary>
        ///     Receiving receives bytes from the accepted client socket when bytes become
        ///     available. While the client is connected and the server hasn't issued the
        ///     shutdown signal, bytes will be received in a loop.
        /// </summary>
        /// <param name="state">Created actor around the accepted client socket</param>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private Task ReceivingAsync(object state)
        {
            return ReceivingAsync(state, 0);
        }

        /// <summary>
        ///     Receiving receives bytes from the accepted client socket when bytes become
        ///     available. While the client is connected and the server hasn't issued the
        ///     shutdown signal, bytes will be received in a loop.
        /// </summary>
        /// <param name="state">Created actor around the accepted client socket</param>
        /// <param name="remaining">Starting offset to receive bytes to</param>
        /// <returns>Returns task details for fault tolerance processing.</returns>
        private async Task ReceivingAsync(object state, int remaining)
        {
            // Initialize multiple receive variables
            var actor = state as TActor;
            var timeout = new CancellationTokenSource();
            int examined = 0, consumed = 0;

            while (actor.Socket.Connected && !shutdownToken.IsCancellationRequested)
            {
                try
                {
#if !DEBUG
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        timeout.Token, shutdownToken.Token);
                    // Receive data from the client socket
                    ValueTask<int> receiveOperation = actor.Socket.ReceiveAsync(
                        actor.Buffer[remaining..],
                        SocketFlags.None,
                        cancellation.Token);
#else
                    ValueTask<int> receiveOperation = actor.Socket.ReceiveAsync(
                        actor.Buffer[remaining..],
                        SocketFlags.None);
#endif

                    int receiveTimeOut = actor.ReceiveTimeOutSeconds > 0
                                             ? actor.ReceiveTimeOutSeconds
                                             : ReceiveTimeoutSeconds;
                    timeout.CancelAfter(TimeSpan.FromSeconds(receiveTimeOut));
                    examined = await receiveOperation;
                    if (examined == 0)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException e)
                {
                    logger.LogError(e, "ReceivingAsync OperationCanceledException");
                    break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode is < SocketError.ConnectionAborted or > SocketError.Shutdown)
                    {
                        logger.LogError(e, "ReceivingAsync Socket Exception");
                    }

                    break;
                }

                // Decrypt traffic
                actor.Cipher?.Decrypt(
                    actor.Buffer.Slice(remaining, examined).Span,
                    actor.Buffer.Slice(remaining, examined).Span);

                // Handle splitting and processing of data
                consumed = 0;
                if (!Splitting(actor, examined + remaining, ref consumed))
                {
                    logger.LogError("Client disconnected due to invalid packet.");
                    actor.Disconnect();
                    break;
                }

                remaining = examined + remaining - consumed;
                actor.Buffer.Slice(consumed, remaining).CopyTo(actor.Buffer);
            }

            actor.Disconnect();
            // Disconnect the client
            Disconnecting(actor);
        }

        /// <summary>
        ///     Splitting splits the actor's receive buffer into multiple packets that can
        ///     then be processed by Received individually. The default behavior of this method
        ///     unless otherwise overridden is to split packets from the buffer using an unsigned
        ///     short packet header for the length of each packet.
        /// </summary>
        /// <param name="actor">Actor for consuming bytes from the buffer</param>
        /// <param name="examined">Number of examined bytes from the receive</param>
        /// <param name="consumed">Number of consumed bytes by the split reader</param>
        /// <returns>Returns true if the client should remain connected.</returns>
        protected virtual bool Splitting(TActor actor, int examined, ref int consumed)
        {
            // Consume packets from the socket buffer
            Span<byte> buffer = actor.Buffer.Span;
            while (consumed + 2 < examined)
            {
                var length = BitConverter.ToUInt16(buffer.Slice(consumed, 2));
                if (length == 0)
                {
                    return false;
                }

                int expected = consumed + length + footerLength;
                if (length > buffer.Length)
                {
                    return false;
                }

                if (expected > examined)
                {
                    break;
                }

                // TODO add footer validation?
                Received(actor, buffer.Slice(consumed, length)); // skip the footer
                consumed += length + footerLength;
            }

            return true;
        }

        /// <summary>
        ///     Disconnecting is called when the client is disconnecting from the server. Allows
        ///     the server to handle client events post-disconnect, and reclaim resources first
        ///     leased to the client on accept.
        /// </summary>
        /// <param name="actor">Actor being disconnected</param>
        private void Disconnecting(TActor actor)
        {
            // Reclaim resources and release back to server pools
            actor.Buffer.Span.Clear();
            bufferPool.Push(actor.Buffer);
            acceptanceSemaphore.Release();
            registry.RemoveActiveClient(actor.IpAddress);

            // Complete processing for disconnect
            Disconnected(actor);
        }

        protected void Close()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close(5);
        }

        // Constants
        private const int ReceiveTimeoutSeconds = 30;
    }
}
