using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpDownload
{
    public class TaskDownloadManager
    {
        public int Capacity { get; set; }

        // DownloadTask list
        ArrayList allTasks = new ArrayList();

        LimitedConcurrencyLevelTaskScheduler lcts = null;
        TaskFactory factory = null;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">最大同时任务数[1, 30], 异常为5</param>
        public TaskDownloadManager(int count)
        {
            //ThreadPool.SetMaxThreads(count, count);
            if (count >= 1 && count <= 30)
                Capacity = count;
            else
                Capacity = 5;
            lcts = new LimitedConcurrencyLevelTaskScheduler(Capacity);
            factory = new TaskFactory(lcts);
        }
        /// <summary>
        /// 添加任务到队列
        /// </summary>
        /// <param name="fileUrl">文件的下载地址</param>
        /// <param name="fileDir">文件再本地的保存路径</param>
        /// <param name="offset">在文件中开始下载的的位置, byte</param>
        /// <returns>任务</returns>
        public DownloadTask enqueueTask(string fileUrl, string fileDir, string expectedMD5, long offset = 0L)
        {
            DownloadTask task = new DownloadTask(fileUrl, fileDir, expectedMD5, offset);
            allTasks.Add(task);
            return task;
        }
        public DownloadTask enqueueTask(int belongingId, int downTaskId, string fileUrl, string fileDir, string expectedMD5, long offset = 0L)
        {
            DownloadTask task = new DownloadTask(belongingId, downTaskId, fileUrl, fileDir, expectedMD5, offset);
            allTasks.Add(task);
            return task;
        }

        public DownloadTask getDownloadTaskByDownloadTaskId(int downloadTaskId) {
            foreach (DownloadTask down in allTasks) {
                if (downloadTaskId == down.DownloadTaskId) {
                    return down;
                }
            }
            return null;
        }
        public void runAllTasks()
        {
            foreach (DownloadTask task in allTasks)
            {
                task.resumeTask();
                factory.StartNew(() => { DownloadTask.download(task); });
            }
        }

        public void pauseAllTasks()
        {
            foreach (DownloadTask task in allTasks)
            {
                task.pauseTask();
            }
        }
        public void resumeAllTasks()
        {
            foreach (DownloadTask task in allTasks)
            {
                task.resumeTask();
                factory.StartNew(() => { DownloadTask.download(task); });
            }
        }

        public ArrayList getAllTasks() { return allTasks; }

        public Hashtable getAllTasksMap() {
            Hashtable table = new Hashtable();
            foreach (DownloadTask down in allTasks) {
                int belongingid = down.BelongingId;
                if (table.ContainsKey(belongingid))
                {
                    ArrayList arr = (ArrayList)table[belongingid];
                    arr.Add(down);
                }
                else {
                    ArrayList arr = new ArrayList();
                    arr.Add(down);
                    table.Add(belongingid, arr);
                }
            }
            return table;
        }

        public void runTask(DownloadTask task)
        {
            task.resumeTask();
            factory.StartNew(() => { DownloadTask.download(task); });
        }

        public void pauseTask(DownloadTask task)
        {
            task.pauseTask();
        }
        public void resumeTask(DownloadTask task)
        {
            task.resumeTask();
            factory.StartNew(() => { DownloadTask.download(task); });
        }
        public void removeTask(DownloadTask task)
        {
            task.pauseTask();
            allTasks.Remove(task);
        }
    }
}
