using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.foxmail.wyyuan1991.NRM.RailwaySolver
{
    public class ExpRecord
    {
        public int ID { get; set; }

        public double Lamada { get; set; }
        public double TransiteRate { get; set; }
        public double transitUtility { get; set; }

        public double LoadFactor { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return ID + "\t" + Lamada + "\t" + TransiteRate + "\t" + transitUtility + "\t" + LoadFactor + "\t" + Value;
        }
    }

    public class ExpResult : HashSet<ExpRecord>
    {
        public void WriteToFile(string Path)
        {
            FileStream fs = new FileStream("D:\\A.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            foreach (ExpRecord r in this)
            {
                sw.WriteLine(r.ToString());
            }
            sw.Close();
            fs.Close();
        }
    }
}
