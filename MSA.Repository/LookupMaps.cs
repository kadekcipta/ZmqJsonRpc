using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate;
using FluentNHibernate.Mapping;
using MSA.LocalCache.Models;

namespace MSA.Repository
{
    public sealed class Icd9Map: ClassMap<Icd9>
    {
        public Icd9Map()
        {
            Table("ICD9CM");
            Id(x => x.Code, "Code");
            Map(x => x.Description, "Description");
            Map(x => x.Section, "Section");
            Map(x => x.SubSection, "SubSection");
        }
    }
}
