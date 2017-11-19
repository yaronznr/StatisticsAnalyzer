using System.Configuration;

namespace WebApp
{
    public class StatsWebConfig : ConfigurationSection
    {
        // Create a "font" element.
        [ConfigurationProperty("isLocal")]
        public IsLocalElement IsLocal
        {
            get
            {
                return (IsLocalElement)this["isLocal"];
            }
            set
            { this["isLocal"] = value; }
        }
    }

    // Define the "font" element
    // with "name" and "size" attributes.
    public class IsLocalElement : ConfigurationElement
    {
        [ConfigurationProperty("isLocal", DefaultValue = "false", IsRequired = false)]
        public bool IsLocal
        {
            get
            { return (bool)this["isLocal"]; }
            set
            { this["isLocal"] = value; }
        }
    }
}