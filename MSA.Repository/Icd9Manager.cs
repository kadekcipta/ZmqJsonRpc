using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSA.LocalCache.Models;
using FluentNHibernate;
using NHibernate.Criterion;
using NHibernate.Linq.Expressions;

namespace MSA.Repository
{
    public class Icd9Manager
    {
        public IList<Icd9> FindByTitle(string keywords)
        {
            using (var session = DBSessionHelper.DefaultSessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    var c = session.CreateCriteria<Icd9>();
                    var criterion = Expression.Or(Expression.Like("Description", keywords, MatchMode.Anywhere), Expression.Like("Code", keywords, MatchMode.Anywhere));
                    c.Add(criterion);
                    c.SetMaxResults(500);
                    return c.List<Icd9>();
                }
            }
        }

        public IList<Icd9> FindBySectionAndSubSection(string section, string subsection)
        {
            using (var session = DBSessionHelper.DefaultSessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    var c = session.CreateCriteria<Icd9>();
                    var criterion = Expression.And(Expression.Like("Section", section, MatchMode.Anywhere), Expression.Like("SubSection", subsection, MatchMode.Anywhere));
                    c.Add(criterion);
                    c.SetMaxResults(500);
                    return c.List<Icd9>();
                }
            }
        }

        public IList<string> GetSection()
        {
            using (var session = DBSessionHelper.DefaultSessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    var d = Projections.Distinct(Projections.ProjectionList()
                        .Add(Projections.Property("Section")));
                    var result = session.QueryOver<Icd9>()
                        .Select(d);
                    return result.List<string>();
                }
            }  
        }

        public IList<string> GetSubSection(string section)
        {
            using (var session = DBSessionHelper.DefaultSessionFactory.OpenSession())
            {
                using (session.BeginTransaction())
                {
                    var d = Projections.Distinct(Projections.ProjectionList()
                        .Add(Projections.Property("SubSection")));
                    var result = session.QueryOver<Icd9>()
                        .Where(p=> p.Section==section)
                        .Select(d);
                    return result.List<string>();
                }
            }  
        }
    }
}
