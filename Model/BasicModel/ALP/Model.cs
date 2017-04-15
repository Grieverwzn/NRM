using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.foxmail.wyyuan1991.NRM.ALP
{
    //决策集
    public class Decision : IALPDecision, IMDPDecision, IEquatable<Decision>
    {
        private HashSet<IProduct> openProductSet = new HashSet<IProduct>();

        public Decision()
        {

        }
        public Decision(Decision d)//从其他Dicision 中复制
        {
            IProduct[] p = new IProduct[d.OpenProductSet.Count];
            d.OpenProductSet.CopyTo(p);
            this.OpenProductSet.UnionWith(p);
        }
        public Decision(HashSet<IProduct> openset)
        {
            openProductSet = openset;
        }

        public HashSet<IProduct> OpenProductSet
        {
            get
            {
                return openProductSet;
            }
        }
        public bool CanSupport(IProduct p)
        {
            if (OpenProductSet.Contains(p)) return true;
            else return false;
        }
        public bool UseResource(IALPResource r)
        {
            return this.openProductSet.FirstOrDefault(i => i.Contains(r)) != null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IProduct ts in openProductSet)
            {
                sb.AppendLine(ts.ToString() + ";");
            }
            return sb.ToString();
        }
        public bool Equals(Decision other)
        {
            if (other.openProductSet.Count == this.openProductSet.Count)
            {
                foreach(IProduct p in other.openProductSet)
                {
                    if(!openProductSet.Contains(p))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
    public class DecisionSpace : List<IMDPDecision>, IALPDecisionSpace, IMDPDecisionSpace
    {
        public IMDPDecision CloseAllDecision()
        {
            return this.FirstOrDefault(i => (i as Decision).OpenProductSet.Count == 0);
        }

        public void UnionWith(IMDPDecisionSpace tempSet)
        {
            base.AddRange(tempSet);
        }
    }

    //State
    public class State : Dictionary<IALPResource, int>, IMDPState, IALPState
    {
        /// <summary>
        /// 在某种状态s下是否能开放p产品。
        /// </summary>
        public bool CanSupportProduct(IProduct p)
        {
            foreach (IALPResource r in p)
            {
                int x = -1;
                if (!(this.TryGetValue(r, out x) && x > 0)) return false;
            }
            return true;
        }
        //public bool CanSupportRoute(Route r)
        //{
        //    foreach(Product p in r)
        //    {
        //        if(!this.CanSupportProduct(p))
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}
        public bool CanSupportDecision(Decision d)
        {
            foreach (IProduct p in d.OpenProductSet)
            {
                if (!this.CanSupportProduct(p)) return false;
            }
            return true;
        }
        public bool Within(IALPState other)
        {
            State temp = other as State;
            if (temp == null) return false;
            foreach (IALPResource r in this.Keys)
            {
                if (!(temp.Keys.Contains(r) && temp[r] >= this[r]))
                {
                    return false;
                }
            }
            return true;
        }
        #region 实现IState
        public IMDPState Clone()
        {
            State s = new State();
            foreach (IALPResource ts in this.Keys)
            {
                s.Add(ts, this[ts]);
            }
            return s;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IALPResource ts in this.Keys)
            {
                sb.AppendLine(ts.ToString() + ":" + this[ts]);
            }
            return sb.ToString();
        }
        public bool Equals(IMDPState other)
        {
            if (this.Keys.Count != (other as State).Keys.Count) return false;
            foreach (IALPResource ts in this.Keys)
            {
                if (!((other as State).Keys.Contains(ts) && (other as State)[ts] == this[ts])) return false;
            }
            return true;
        }
        #endregion

    }
    public class StateSpace : List<IMDPState>, IMDPStateSpace
    {
        public void AddRange(List<IMDPState> templist)
        {
            base.AddRange(templist);
        }

        //public State MinusOneUnit(IMDPState s, Route route)
        //{
        //    IMDPState _s = s.Clone();
        //    foreach (Product p in route)
        //    {
        //        foreach (Resource r in p)
        //        {
        //            if ((_s as State).Keys.Contains(r) && (_s as State)[r] > 0)
        //            {
        //                (_s as State)[r] -= 1;
        //            }
        //        }
        //    }
        //    return this.FirstOrDefault(i => i.Equals(_s)) as State;
        //}
        public State MinusOneUnit(IMDPState s, IProduct p)
        {
            IMDPState _s = s.Clone();
            foreach (IALPResource r in p)
            {
                if ((_s as State).Keys.Contains(r) && (_s as State)[r] > 0)
                {
                    (_s as State)[r] -= 1;
                }
            }            
            return this.FirstOrDefault(i => i.Equals(_s)) as State;
        }
        public State MinusOneUnit(IMDPState s, IALPResource r)
        {
            IMDPState _s = s.Clone();
            if ((_s as State).Keys.Contains(r) && (_s as State)[r] > 0)
            {
                (_s as State)[r] -= 1;
            }
            return this.FirstOrDefault(i => i.Equals(_s)) as State;
        }
        public State PlusOneUnit(IMDPState s, IALPResource r)
        {
            IMDPState _s = s.Clone();
            if ((_s as State).Keys.Contains(r) && (_s as State)[r] >= 0)
            {
                (_s as State)[r] += 1;
            }
            return _s as State;
        }
    }
}
