using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskDatabase.Model;

namespace TaskDatabase
{
    public class TaskService
    {
        public bool AddTask(TaskModel task) {
            ITransaction transaction = null;
            try
            {
                //如果存在该任务, 则不允许添加
                if (isContainsTask(task.Id))
                {
                    return false;
                }
                using (ISession session = NHibernateHelper.OpenSession()) {
                    using (transaction = session.BeginTransaction()) {
                        try
                        {
                            session.Save(task);
                            if(task.DownloadTaskModelList != null)
                            {
                                foreach(var download in task.DownloadTaskModelList)
                                {
                                    session.Save(download);
                                }
                            }
                            transaction.Commit();
                        }
                        catch {
                            if (transaction != null && transaction.IsActive)
                            {
                                transaction.Rollback();
                            }
                        }
                    }
                }
                return true;
            }
            catch {}
            return false;
        }

        public bool UpdateTask(TaskModel task)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        session.Update(task);
                        transaction.Commit();
                    }
                }
                return true;
            }
            catch { }
            return false;
        }
        public bool UpdateDownload(DownloadTaskModel download)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        session.Update(download);
                        transaction.Commit();
                    }
                }
                return true;
            }
            catch { }
            return false;
        }
        public IList<TaskModel> GetTasksByStatus(int status)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        var queryResult = session.CreateCriteria(typeof(TaskModel))
                                .Add(Restrictions.Eq("Task_status", status))
                                .List<TaskModel>();
                        return queryResult;
                    }
                }
            }
            catch (Exception ex){
                
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public TaskModel GetTasksByTid(int tid)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        var queryResult = session.CreateCriteria(typeof(TaskModel))
                                .Add(Restrictions.Eq("Tid", tid))
                                .UniqueResult<TaskModel>();
                        return queryResult;
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public IList<TaskModel> GetTasksByTaskId(int taskId)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        var queryResult = session.CreateCriteria(typeof(TaskModel))
                                .Add(Restrictions.Eq("Id", taskId))
                                .List<TaskModel>();
                        return queryResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public bool isContainsTask(int taskId)
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        var queryResult = session.CreateCriteria(typeof(TaskModel))
                                .Add(Restrictions.Eq("Id", taskId))
                                .List<TaskModel>();
                        return queryResult.Count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }

        public void removeErrorTask()
        {
            ITransaction transaction = null;
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (transaction = session.BeginTransaction())
                    {
                        var queryResult = session.CreateCriteria(typeof(TaskModel))
                                .Add(Restrictions.Le("Task_status", -1))
                                .List<TaskModel>();
                        for (int i = 0; i < queryResult.Count; ++i) {
                            session.Delete(queryResult[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
