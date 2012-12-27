using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate;
using FluentNHibernate.Mapping;
using MSA.LocalCache.Models;

namespace MSA.Repository
{
    public sealed class PatientMap : ClassMap<MSA.LocalCache.Models.Patient>
    {
        public PatientMap()
        {
            Table("Patient");
            Id(x => x.ID, "pat_id");
            Map(x => x.Name, "pat_name");
        }
    }
}
