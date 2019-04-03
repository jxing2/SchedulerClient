using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskDatabase;
using TaskDatabase.Model;
using NHibernate.Cfg;
using Newtonsoft.Json;

namespace UnitTestDatabase
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize()]
        public void init() {
            var cfg = new Configuration();
            cfg.Configure();
            
        }
        [TestMethod]
        public void SelectAllValid()
        {
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
            Assert.IsTrue(tasks.Count== 2) ;
        }


        [TestMethod]
        public void TestIfExist()
        {
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
            Assert.IsTrue(service.isContainsTask(tasks[0].Id));
            Assert.IsTrue(service.isContainsTask(tasks[1].Id));
            Assert.IsFalse(service.isContainsTask(100));
        }
        [TestMethod]
        public void TestUpdate()
        {
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
            TaskModel task = tasks[0];
            string json = JsonConvert.SerializeObject(task, Formatting.None);
            Console.WriteLine(json);
            foreach (var download in task.DownloadTaskModelList) {
                download.Task_md5 = "abc";
                service.UpdateDownload(download);
            }
            json = JsonConvert.SerializeObject(task, Formatting.None);
            service.UpdateTask(task);
        }

        [TestMethod]
        public void TestUpdate2()
        {
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
            TaskModel task = tasks[0];
            string json = JsonConvert.SerializeObject(task, Formatting.None);
            Console.WriteLine(json);
            //task.Task_status = 1;
            task.Task_expected_finish_time = 100;
            json = JsonConvert.SerializeObject(task, Formatting.None);
            service.UpdateTask(task);
            TaskModel newTask = service.GetTasksByTid(task.Tid);
            json = JsonConvert.SerializeObject(task, Formatting.None);
            Console.WriteLine(json);
        }

        [TestMethod]
        public void TestAdd()
        {
            TaskService service = new TaskService();
            IList<TaskModel> tasks = service.GetTasksByStatus(0);
            TaskModel task = tasks[1];
            
            task.Task_status = 1;
            task.Task_cmd = "cmd";
            task.Id = 10;
            task.Task_expected_finish_time = 10000;
            task.Task_param = "param";
            task.Task_result_json = "json";
            service.AddTask(task);
            //TaskModel newTask = service.GetTasksByTid(task.Tid);
            //json = JsonConvert.SerializeObject(task, Formatting.None);
            //Console.WriteLine(json);
        }
    }
}
