using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using TaskDatabase.Model;
using TaskDatabase;
using NettyClient;
using System.Threading;
using Client;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            //cfg.AddAssembly(typeof(TaskModel).Assembly);
            testMain();
            //testClientInfo();
        }

        public static void test1() {
            var cfg = new Configuration();
            cfg.Configure();
            cfg.AddAssembly(typeof(TaskModel).Assembly);

            //new SchemaExport(cfg).Execute(false, true, false, false);
            new SchemaExport(cfg).Execute(true, true, false);
        }

        public static void select()
        {
            var cfg = new Configuration();
            cfg.Configure();
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
        }

        public static void testMain() {
            ClientApp client = new ClientApp("127.0.0.1", 6666, "E:\\", 3, new myRevitCmdExecutor(), 5);
            client.Start();
            Console.ReadLine();
            client.Close();
        }

        public static void testClientInfo() {
            string ipv4 = ClientInfoUtil.GetClientLocalIPv4Address();
            string mac = ClientInfoUtil.GetMacAddress();
            string userName = ClientInfoUtil.GetUserName();
        }
    }
    public class myRevitCmdExecutor : IRevitCommandExcutor
    {
        public string ExecuteCmd(string revitCmd, string param, HashSet<string> filePathList)
        {
            return "{abcdddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd}";
        }
    }
}
