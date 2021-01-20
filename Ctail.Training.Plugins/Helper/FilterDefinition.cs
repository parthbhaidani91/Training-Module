using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ctail.Training.Plugins.Helper
{
    [DataContract]
    public class Condition
    {
        [DataMember]
        public string attribute { get; set; }
        [DataMember]
        public string @operator { get; set; }
        [DataMember]
        public string uiname { get; set; }
        [DataMember]
        public string uitype { get; set; }
        [DataMember]
        public string value { get; set; }
        [DataMember(Name = "adx.id")]
        public string AdxId { get; set; }
    }

    [DataContract]
    public class Filter
    {
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public List<object> filters { get; set; }
        [DataMember]
        public List<Condition> conditions { get; set; }
        [DataMember(Name = "adx.uiname")]
        public string AdxUiname { get; set; }
    }

    [DataContract]
    public class Link
    {
        [DataMember(Name = "adx.filtertype")]
        public string AdxFiltertype { get; set; }
        [DataMember(Name = "adx.id")]
        public int AdxId { get; set; }
        [DataMember(Name = "adx.uiorder")]
        public int AdxUiorder { get; set; }
        [DataMember(Name = "adx.rawfetch")]
        public string AdxRawfetch { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string from { get; set; }
        [DataMember]
        public string to { get; set; }
        [DataMember]
        public string visible { get; set; }
        [DataMember]
        public string intersect { get; set; }
        [DataMember(Name = "adx.uiselectionmode")]
        public string AdxUiselectionmode { get; set; }
        [DataMember]
        public List<Filter> filters { get; set; }
        [DataMember]
        public List<Link2> links { get; set; }
    }

    [DataContract]
    public class Link2
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string from { get; set; }
        [DataMember]
        public string to { get; set; }
        [DataMember]
        public string alias { get; set; }
        [DataMember(Name = "adx.uiselectionmode")]
        public string AdxUiselectionmode { get; set; }
        [DataMember]
        public List<Filter> filters { get; set; }
        [DataMember]
        public List<Link> links { get; set; }
    }

    [DataContract]
    public class EntityClass
    {
        [DataMember]
        public string name { get; set; }
        [DataMember(Name = "all-attributes")]
        public bool AllAttributes { get; set; }
        [DataMember]
        public List<Filter> filters { get; set; }
        [DataMember]
        public List<Link> links { get; set; }
    }

    [DataContract]
    public class FilterDefinition
    {
        [DataMember(Name = "xmlns.adx")]
        public string XmlnsAdx { get; set; }
        [DataMember]
        public bool distinct { get; set; }
        [DataMember]
        public EntityClass entity { get; set; }
    }
}
