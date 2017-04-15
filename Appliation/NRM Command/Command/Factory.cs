using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.Command
{
    public class Factory
    {
        private Warpper warpper;
        private Command cmd;
        private Dictionary<string, Command> dictNP = new Dictionary<string, Command>();

        private static Factory factory;
        public static Factory GetInstance(Warpper _warpper)
        {
            if (null == factory)
            {
                factory = new Factory();
                factory.warpper = _warpper;                
            }
            return factory;
        }

        public Command Create(string name)
        {
            foreach (KeyValuePair<string, Command> k in dictNP)
            {
                if (name == k.Key)
                    return k.Value;
            }

            try
            {
                cmd = null;
                if ("read" == name)
                    cmd = new ReadDataCommand(warpper);
                else if ("gen" == name)
                    cmd = new GenBidPriceCommand(warpper);
                else if ("arr" == name)
                    cmd = new SimArrCommand(warpper);
                else if ("sim" == name)
                    cmd = new SimBPCCommand(warpper);
                else if ("oac" == name)
                    cmd = new SimOACCommand(warpper);
                else if ("cnn" == name)
                    cmd = new SimCNNestingCommand(warpper); 
                else if ("ana" == name)
                    cmd = new AnalysisCommand(warpper);               
                else if ("anahead" == name)
                    cmd = new ShowIndexsCommand(warpper);
                else
                    return null;
            }
            catch
            {
                throw;
            }

            dictNP.Add(name, cmd);
            return cmd;
        }
    }
}
