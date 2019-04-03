using Cn.Sagacloud.Proto;
using NettyClient;
using NettyClient.proto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class MessageHandler
    {
        private SimpleMessageHandler simpleHandler;
        private TaskHandler taskHandler;
        private bool isPauseTask = false;
        private int maxTaskCount;
        public MessageHandler(SimpleMessageHandler simpleHandler, int maxTaskCount, TaskHandler taskHandler) {
            this.simpleHandler = simpleHandler;
            this.maxTaskCount = maxTaskCount;
            this.taskHandler = taskHandler;
        }
        public void setSimpleMessageHandler(SimpleMessageHandler simpleHandler) {
            this.simpleHandler = simpleHandler;
        }
        public void HandleMessage(object obj) {
            while (true) {
                Message message;
                bool hasMsg = SimpleMessageHandler.messageQueue.TryDequeue(out message);
                if (!hasMsg) {
                    Thread.Sleep(500);
                    continue;
                }
                Command command = Command.Useless;
                try
                {
                     command = (Command)Enum.Parse(typeof(Command), message.Cmd);
                }
                catch { command = Command.Useless; }
                Message retMsg;
                switch (command) {
                    case Command.SendTask:
                        // 1. 检测是否接受该任务 
                        if (checkIsAcceptTask(message.TaskId))
                        {
                            // 2. 如果接受, 保存任务到数据库, 并同步该任务到内存, 并交由任务执行线程开始执行
                            bool isSuccess = taskHandler.addOneTask(message.TaskId, message.Content);
                            if (isSuccess)
                            {
                                retMsg = MessageUtil.generateMessage(Command.AcceptTask.ToString(), message.TaskId, "");
                                simpleHandler.WriteMessage(retMsg);
                            }
                        }
                        else {
                            // 3. 如果拒绝, 直接返回拒绝消息
                            retMsg = MessageUtil.generateMessage(Command.RefuseTask.ToString(), message.TaskId, "");
                            simpleHandler.WriteMessage(retMsg);
                        }
                        break;
                    case Command.ClientInfo: // 服务端要求客户端返回客户端信息
                        ClientInfo info = new ClientInfo();
                        info.Ipv4 = ClientInfoUtil.GetClientLocalIPv4Address();
                        info.MacAddr = ClientInfoUtil.GetMacAddress();
                        info.Name = ClientInfoUtil.GetUserName();
                        retMsg = MessageUtil.generateMessage(Command.ClientInfo.ToString(), 0, JsonConvert.SerializeObject(info, Formatting.None));
                        simpleHandler.WriteMessage(retMsg);
                        break;
                    default:
                        break;
                }
            }
        }

        private bool checkIsAcceptTask(int taskId)
        {
            //一. isPauseTask == true的时候拒绝任务, 二. 如果已存在该任务, 则拒绝, 三. 如果当前任务数达到maxTaskCount, 拒绝
            if (isPauseTask)
                return false;
            if (taskHandler.isContainTask(taskId)) {
                return false;
            }
            if (taskHandler.taskModels.Count >= maxTaskCount)
                return false;
            return true;


        }

        internal void PauseAllTasks()
        {
            isPauseTask = true;
        }

        internal void ResumeAllTasks()
        {
            isPauseTask = false;
        }
    }

    enum Command
    {
        SendTask,
        RefuseTask,
        AcceptTask,
        DownloadError,
        CommandError,
        TaskSuccess,
        ClientInfo,
        Useless
    }
}
