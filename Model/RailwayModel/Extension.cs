using com.foxmail.wyyuan1991.NRM.ALP;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.RailwayModel
{
    public static class Extension
    {
        public static bool CanSupportRoute(this State s, Route r)
        {
            foreach (Product p in r)
            {
                if (!s.CanSupportProduct(p))
                {
                    return false;
                }
            }
            return true;
        }

        public static State MinusOneUnit(this StateSpace ss, IMDPState s, Route route)
        {
            IMDPState _s = s.Clone();
            foreach (Product p in route)
            {
                foreach (Resource r in p)
                {
                    if ((_s as State).Keys.Contains(r) && (_s as State)[r] > 0)
                    {
                        (_s as State)[r] -= 1;
                    }
                }
            }
            return ss.FirstOrDefault(i => i.Equals(_s)) as State;
        }
    }
}
