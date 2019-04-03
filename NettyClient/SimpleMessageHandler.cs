using Cn.Sagacloud.Proto;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NettyClient
{
    public class SimpleMessageHandler : ChannelHandlerAdapter
    {
        public SimpleMessageHandler() {
        }
        public static ConcurrentQueue<Message> messageQueue = new ConcurrentQueue<Message>();
        public IChannelHandlerContext context;
        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.WriteLine("connected");
            this.context = context;
        }
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is Message msg)
            {
                toString(msg);
                messageQueue.Enqueue(msg);
            }
        }

        public bool WriteMessage(Message message) {
            if (context == null || !context.Channel.Active) {
                return false;
            }
            try
            {
                context.WriteAndFlushAsync(message);
            }
            catch {
                return false;
            }
            return true;
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
        //public override void ChannelInactive(IChannelHandlerContext context)
        //{
        //    base.ChannelInactive(context);
        //    Console.WriteLine("1");
        //}

        //public override void ChannelUnregistered(IChannelHandlerContext context)
        //{
        //    base.ChannelUnregistered(context);
        //    Console.WriteLine("2");
        //}

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            base.HandlerRemoved(context);
        }



        public void toString(Message msg)
        {
            Console.WriteLine("Received from server: cmd : " + msg.Cmd + ", taskId : " + msg.TaskId + ", content : " + msg.Content);
        }
    }
}
