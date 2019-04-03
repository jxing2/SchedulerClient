using Cn.Sagacloud.Proto;
using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NettyClient
{
    public class TaskNettyClient
    {
        private string ip;
        private int port;
        IChannel clientChannel;
        MultithreadEventLoopGroup group;
        Bootstrap bootstrap;
        public TaskNettyClient(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
        public async Task RunClientAsync(ChannelHandlerAdapter channelHandler)
        {
            group = new MultithreadEventLoopGroup();
            try
            {
                bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(
                        new ActionChannelInitializer<ISocketChannel>(
                            channel =>
                            {
                                IChannelPipeline pipeline = channel.Pipeline;
                                pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
                                pipeline.AddLast("encoder", new ProtobufEncoder());
                                pipeline.AddLast(new ProtobufVarint32FrameDecoder());
                                pipeline.AddLast("decoder", new ProtobufDecoder(Message.Parser));
                                pipeline.AddLast("simple", channelHandler);

                            }));
                clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
                //await clientChannel.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //await Task.WhenAll(group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }
        public async Task CloseAsync() {
            try
            {
                await clientChannel.CloseAsync();
            }
            catch { }
            finally {
                await Task.WhenAll(group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }
        public static void Main(string[] args)
        {
            TaskNettyClient client = new TaskNettyClient("127.0.0.1", 6666);
            SimpleMessageHandler simpleHandler = new SimpleMessageHandler();
            client.RunClientAsync(simpleHandler).Wait();
            while (true) {
                Thread.Sleep(15000);
                client.CloseAsync().Wait();
                // 重启
                //client.RunClientAsync(new SimpleHandler()).Wait();
            }
        }
    }

    public static class NettyHelper
    {
        //static NettyHelper()
        //{
        //    Configuration = new ConfigurationBuilder()
        //    //    .SetBasePath(ProcessDirectory)
        //    //    .AddJsonFile("appsettings.json")
        //        .Build();
        //}
        
        //public static IConfigurationRoot Configuration { get; }

        //public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
        public static void SetConsoleLogger()
        {
            InternalLoggerFactory.DefaultFactory.AddProvider(
                new ConsoleLoggerProvider((s, level) => { level = Microsoft.Extensions.Logging.LogLevel.Error; return true; }, false)
            );
        }
    }
}
