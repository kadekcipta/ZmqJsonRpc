using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MSA.Zmq.Service
{
    public class ZMSAConfigurationSectionHandler: ConfigurationSection
    {
        [ConfigurationProperty("handlers", IsRequired=true)]
        public ZMSAServiceHandlerCollection Handlers 
        {
            get 
            {
                return (ZMSAServiceHandlerCollection)base["handlers"];
            }
        }
    }

    [ConfigurationCollection(typeof(ZMSAServiceHandlerElement))]
    public class ZMSAServiceHandlerElement : ConfigurationElement
    {
        [ConfigurationProperty("assemblyName", IsRequired=true)]
        public string AssemblyName
        {
            get
            {
                return (string)base["assemblyName"];
            }
            set
            {
                base["assemblyName"] = value;
            }
        }
        
        [ConfigurationProperty("handlerName", IsRequired = true, IsKey=true)]
        public string HandlerName
        {
            get
            {
                return (string)base["handlerName"];
            }
            set
            {
                base["handlerName"] = value;
            }
        }

        [ConfigurationProperty("endpointPrefix", IsRequired=false, IsKey=true)]
        public string EndPointPrefix
        {
            get
            {
                return (string)base["endpointPrefix"];
            }
            set
            {
                base["endpointPrefix"] = value;
            }
        }
    }

    public class ZMSAServiceHandlerCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ZMSAServiceHandlerElement();
        }

        public ZMSAServiceHandlerElement this[int index]
        {
            get
            {
                return (ZMSAServiceHandlerElement)BaseGet(index);
            }
        }


        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ZMSAServiceHandlerElement)element).HandlerName;
        }
    }
}
