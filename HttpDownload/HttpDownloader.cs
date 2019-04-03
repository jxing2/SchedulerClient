using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace HttpDownload
{
    class HttpDownloader
    {
        const int bytebuff = 4096;
        const int ReadWriteTimeOut = 10 * 1000;// 在写入超时或读取超时之前的毫秒数
        const int TimeOutWait = 10 * 1000;// 请求超时前等待的毫秒数
        const int MaxTryTime = 10; // 最大重试次数
        int currentRetryTimes = 0;
        private bool finished = false;
        public bool Finished { get { return finished; } }
        private long totalRemoteFileSize = 0L; // 要下载的文件的总大小
        public long TotalRemoteFileSize { get { return totalRemoteFileSize; } }
        public long BytesWritten { get { return offset; } }

        string url, destPath;
        private long offset, timeStamp, bytesAtTimeStamp;
        volatile private int speed;
        FileStream fs;
        internal volatile bool running = false;


        public HttpDownloader(string url, string destPath, long offset = 0)
        {
            this.destPath = destPath;
            this.url = url;
            this.offset = offset;
        }

        public void DownloadFile()
        {
            if (isDownloadFinished(offset))
            {
                return;
            }
            try
            {
                for (; !finished && running;)
                    HttpDownloadFile();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        const string errorInfo = "下载结束, 但是文件长度与下载字节数不一致!";
        /// <summary>
        /// 下载文件（同步）  支持断点续传
        /// </summary>
        private bool HttpDownloadFile()
        {
            //打开上次下载的文件
            //long lStartPos = 0;
            fs = getFileStream();
            HttpWebRequest request = null;
            WebResponse respone = null;
            Stream ns = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.ReadWriteTimeout = ReadWriteTimeOut;
                request.Timeout = TimeOutWait;
                if (offset > 0)
                    request.AddRange(offset);//设置Range值，断点续传

                //向服务器请求，获得服务器回应数据流
                respone = request.GetResponse();
                ns = respone.GetResponseStream();
                byte[] nbytes = new byte[bytebuff];
                int nReadSize = ns.Read(nbytes, 0, bytebuff);
                if (timeStamp == 0)
                {
                    timeStamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
                    bytesAtTimeStamp = offset;
                }
                while (nReadSize > 0 && running)
                {
                    long now = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
                    if (now - timeStamp >= 1000)
                    {
                        long dataDiff = offset - bytesAtTimeStamp, timeDiff = now - timeStamp;
                        speed = (int)(dataDiff * 1000 / timeDiff);     // 1000是由ms转为s
                        timeStamp = now;
                        bytesAtTimeStamp = offset;
                    }
                    fs.Write(nbytes, 0, nReadSize);
                    fs.Flush();
                    offset += nReadSize;
                    //Thread.Sleep(20);
                    nReadSize = ns.Read(nbytes, 0, bytebuff);
                }
                if (!running)
                    return true;
                if (offset != totalRemoteFileSize && running)//文件长度不等于下载长度，下载出错
                {
                    throw new Exception(errorInfo);
                }
                if (running)
                    finished = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals(errorInfo))
                    throw ex;
                ++currentRetryTimes;
                if (currentRetryTimes > MaxTryTime)
                {
                    throw ex;
                }
            }
            finally
            {
                if (ns != null)
                    ns.Close();
                if (fs != null)
                    fs.Close();
                if (request != null)
                    request.Abort();
            }
            return false;
        }

        private FileStream getFileStream()
        {
            if (fs != null)
            {
                fs.Close();
            }
            if (File.Exists(destPath))
            {
                fs = File.OpenWrite(destPath);
                // offset < 0 则按照文件的大小定为offset
                if (offset < 0)
                {
                    offset = fs.Length;
                }
                fs.Seek(offset, SeekOrigin.Current);//移动文件流中的当前指针
            }
            else
            {
                string dirName = Path.GetDirectoryName(destPath);

                if (!Directory.Exists(dirName))//如果不存在保存文件夹路径，新建文件夹
                {
                    Directory.CreateDirectory(dirName);
                }
                fs = new FileStream(destPath, FileMode.Create);
                offset = 0;
            }
            return fs;
        }

        /// <summary>
        /// 获取下载文件长度
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static long GetFileContentLength(string url)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Timeout = TimeOutWait;
                request.ReadWriteTimeout = ReadWriteTimeOut;
                //向服务器请求，获得服务器回应数据流
                WebResponse respone = request.GetResponse();
                request.Abort();
                return respone.ContentLength;
            }
            catch (Exception e)
            {
                if (request != null)
                    request.Abort();
                return 0;
            }
        }
        private bool isDownloadFinished(long offset)
        {
            if (finished)
                return true;
            if (totalRemoteFileSize == 0)
                totalRemoteFileSize = GetFileContentLength(url);
            if (totalRemoteFileSize != 0 && totalRemoteFileSize == offset)
            {
                //下载完成
                finished = true;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <param name="formatted"></param>
        /// <returns></returns>
        public string getSpeed(bool formatted)
        {
            if (formatted)
            {
                if (finished)
                    return "0 kB/s";
                else
                {
                    return formatSpeed(speed, 0L, 0);
                }
            }
            else
            {
                if (finished)
                    return "0";
                else
                    return speed + "";
            }
        }
        private string formatSpeed(long speed, long decimalDigit, int depth)
        {
            if (speed < 1024)
            {
                return handleDigit(speed, decimalDigit) + getUnitByDepth(depth);
            }
            else
            {
                return formatSpeed(speed / 1024, speed % 1024, depth + 1);
            }
        }
        private string handleDigit(long integer, long decimalDigit)
        {
            if (decimalDigit == 0)
            {
                return integer.ToString();
            }
            else
            {
                return integer.ToString() + Math.Round((double)decimalDigit / 1024, 1).ToString().Substring(1);
            }
        }
        private string getUnitByDepth(int depth)
        {
            string val = string.Empty;
            switch (depth)
            {
                case 0:
                    val = " B/s";
                    break;
                case 1:
                    val = " kB/s";
                    break;
                case 2:
                    val = " MB/s";
                    break;
                case 3:
                    val = " GB/s";
                    break;
                default:
                    val = " unknown/s";
                    break;
            }
            return val;
        }
        public double getPercentage(int decimals)
        {
            if (totalRemoteFileSize == 0L)
            {
                return Math.Round(0d, decimals);
            }
            if (finished)
                return Math.Round(100d, decimals);
            double percentage = (double)offset * 100d / totalRemoteFileSize;
            return Math.Round(percentage, decimals);
        }
    }
}
