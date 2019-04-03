using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpDownload;
using NettyClient;
using NettyClient.proto;
using Newtonsoft.Json;
using TaskDatabase;
using TaskDatabase.Model;

namespace Client
{
    class TaskHandler
    {
        private SimpleMessageHandler simpleHandler;
        private TaskService taskService;
        public IList<TaskModel> taskModels;
        private TaskDownloadManager taskDownloadManager;
        private string defaultFileDir;
        IRevitCommandExcutor revitCommandExcutor;

        public void RunTasks(object obj) {
            while (true) {
                try
                {
                    for (int i = taskModels.Count - 1; i > -1; --i) {
                        TaskModel task = taskModels[i];
                        TaskModel downloadException = null, commandException = null;
                        bool isAllDownloaded = true;
                        // 看当前任务依赖的文件下载状况
                        foreach (DownloadTaskModel downloadTaskModel in task.DownloadTaskModelList) {
                            DownloadTask download = taskDownloadManager.getDownloadTaskByDownloadTaskId(downloadTaskModel.Tid);
                            if (download == null) // 如果还未加入下载队列
                            {
                                // 开始下载
                                if (downloadTaskModel.Local_dir == null) {
                                    downloadTaskModel.Local_dir = defaultFileDir + downloadTaskModel.Tid + ".rvt";
                                }
                                download = taskDownloadManager.enqueueTask(task.Id, downloadTaskModel.Tid, downloadTaskModel.Task_url,
                                    downloadTaskModel.Local_dir, downloadTaskModel.Task_md5, downloadTaskModel.Downloaded_bytes);
                                taskDownloadManager.runTask(download);
                                isAllDownloaded &= false;
                            }
                            else {
                                // 如果该下载发生异常
                                if (download.Status == DownloadStatus.ExceptionStopped) {
                                    isAllDownloaded &= false;
                                    downloadException = task;
                                    break;
                                }
                                // 同步当前状态到数据库
                                if (download.Status == DownloadStatus.Completed)
                                {
                                    downloadTaskModel.Finish_time = GetTimeStampSeconds();
                                    isAllDownloaded &= true;
                                }
                                else {
                                    isAllDownloaded &= false;
                                }
                                syncSingleDownTask(download, downloadTaskModel);
                            }
                        } // foreach end
                        // 如果该下载任务下载失败
                        if (downloadException != null)
                        {
                            task.Task_status = -1;
                            taskService.UpdateTask(task);
                            taskModels.RemoveAt(i);
                            simpleHandler.WriteMessage(MessageUtil.generateMessage(Command.DownloadError.ToString(), task.Id, ""));
                            continue;
                        }
                        // 如果该任务的所有下载文件均已完成, 开始执行Revit命令
                        if (isAllDownloaded) {
                            try
                            {
                                HashSet<string> fileSet = new HashSet<string>();
                                foreach(var downloadModel in task.DownloadTaskModelList) {
                                    fileSet.Add(downloadModel.Local_dir);
                                }
                                try
                                {
                                    string resultJson = revitCommandExcutor.ExecuteCmd(task.Task_cmd, task.Task_param, fileSet);
                                    simpleHandler.WriteMessage(MessageUtil.generateMessage(Command.TaskSuccess.ToString(), task.Id, resultJson));
                                    task.Task_status = 1;
                                    task.Task_result_json = resultJson;
                                    taskService.UpdateTask(task);
                                    taskModels.RemoveAt(i);
                                }
                                catch {
                                    commandException = task;
                                }
                            }
                            catch {

                            }
                        }
                        if (commandException != null)
                        {
                            task.Task_status = -2;
                            taskService.UpdateTask(task);
                            taskModels.RemoveAt(i);
                            simpleHandler.WriteMessage(MessageUtil.generateMessage(Command.CommandError.ToString(), task.Id, ""));
                            continue;
                        }
                    }
                    taskService.removeErrorTask();
                }
                catch (Exception ex){
                    Console.WriteLine(ex.StackTrace);
                }
                Thread.Sleep(1000);
                
            }
        }

        private void syncSingleDownTask(DownloadTask downTask, DownloadTaskModel downloadModel)
        {
            bool changed = false;
            if (downloadModel.Downloaded_bytes != downTask.BytesWritten)
            {
                downloadModel.Downloaded_bytes = downTask.BytesWritten;
                changed = true;
            }
            if (downloadModel.File_bytes != downTask.totalFileSize)
            {
                downloadModel.File_bytes = downTask.totalFileSize;
                changed = true;
            }
            if(changed)
                taskService.UpdateDownload(downloadModel);
        }

        public void setSimpleMessageHandler(SimpleMessageHandler simpleHandler)
        {
            this.simpleHandler = simpleHandler;
        }

        private TaskHandler(SimpleMessageHandler simpleHandler, TaskService taskService)
        {
            this.simpleHandler = simpleHandler;
            this.taskService = taskService;
        }

        public TaskHandler(SimpleMessageHandler simpleHandler, TaskService taskService, TaskDownloadManager taskDownloadManager, string defaultFileDir, IRevitCommandExcutor revitCommandExcutor) : this(simpleHandler, taskService)
        {
            this.taskDownloadManager = taskDownloadManager;
            this.defaultFileDir = defaultFileDir;
            this.revitCommandExcutor = revitCommandExcutor;
        }

        internal void SyncAllTask()
        {
            IList<TaskModel> tasks = taskService.GetTasksByStatus(0); // 任务状态 0 未执行完的任务, 1 执行完成  -1 下载出错  -2 revit命令执行异常
            taskModels = tasks;
        }
        internal bool addOneTask(int taskId, string msgContent) {
            try
            {
                TaskModel taskModel = JsonConvert.DeserializeObject<TaskModel>(msgContent);
                taskService.AddTask(taskModel);
                TaskModel task = taskService.GetTasksByTaskId(taskId)[0];
                taskModels.Add(task);
                foreach (DownloadTaskModel download in taskModel.DownloadTaskModelList) {
                    download.Local_dir = defaultFileDir + download.Tid + ".rvt";
                    taskService.UpdateDownload(download);
                }
                return true;
            }
            catch {
                return false;
            }
        }

        internal bool isContainTask(int taskId)
        {
            try
            {
                return taskService.isContainsTask(taskId);
            }
            catch {
                return true;
            }
        }

        public static long GetTimeStampSeconds() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long)ts.TotalSeconds;
        }
    }
}
