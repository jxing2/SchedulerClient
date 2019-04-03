using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface IRevitCommandExcutor
    {
        /// <summary>
        /// 执行Revit命令
        /// </summary>
        /// <param name="revitCmd">被执行的Revit命令</param>
        /// <param name="param">执行命令的参数</param>
        /// <param name="filePathList">用到的文件的路径</param>
        /// <returns>任务执行结果返回json格式字符串</returns>
        string ExecuteCmd(string revitCmd, string param, HashSet<string> filePathList);
    }
}
