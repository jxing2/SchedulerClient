using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpDownload
{
    public class DownloadTask
    {
        public int BelongingId { get; set; }
        public int DownloadTaskId { get; set; }
        public string FileUrl { get; }
        public string FileDir { get; }
        public long Offset { get; } // 开始下载的位置
        public string ExpectedMD5 { get; }

        private volatile DownloadStatus status;
        public DownloadStatus Status { get { return status; } }
        private string exceptionInfo;
        public string ExceptionInfo { get { return exceptionInfo; } } //  保存发生异常时的Message
        private double percentage = 0d;
        private HttpDownloader httpDownloader = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileUrl"></param>
        /// <param name="fileDir"></param>
        /// <param name="expectedMD5"></param>
        /// <param name="offset">如果 offset == -1, 则把已写入文件的字节数当作offset</param>
        public DownloadTask(string fileUrl, string fileDir, string expectedMD5, long offset = 0L)
        {
            this.FileUrl = fileUrl;
            this.FileDir = fileDir;
            this.Offset = offset;
            this.ExpectedMD5 = expectedMD5;
            status = DownloadStatus.Created;
            exceptionInfo = string.Empty;
            httpDownloader = new HttpDownloader(fileUrl, fileDir, offset);

        }

        public DownloadTask(int belongingId, int downTaskId, string fileUrl, string fileDir, string expectedMD5, long offset = 0L)
        {
            this.BelongingId = belongingId;
            this.DownloadTaskId = downTaskId;
            this.FileUrl = fileUrl;
            this.FileDir = fileDir;
            this.Offset = offset;
            this.ExpectedMD5 = expectedMD5;
            status = DownloadStatus.Created;
            exceptionInfo = string.Empty;
            httpDownloader = new HttpDownloader(fileUrl, fileDir, offset);
        }


        internal static void download(DownloadTask dTask)
        {
            try
            {
                if (dTask.status.Equals(DownloadStatus.Paused))
                    return;
                dTask.status = DownloadStatus.Processing;
                dTask.httpDownloader.DownloadFile();
                if (dTask.httpDownloader.running)
                    dTask.status = DownloadStatus.Completed;
            }
            catch (Exception ex)
            {
                dTask.status = DownloadStatus.ExceptionStopped;
                dTask.exceptionInfo = ex.Message;
            }
        }

        public string calculateMD5()
        {
            if (Status.Equals(DownloadStatus.Completed))
            {
                string md5Digest = "";
                return md5Digest;
            }
            return null;
        }

        public void pauseTask()
        {
            if (status == DownloadStatus.Completed)
                return;
            httpDownloader.running = false;
            status = DownloadStatus.Paused;
        }
        public void resumeTask()
        {
            if (!httpDownloader.running)
            {
                httpDownloader.running = true;
                status = DownloadStatus.WaitToRun;
            }
        }
        public bool isFinished()
        {
            return httpDownloader.Finished;
        }
        public string getFileName()
        {
            return Path.GetFileName(FileDir);
        }

        public long totalFileSize
        {
            get
            {
                if (httpDownloader != null)
                    return httpDownloader.TotalRemoteFileSize;
                return 0;
            }
        }
        public long BytesWritten
        {
            get
            {
                if (httpDownloader != null)
                    return httpDownloader.BytesWritten;
                return 0;
            }
        }
        public string getSpeed(bool isFormatted)
        {
            if (httpDownloader != null && DownloadStatus.Processing.Equals(status))
                return httpDownloader.getSpeed(isFormatted);
            return "0 B/s";
        }
        public double getPercentage(int decimals)
        {
            if (httpDownloader != null)
                return httpDownloader.getPercentage(decimals);
            return 0d;
        }
    }
    public enum DownloadStatus
    {
        Created, // 初始状态
        WaitToRun, // 进入队列等待被执行 
        Paused,    // 已暂停
        Processing,// 正在执行
        Completed, // 完成
        ExceptionStopped // 出现异常已暂停
    }
}
