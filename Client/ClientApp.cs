using DotNetty.Transport.Channels;
using HttpDownload;
using NettyClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskDatabase;

namespace Client
{
    public class ClientApp
    {
        TaskNettyClient client;                    // 网络传输客户端
        SimpleMessageHandler simpleHandler;        // 网络传输处理
        TaskService taskService;                   // 持久化任务
        TaskDownloadManager taskDownloadManager;   // 下载任务文件(Http)
        MessageHandler messageHandler;             // 处理客户端收到的消息
        TaskHandler taskHandler;                   // 处理服务端发送的任务(监视下载状况等待)
        int maxTaskCount;                          // 能处理的最大任务数量, 影响是否拒绝服务器分发的任务
        string defaultFileDir;                     // 默认文件目录
        IRevitCommandExcutor revitCommandExcutor;  
        public ClientApp(string ip, int port, string dir, int maxTaskCount, IRevitCommandExcutor revitCommandExcutor, int maxDownTaskCount=5) {
            client = new TaskNettyClient(ip, port);
            taskService = new TaskService();
            taskDownloadManager = new TaskDownloadManager(maxDownTaskCount);
            this.maxTaskCount = maxTaskCount;
            defaultFileDir = dir;
            this.revitCommandExcutor = revitCommandExcutor;
        }

        public void Start() {
            simpleHandler = new SimpleMessageHandler();
            if (messageHandler == null)
            {
                taskHandler = new TaskHandler(simpleHandler, taskService, taskDownloadManager, defaultFileDir, revitCommandExcutor);
                taskHandler.SyncAllTask();
                messageHandler = new MessageHandler(simpleHandler, maxTaskCount, taskHandler);
                messageHandler.ResumeAllTasks();
                ThreadPool.QueueUserWorkItem(messageHandler.HandleMessage);
                ThreadPool.QueueUserWorkItem(taskHandler.RunTasks);
            }
            else {
                messageHandler.setSimpleMessageHandler(simpleHandler);
                taskHandler.setSimpleMessageHandler(simpleHandler);
            }
            client.RunClientAsync(simpleHandler).Wait();
        }

        public void Close() {
            client.CloseAsync().Wait();
        }

        public void PauseAllTask() {
            taskDownloadManager.pauseAllTasks();
            messageHandler.PauseAllTasks();
        }
        public void ResumeAllTask()
        {
            taskDownloadManager.resumeAllTasks();
            messageHandler.ResumeAllTasks();
        }
    }

    
}
