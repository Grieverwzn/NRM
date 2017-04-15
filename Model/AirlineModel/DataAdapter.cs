using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRM.Common;
using System.Collections.Generic;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.AirlineModel
{
    public class DataAdapter : IALPFTMDP,IFTMDP
    {
        public List<Product> ProSpace { get; set; }
        public Market MarketInfo { get; set; }
        public IALPResourceSpace ResSpace { get; set; }

        #region 实现IDataAdapter
        private IMDPDecisionSpace _ds = new DecisionSpace();
        private IMDPStateSpace _ss = new StateSpace();

        public int TimeHorizon { get; set; }
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
            if((s as State).CanSupportDecision(a as Decision))
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
                return 1 - (a as Decision).OpenProductSet.Sum(p => Ro(time) * P(time, p, a as Decision));
            }
            else
            {
                IProduct p = (a as Decision).OpenProductSet.
                    FirstOrDefault(i => (GenStateSpace(s1, a) as StateSpace).MinusOneUnit(s1, i).Equals(s2));
                return Ro(time) * P(time, p, a as Decision);
            }
        }
        public double Reward(int t, IMDPState s, IMDPDecision a)
        {
            return (a as Decision).OpenProductSet.
            Where(i => (s as State).CanSupportProduct(i)).
            Sum(i => Ro(t) * P(t, i, a as Decision) * f(i));
        }
        public IMDPState InitialState { get; set; }
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
            return Ro(t) * (a as Decision).OpenProductSet.
            Sum(i => P(t, i, a as Decision) * f(i));
        }
        public double Qti(int t, IALPResource re, IMDPDecision a)
        {
            return Ro(t) * (a as Decision).OpenProductSet.
            Sum(i => P(t, i, a as Decision) * aij(re,i));
        }
        public IALPState CreateOrFind(IDictionary<IALPResource, int> RecDic)
        {
            State state = new State();
            foreach (Resource ts in this.ResSpace)
            {
                state.Add(ts, RecDic[ts]);//这里没有管安全性
            }
            IMDPState res = _ss.FirstOrDefault(i => i.Equals(state));
            if(res==null)
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
        private int aij(IALPResource i,IProduct j)
        {
            if (j.Contains(i)) return 1;
            else return 0;
        }
        private double f(IProduct p)
        {
            return p.Fare;
        }
        private double P(int time, IProduct p, Decision d)
        {
            double a = 0;
            foreach(MarketSegment ms in MarketInfo)
            {
                a += ms.Lamada(time) * Pijs(ms, p, d);
            }
            return a;
            //return MarketInfo.Sum(ms => ms.Lamada(time) * Pijs((ms as MarketSegment), p, d));
        }
        private double Pijs(MarketSegment l, IProduct p, Decision s)
        {
            double a = l.ConsiderationDic.ContainsKey(p) ? l.ConsiderationDic[p] : 0;
            double b = l.ProList.Intersect(s.OpenProductSet).Sum(i => l.ConsiderationDic[i]);
            double c = l.Retreat;
            return a / (b + c);
        }
        private double Ro(int time)
        {
            return MarketInfo.Ro(time);
        }
        #endregion

        #region 生成数据
        /*
        public void LoadData()
        {
            //Resource
            #region Resouce
            Resource Resource1 = new Resource() { ResID = 1, Description = "A->B", Capacity =3 };
            Resource Resource2 = new Resource() { ResID = 2, Description = "A->C", Capacity =2 };
            Resource Resource3 = new Resource() { ResID = 3, Description = "B->C", Capacity = 2 };
            _rs.Add(Resource1);
            _rs.Add(Resource2);
            _rs.Add(Resource3);
            #endregion
            //Product      
            #region Products     
            //Product pro0 = new Product() { ProID = 0, Description = "放弃出行" };
            Product pro1 = new Product() { ProID = 1, Description = "A->C:H", Fare = 1200 }; pro1.Add(Resource2);
            Product pro2 = new Product() { ProID = 2, Description = "A->B->C:H", Fare = 800 }; pro2.Add(Resource1); pro2.Add(Resource3);
            Product pro3 = new Product() { ProID = 3, Description = "A->B:H", Fare = 500 }; pro3.Add(Resource1);
            Product pro4 = new Product() { ProID = 4, Description = "B->C:H", Fare = 500 }; pro4.Add(Resource3);
            Product pro5 = new Product() { ProID = 5, Description = "A->C:L", Fare = 800 }; pro5.Add(Resource2);
            Product pro6 = new Product() { ProID = 6, Description = "A->B->C:L", Fare = 500 }; pro6.Add(Resource1); pro6.Add(Resource3);
            Product pro7 = new Product() { ProID = 7, Description = "A->B:L", Fare = 300 }; pro7.Add(Resource1);
            Product pro8 = new Product() { ProID = 8, Description = "B->C:L", Fare = 300 }; pro8.Add(Resource3);
            proSpace.Add(pro1);
            proSpace.Add(pro2);
            proSpace.Add(pro3);
            proSpace.Add(pro4);
            proSpace.Add(pro5);
            proSpace.Add(pro6);
            proSpace.Add(pro7);
            proSpace.Add(pro8);
            #endregion
            //Market    
            #region Market    
            MarketSegment MS1 = new MarketSegment()
            {
                MSID = 1,
                Description = "Price sensitive, Nonstop (A→C)",
                Lamada = x => { return 0.15+(x>=10?0.1:0.01*x); },
                Retreat = 5
            };
            MS1.ConsiderationDic.Add(pro1, 8); MS1.ConsiderationDic.Add(pro5, 2);
            MarketSegment MS2 = new MarketSegment()
            {
                MSID = 2,
                Description = "Price insensitive (A→C)",
                Lamada = x => { return 0.15+ (x >= 10 ? 0.1 : 0.01 * x); },
                Retreat = 10
            };
            MS2.ConsiderationDic.Add(pro1, 6); MS2.ConsiderationDic.Add(pro2, 5);
            MarketSegment MS3 = new MarketSegment()
            {
                MSID = 3,
                Description = "Price sensitive (A→C)",
                Lamada = x => { return 0.2; },
                Retreat = 8
            };
            MS3.ConsiderationDic.Add(pro5, 5); MS3.ConsiderationDic.Add(pro6, 2);
            MarketSegment MS4 = new MarketSegment()
            {
                MSID = 4,
                Description = "Price sensitive (A→B)",
                Lamada = x => { return 0.25- (x >= 10 ? 0.1 : 0.01 * x); },
                Retreat = 4
            };
            MS4.ConsiderationDic.Add(pro3, 8);
            MS4.ConsiderationDic.Add(pro7, 2);
            MarketSegment MS5 = new MarketSegment()
            {
                MSID = 5,
                Description = "Price sensitive (B→C)",
                Lamada = x => { return 0.25- (x >= 10 ? 0.1 : 0.01 * x); },
                Retreat = 6
            };
            MS5.ConsiderationDic.Add(pro4, 8); MS5.ConsiderationDic.Add(pro8, 2);
            market.Add(MS1); market.Add(MS2); market.Add(MS3);
            market.Add(MS4);
            market.Add(MS5);
            #endregion         
        }
        */

        public void GenDS()
        {
            _ds.Add(new Decision());
            IMDPDecisionSpace tempSet = new DecisionSpace();
            for (int i = 0; i < ProSpace.Count; i++)
            {
                tempSet.Clear();
                foreach (Decision dic in _ds)
                {
                    Decision temp = new Decision(dic);
                    temp.OpenProductSet.Add(ProSpace[i]);
                    tempSet.Add(temp);
                }
                _ds.UnionWith(tempSet);
            }
        }
        public void GenSS()
        {
            IMDPState temp = GenZeroState();
            _ss.Add(temp);
            foreach (Resource ts in ResSpace)
            {
                GenSSOneDemension(ts);
            }
        }
        public void GenSS1()
        {
            State state = new State();
            foreach (Resource ts in ResSpace)
            {
                state.Add(ts, ts.Capacity);
                //GenFirstSS(ts);
            }
            //_ss.Add(GenZeroState());
            _ss.Add(state);
        }
        public IMDPState GenInitialState()
        {
            State state = new State();
            foreach (Resource ts in ResSpace)
            {
                state.Add(ts, ts.Capacity);
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
        private void GenFirstSS(Resource ts)
        {
            State state = new State();
            foreach (Resource _ts in this.ResSpace)
            {
                if (_ts == ts)
                {
                    state.Add(_ts, ts.Capacity);
                }
                else
                {
                    state.Add(_ts, 0);
                }
            }
            _ss.Add(state);
        }
        private void GenSSOneDemension(Resource ts)
        {
            List<IMDPState> templist = new List<IMDPState>();
            State temp2;
            foreach (State temp in _ss)
            {
                temp2 = temp;
                for (; temp2[ts] < ts.Capacity;)
                {
                    temp2 = (_ss as StateSpace).PlusOneUnit(temp2, ts);
                    templist.Add(temp2);
                }
            }
            (_ss as StateSpace).AddRange(templist);
        }
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

        public string ProOfProduct(int time)
        {
            string s = time + "\t";
            double a = 0;
            foreach (Product ms in ProSpace)
            {
                s += P(time,ms, (Decision)_ds.Last()) + "\t";
                a += P(time, ms, (Decision)_ds.Last());
            }
            s += a;
            return s;
        }
    }
}
