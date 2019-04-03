using Cn.Sagacloud.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NettyClient.proto
{
    public class MessageUtil
    {
        public static Message generateMessage(string cmd, int taskId, string content) {
            Message msg = new Message();
            msg.Cmd = cmd;
            msg.TaskId = taskId;
            msg.Content = content;
            return msg;
        }
    }
}
