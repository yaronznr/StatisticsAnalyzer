using System;
using System.Linq;
using System.Web;
using System.Xml;

namespace StatisticsAnalyzerCore.StatConfig
{
    public class StatConfig
    {
        private readonly XmlDocument _config;

        public StatConfig(string fileName)
        {
            _config = new XmlDocument();
            try
            {
                _config.Load(HttpContext.Current.Server.MapPath(string.Format("~/bin/{0}", fileName)));

            }
            catch (Exception)
            {
                _config.Load(string.Format("bin/{0}", fileName));
            }
        }

        public string ReadString(string path)
        {
            var pathParts = path.Split('.');
            var nodeList = _config.GetElementsByTagName(pathParts[0])[0].ChildNodes;

            foreach (var pathPart in pathParts.Skip(1))
            {
                XmlNodeList nextNodeList = null;
                foreach (XmlNode node in nodeList)
                {
                    if (node.Name == pathPart)
                    {
                        nextNodeList = node.ChildNodes;
                    }
                }

                if (nextNodeList == null) return null;
                nodeList = nextNodeList;
            }

            var xmlNode = nodeList.Item(0);
            if (xmlNode != null)
            {
                var t = xmlNode.InnerText;
                return t;
            }

            return null;
        }

        public double ReadDecimal(string path)
        {
            return double.Parse(ReadString(path));
        }

        public bool ReadBool(string path)
        {
            return bool.Parse(ReadString(path));
        }
    }
}
