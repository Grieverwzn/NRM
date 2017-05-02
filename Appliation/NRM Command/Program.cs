using System;
using System.IO;
using System.Linq;
using System.Text;

namespace com.foxmail.wyyuan1991.NRM.Command
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //初始化容器
            Warpper warpper = new Warpper();
            Factory factory = Factory.GetInstance(warpper);          
            string cur="";
            if (args.Count() > 0)
            {
                string path = args[0];
                StreamReader sr = new StreamReader(path, Encoding.Default);
                cur = sr.ReadToEnd();              
            }
#if DEBUG
            //cur = @"read -p C:\Users\yuanwuyang\Desktop\Net2.xls;arr -p C:\Users\yuanwuyang\Desktop\arr -n 20;cnn -i C:\Users\yuanwuyang\Desktop\arr -o C:\Users\yuanwuyang\Desktop\CNN -c C:\Users\yuanwuyang\Desktop\CNN.txt";
            //cur = @"read -p C:\Users\yuanwuyang\Desktop\Net1.xls;gen -p C:\Users\yuanwuyang\Desktop\Net1.txt; arr -p C:\Users\yuanwuyang\Desktop\arr -n 20;sim -i C:\Users\yuanwuyang\Desktop\arr -o C:\Users\yuanwuyang\Desktop\BPC -c C:\Users\yuanwuyang\Desktop\Net1.txt;oac -i C:\Users\yuanwuyang\Desktop\arr -o C:\Users\yuanwuyang\Desktop\OAC";
            cur = @"read -p C:\Users\yuanwuyang\Desktop\Net1.xls;anahead -o C:\Users\yuanwuyang\Desktop\res.txt;ana -i C:\Users\yuanwuyang\Desktop\arr -s C:\Users\yuanwuyang\Desktop\OAC\Sell -c C:\Users\yuanwuyang\Desktop\OAC\Control -o C:\Users\yuanwuyang\Desktop\res.txt";
#endif
            while (cur != "" || (cur = Console.ReadLine()) != "exit")
            {
                string[] r = cur.Split(new char[] { ';' });
                foreach (string s in r)
                {
                    args = s.Trim().Split(new char[] { ' ' });
                    if (args.Count() > 0)
                    {
                        if (args[0] == "exit") return;
                        Command Command = factory.Create(args[0]);
                        ExecuteCommand(args, Command);
                    }
                }
                cur = "";
            }
        }

        private static void ExecuteCommand(string[] args, Command Command)
        {
            Console.WriteLine("开始执行命令:{0}", args[0].ToString());
            try
            {
                if (CommandLine.Parser.Default.ParseArguments(args.Skip(1).ToArray(), Command))
                {
                    Command.ExecuteCommand();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("执行失败，原因{0}",e.Message);
            }
        }
    }

}
