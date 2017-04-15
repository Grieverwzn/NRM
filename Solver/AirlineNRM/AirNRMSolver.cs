using com.foxmail.wyyuan1991.NRM.AirlineModel;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRMSolver;
using ILOG.Concert;
using System.Collections.Generic;
using System.Linq;

namespace AirlineNRM
{
    public class AirlineNRMSolver_CLP : CLP1_Solver
    {
        private DataAdapter _Data;
        public DataAdapter Data
        {
            get
            {
                return _Data;
            }
            set
            {
                base.Data = value as IALPFTMDP;
                _Data = value;
            }
        }

        protected override void CreateFeasibleSolution()
        {

        }
        DecisionSpace _ds = new DecisionSpace();
        protected override bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;
            List<Product> list = Data.ProSpace.Where(i =>
            {
                double w = bidprice(t, i);
                if (i.Fare < w)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }).ToList();
            List<Product> templist = new List<Product>();
            //加入第一个元素
            Product tempProduct = null;
            double temp = computeValue(t, templist, tempProduct);
            foreach (Product p in list)
            {
                if (temp < computeValue(t, templist, p))
                {
                    temp = computeValue(t, templist, p);
                    tempProduct = p;
                }
            }
            if (tempProduct != null)
            {
                templist.Add(tempProduct);
                list.Remove(tempProduct);
            }
            //贪婪方法加入其他元素        
            for (int length = 0; length < templist.Count();)
            {
                length = templist.Count();
                foreach (Product p in list)
                {
                    double temp1 = computeValue(t, templist, p);
                    if (temp < temp1)
                    {
                        temp = temp1;
                        templist.Add(p);
                    }
                }
            }

            if (computeValue(t, templist, null) < Tolerance)
            {
                return true;
            }
            else
            {
                //从u生成decision
                Decision d = new Decision();
                d.OpenProductSet.UnionWith(templist);
                deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
                if (deci_a == null) { _ds.Add(d); deci_a = d; }
                return false;
            }
        }

        private double bidprice(int t, Product i)
        {
            double w = 0;
            foreach (Resource r in Data.RS)
            {
                if (i.Contains(r))
                {
                    w += V[t][Data.RS.IndexOf(r)];
                }
            }

            return w;
        }
        private double computeValue(int t, List<Product> list, Product tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Product p in l.ConsiderationDic.Keys)
                {
                    if (list.Contains(p) || p.Equals(tp))
                    {
                        a += l.ConsiderationDic[p] * (p.Fare - bidprice(t, p));
                        b += l.ConsiderationDic[p];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * a / b;
            }
            if (t < Data.TimeHorizon - 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp!=null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)] - V[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t] - Sita[t + 1];
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t];
            }
            return v;
        }
    }
    public class AirlineNRMSolver_CLP_Alpha : CLP1_Alpha_Solver
    {
        private DataAdapter _Data;
        public new DataAdapter Data
        {
            get
            {
                return _Data;
            }
            set
            {
                base.Data = value as IALPFTMDP;
                _Data = value;
            }
        }

        DecisionSpace _ds = new DecisionSpace();
        protected override void CreateFeasibleSolution()
        {
            Decision d = new Decision();
            _ds.Add(d);
            AddConstraint1(d);
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            {
                AddConstraint2(t, d);
            }
        }
        public override void StepFoward()
        {
            alpha -= step;
            //TODO:Delete Agg constraint(s)
            foreach (var a in Aggconstraints)
            {
                RMPModel.Remove(a.Value);
            }
            List<IALPDecision> list = Aggconstraints.Keys.ToList();
            Aggconstraints.Clear();

            //TODO:Add DisAgg variables and constraint(s)
            for (int i = alpha + 1; i <= alpha + step; i++)
            {
                constraints.Add(i, new Dictionary<IALPDecision, IRange>());
                Var1.Add(i, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
                Var2.Add(i, RMPModel.NumVar(0, double.MaxValue));
                V.Add(i, new double[Data.RS.Count]);
                Sita.Add(i, 0);
            }

            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                if (t < Data.TimeHorizon - 1)
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        INumExpr exp2 = RMPModel.Sum(Var1[t][Data.RS.IndexOf(re)], RMPModel.Prod(-1, Var1[t + 1][Data.RS.IndexOf(re)]));
                        RMPModel.AddGe(exp2, 0);
                    }
                    INumExpr exp3 = RMPModel.Sum(Var2[t], RMPModel.Prod(-1, Var2[t + 1]));
                    RMPModel.AddGe(exp3, 0);
                }
            }
            foreach (IALPResource re in Data.RS)
            {
                INumExpr exp6 = RMPModel.NumExpr();
                exp6 = RMPModel.Sum(AggVar1[Data.RS.IndexOf(re)], RMPModel.Prod(-1, Var1[alpha + 1][Data.RS.IndexOf(re)]));
                RMPModel.AddGe(exp6, 0);
            }

            INumExpr exp4 = RMPModel.NumExpr();
            exp4 = RMPModel.Sum(exp4, AggVar2, RMPModel.Prod(-1, Var2[alpha + 1]));
            RMPModel.AddGe(exp4, 0);

            foreach (IALPDecision de in list)
            {
                AddConstraint1(de);
            }
            Decision d = _ds[0] as Decision;            
            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                AddConstraint2(t, d);
            }
        }

        private bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;
            List<Product> list = Data.ProSpace.Where(i =>
            {
                double w = bidprice(t, i);
                if (i.Fare < w)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }).ToList();
            List<Product> templist = new List<Product>();
            //加入第一个元素
            Product tempProduct = null;
            double temp = computeValue(t, templist, tempProduct);
            foreach (Product p in list)
            {
                if (temp < computeValue(t, templist, p))
                {
                    temp = computeValue(t, templist, p);
                    tempProduct = p;
                }
            }
            if (tempProduct != null)
            {
                templist.Add(tempProduct);
                list.Remove(tempProduct);
            }
            //贪婪方法加入其他元素        
            for (int length = 0; length< templist.Count();)
            {
                length = templist.Count();
                foreach (Product p in list)
                {
                    double temp1 = computeValue(t, templist, p);
                    if (temp < temp1)
                    {
                        temp = temp1;
                        templist.Add(p);
                    }
                }
            }

            //从u生成decision
            Decision d = new Decision();
            d.OpenProductSet.UnionWith(templist);
            deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
            if (deci_a == null) { _ds.Add(d); deci_a = d; }

            if (computeValue(t, templist, null) < Tolerance ||
                (t > alpha ? constraints[t].ContainsKey(deci_a) :
                Aggconstraints.ContainsKey(deci_a)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        protected override bool CG1(out IALPDecision deci_a)
        {
            return CG(alpha, out deci_a);
        }
        protected override bool CG2(int t,out IALPDecision deci_a)
        {
            return CG(t,out deci_a);
        }

        private double bidprice(int t, Product i)
        {
            double w = 0;
            if (t >= alpha + 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (i.Contains(r))
                    {
                        w += V[t][Data.RS.IndexOf(r)];
                    }
                }
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (i.Contains(r))
                    {
                        w += AggV[Data.RS.IndexOf(r)];
                    }
                }
            }

            return w;
        }
        private double computeValue(int t, List<Product> list, Product tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Product p in l.ConsiderationDic.Keys)
                {
                    if (list.Contains(p) || p.Equals(tp))
                    {
                        a += l.ConsiderationDic[p] * (p.Fare - bidprice(t, p));
                        b += l.ConsiderationDic[p];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * a / b;
            }
        
            if (t <= alpha)
            {
                v = v * (1 + alpha);
                v -= AggSita - Sita[alpha + 1];
            }
            else if (t > alpha && t < Data.TimeHorizon - 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)] - V[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t] - Sita[t + 1];
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t];
            }
            return v;
        }
    }
}

