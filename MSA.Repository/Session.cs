using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using System.Configuration;

namespace MSA.Repository
{
    internal static class DBSessionHelper
    {
        private static ISessionFactory _defaultSessionFactory;

        static DBSessionHelper()
        {
        }

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString( cfg => cfg
                    .FromConnectionStringWithKey("MSA-Default")
                ))
                .Mappings(m => 
                    m.FluentMappings.AddFromAssemblyOf<Icd9Map>()
                ).BuildSessionFactory();
        }

        public static ISessionFactory DefaultSessionFactory
        {
            get
            {
                if (_defaultSessionFactory == null)
                {
                    _defaultSessionFactory = CreateSessionFactory();
                }

                return _defaultSessionFactory;
            }
        }

        public static void CloseSessionFactory()
        {
            if (_defaultSessionFactory != null)
            {
                _defaultSessionFactory.Close();
                _defaultSessionFactory = null;
            }
        }
    }
}
