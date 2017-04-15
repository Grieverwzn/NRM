using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRM.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.RailwayModel
{
    public class NRMDataAdapter : IALPFTMDP, IFTMDP, IDisposable
    {
        public List<Route> pathList { get; set; }
        public List<Product> ProSpace { get; set; }
        public Market MarketInfo { get; set; }

        public IALPResourceSpace ResSpace { get; set; }
        //public IMetaResourceSet MetaResSpace { get; set; }
        public MetaResourceSet MetaResSpace { get; set; }

        public ResouceState InitState { get; set; }

        #region 实现IMdpDataAdapter
        private IMDPDecisionSpace _ds = new DecisionSpace();
        private IMDPStateSpace _ss = new StateSpace();

        public int TimeHorizon { get; set; }
        public IMDPState InitialState { get; set; }//初始状态

        public IMDPStateSpace SS
        {
            get
            {
                return _ss;
            }
        }
        public IMDPDecisionSpace DS
        {
            get
            {
                return _ds;
            }
        }
        public IMDPDecisionSpace GenDecisionSpace(IMDPState s)
        {
            IMDPDecisionSpace res = new DecisionSpace();
            foreach (Decision _d in _ds)
            {
                if ((s as State).CanSupportDecision(_d))
                {
                    res.Add(_d);
                }
            }
            return res;
        }
        public IMDPStateSpace GenStateSpace(IMDPState s, IMDPDecision a)
        {
            //目前状态减去开放产品集中的产品
            IMDPStateSpace subss = new StateSpace();
            if ((s as State).CanSupportDecision(a as Decision))
            {
                foreach (Product p in (a as Decision).OpenProductSet)
                {
                    IMDPState _s = (_ss as StateSpace).MinusOneUnit(s, p);
                    if (!subss.Contains(_s)) subss.Add(_s);
                }
                //再加入本身
                subss.Add(s);
            }

            return subss;
        }
        public double Prob(int time, IMDPState s1, IMDPState s2, IMDPDecision a)
        {
            if (s1.Equals(s2))
            {
                return 1 - suppRoute((a as Decision).OpenProductSet).Sum(p => Ro(time) * P(time, p, a as Decision));
            }
            else
            {
                //Find a route via which s1 transits to s2.
                Route r = suppRoute((a as Decision).OpenProductSet).
                    FirstOrDefault(i => (GenStateSpace(s1, a) as StateSpace).MinusOneUnit(s1, i).Equals(s2));
                if (r != null)
                {
                    return Ro(time) * P(time, r, a as Decision);
                }
                else
                {
                    return 0;
                }
            }
        }
        public double Reward(int t, IMDPState s, IMDPDecision a)
        {
            return suppRoute((a as Decision).OpenProductSet).
            Where(i => (s as State).CanSupportRoute(i)).
            Sum(i => Ro(t) * P(t, i, a as Decision) * f(i));
        }

        #endregion

        #region 实现IAFFDataAdater
        public IALPResourceSpace RS
        {
            get
            {
                return ResSpace;
            }
        }
        public double Rt(int t, IMDPDecision a)
        {
            return Ro(t) * suppRoute((a as Decision).OpenProductSet).
            Sum(i => P(t, i, a as Decision) * f(i));
        }
        public double Qti(int t, IALPResource re, IMDPDecision a)
        {
            return Ro(t) * suppRoute((a as Decision).OpenProductSet).
            Sum(i => P(t, i, a as Decision) * aik(re, i));
        }
        public IALPState CreateOrFind(IDictionary<IALPResource, int> RecDic)
        {
            State state = new State();
            foreach (Resource ts in this.ResSpace)
            {
                state.Add(ts, RecDic[ts]);//这里没有管安全性
            }
            IMDPState res = _ss.FirstOrDefault(i => i.Equals(state));
            if (res == null)
            {
                _ss.Add(state);
                return state;
            }
            else
            {
                return res as IALPState;
            }
        }
        public IALPState CreateOrFind(ResouceState RecDic)
        {
            State state = new State();
            foreach (Resource ts in this.ResSpace)
            {
                state.Add(ts, RecDic.GetRemainNum(ts));//这里没有管安全性
            }
            IMDPState res = _ss.FirstOrDefault(i => i.Equals(state));
            if (res == null)
            {
                _ss.Add(state);
                return state;
            }
            else
            {
                return res as IALPState;
            }
        }
        #endregion

        #region 内部函数
        private int aik(IALPResource i, Route r)
        {
            if (r.Exists(a => a.Contains(i as Resource))) return 1;
            else return 0;
        }
        private double f(Route p)
        {
            return p.Sum(i => i.Fare);
        }
        public double P(int time, Route r, Decision d)
        {
            double a = 0;
            foreach (MarketSegment ms in MarketInfo)
            {
                a += ms.Lamada(time) * Piks(ms, r, d.OpenProductSet);
            }
            return a;
        }

        //public double Piks(MarketSegment l, Route r, HashSet<Product> openset)
        //{
        //    double a = l.ConsiderationDic.ContainsKey(r) ? l.ConsiderationDic[r] : 0;
        //    double b = l.RouteList.Intersect(suppRoute(openset)).Sum(i => l.ConsiderationDic[i]);
        //    double c = l.Retreat;
        //    return a / (b + c);
        //}
        public double Piks(MarketSegment l, Route r, HashSet<IProduct> openset)
        {
            double a = l.ConsiderationDic.ContainsKey(r) ? l.ConsiderationDic[r] : 0;
            double b = l.RouteList.Intersect(suppRoute(openset)).Sum(i => l.ConsiderationDic[i]);
            double c = l.Retreat;
            return a / (b + c);
        }
        private double Ro(int time)
        {
            return MarketInfo.Ro(time);
        }
        #endregion

        #region 生成状态空间
        //public void GenDS()
        //{
        //    _ds.Clear();
        //    _ds.Add(new Decision());
        //    IMDPDecisionSpace tempSet = new DecisionSpace();
        //    for (int i = 0; i < ProSpace.Count; i++)
        //    {
        //        tempSet.Clear();
        //        foreach (Decision dic in _ds)
        //        {
        //            Decision temp = new Decision(dic);
        //            temp.OpenProductSet.Add(ProSpace[i]);
        //            tempSet.Add(temp);
        //        }
        //        _ds.UnionWith(tempSet);
        //    }
        //}
        //public void GenSS()
        //{
        //    _ss.Clear();
        //    IMDPState temp = GenZeroState();
        //    _ss.Add(temp);
        //    foreach (Resource ts in ResSpace)
        //    {
        //        GenSSOneDemension(ts);
        //    }
        //}
        //public void GenSS1()//依赖于InitialState
        //{
        //    _ss.Clear();
        //    State state = new State();
        //    foreach (Resource ts in (InitialState as State).Keys)
        //    {
        //        state.Add(ts, (InitialState as State)[ts]);
        //        //GenFirstSS(ts, (InitialState as State)[ts]);
        //    }
        //    //_ss.Add(GenZeroState());
        //    _ss.Add(state);
        //}
        //public void GenSS2(ResouceState RecDic)
        //{
        //    _ss.Clear();
        //    State state = new State();
        //    foreach (Resource ts in RecDic.Keys)
        //    {
        //        state.Add(ts, RecDic[ts]);
        //        //GenFirstSS(ts, RecDic[ts]);
        //    }
        //    //_ss.Add(GenZeroState());
        //    _ss.Add(state);
        //}
        public IMDPState GenInitialState(ResouceState s)
        {
            State state = new State();
            foreach (Resource ts in ResSpace)
            {
                state.Add(ts, s.GetRemainNum(ts));//修改正确
            }
            return state;
        }
        public IMDPState CreateOrFind(IMDPState _s)
        {
            IMDPState res = _ss.FirstOrDefault(s => s.Equals(_s));
            if (res == null)
            {
                _ss.Add(_s);
                return _s;
            }
            else
            {
                return res;
            }
        }
        private void GenFirstSS(Resource ts, int value)
        {
            State state = new State();
            foreach (Resource _ts in this.ResSpace)
            {
                if (_ts == ts)
                {
                    state.Add(_ts, value);
                }
                else
                {
                    state.Add(_ts, 0);
                }
            }
            _ss.Add(state);
        }
        //private void GenSSOneDemension(Resource ts)
        //{
        //    List<IMDPState> templist = new List<IMDPState>();
        //    State temp2;
        //    foreach (State temp in _ss)
        //    {
        //        temp2 = temp;
        //        for (; temp2[ts] < ts.Capacity;)
        //        {
        //            temp2 = (_ss as StateSpace).PlusOneUnit(temp2, ts);
        //            templist.Add(temp2);
        //        }
        //    }
        //    (_ss as StateSpace).AddRange(templist);
        //}
        private IMDPState GenZeroState()
        {
            State state = new State();
            foreach (Resource ts in this.ResSpace)
            {
                state.Add(ts, 0);
            }
            return state as IMDPState;
        }
        #endregion

        /// <summary>
        /// Select all routes
        /// </summary>
        /// <param name="proset"></param>
        /// <returns></returns>
        //private List<Route> suppRoute(HashSet<Product> proset)
        //{
        //    List<Route> res = new List<Route>();
        //    foreach (Route r in pathList)
        //    {
        //        if (r.All(i => proset.Contains(i)))
        //        {
        //            res.Add(r);
        //        }
        //    }
        //    return res;
        //}
        private List<Route> suppRoute(HashSet<IProduct> proset)
        {
            List<Route> res = new List<Route>();
            foreach (Route r in pathList)
            {
                if (r.All(i => proset.Contains(i)))
                {
                    res.Add(r);
                }
            }
            return res;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    pathList = null;
                    ProSpace = null;
                    MarketInfo = null;
                    ResSpace = null;
                    _ds = null;
                    _ss = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~DataAdapter() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
