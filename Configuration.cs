using Antibayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AntiBeyer
{
    public class Configuration
    {
        public string LastFolder;
        public HistogramSettings HistogramSettings;

        public static string ConfigFolder
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\";
            }
        }
        public static string ConfigFile
        {
            get
            {
                return Path.Combine(ConfigFolder,"BayerAdjust.config");
            }
        }
        public static Configuration GetConfig()
        {
            if (File.Exists(ConfigFile))
            {
                var streamReader = new System.IO.StringReader(ConfigFile);
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    FileStream file = new FileStream(ConfigFile, FileMode.Open);
                    var temp = serializer.Deserialize(file) as Configuration;
                    file.Close();                    
                    return temp;
                }
                catch
                {

                }
            }
            return null;
        }

        internal string StoreConfig()
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(Configuration));
                var streamWriter = new System.IO.StreamWriter(ConfigFile);
                xmlSerializer.Serialize(streamWriter, this);
                streamWriter.Close();
                return null;
            }
            catch (Exception error)
            {
                return "Error saving job config: " + error.Message;
            }
        }
    }
}
