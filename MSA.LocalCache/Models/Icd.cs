using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.LocalCache.Models
{
    public class Icd9
    {
        public virtual string Code { get; set; }
        public virtual string Description { get; set; }
        public virtual string Section { get; set; }
        public virtual string SubSection { get; set; }
    }
}
