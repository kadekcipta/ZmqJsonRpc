using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.LocalCache.Models
{
    public class Patient
    {
        public virtual string Name { get; set; }
        public virtual int ID { get; set; }
        public virtual string Address { get; set; }
        public virtual byte[] Photo { get; set; }
    }

    public class PatientSearchCriteria
    {
        public DateTime? BirthDate { get; set; }
        public string NameLike { get; set; }
    }

    public class NestedSearchCriteria
    {
        public PatientSearchCriteria Criteria { get; set; }
        public string Description { get; set; }
    }
}
