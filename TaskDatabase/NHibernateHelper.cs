﻿using NHibernate;
using NHibernate.Cfg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskDatabase
{
    public class NHibernateHelper
    {
        private static object lockObj = new object();
        private static ISessionFactory _sessionFactory;

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    lock (lockObj)
                    {
                        if (_sessionFactory == null)
                        {
                            var configuration = new Configuration();
                            configuration.Configure();
                            //configuration.AddAssembly(typeof(TaskModel).Assembly);
                            _sessionFactory = configuration.BuildSessionFactory();
                        }
                    }
                }
                return _sessionFactory;
            }
        }

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
    }
}
