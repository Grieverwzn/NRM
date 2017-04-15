using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRMSolver
{

    /// <summary>
    /// Dynamic disaggregation method
    /// </summary>
    public abstract class DD_Solver : NRM_Solver
    {
        #region variables
        public Cplex RMPModel = new Cplex();//主问题（LP问题）

        //变量
        protected Dictionary<int, INumVar[]> DisVar1;//非压缩变量
        protected Dictionary<int, INumVar> DisVar2;
        protected Dictionary<int, INumVar[]> AggVar1;
        protected Dictionary<int, INumVar> AggVar2;
        protected INumVar[] CenterVar1;//中间变量
        protected INumVar CenterVar2;
        //值
        protected Dictionary<int, double[]> DisV;
        protected Dictionary<int, double> DisSita;
        protected Dictionary<int, double[]> AggV;
        protected Dictionary<int, double> AggSita;
        protected double[] CenV;
        protected double CenSita;
        //约束集合
        protected Dictionary<int, Dictionary<IALPDecision, IRange>> DisConstraints;
        protected Dictionary<int, Dictionary<IALPDecision, IRange>> AggConstraints;
        protected IRange[] AggCenterRange1;
        protected IRange AggCenterRange2;
        protected IRange[] DisCenterRange1;
        protected IRange DisCenterRange2;
        #endregion

        #region Attributes
        //收敛参数
        public int alpha = 0;//α值
        public int step = 1;//搜索步长
        //停止参数
        private double m_Tolerance = 1e-2;
        private double threshold = 1e-3;
        private int obeyTime = 10;

        public double Tolerance
        {
            get
            {
                return m_Tolerance;
            }

            set
            {
                m_Tolerance = value;
            }
        }
        protected double Threshold//改进容许值
        {
            get
            {
                return threshold;
            }

            set
            {
                threshold = value;
            }
        }
        protected int ObeyTime//允许在容许值外的次数
        {
            get
            {
                return obeyTime;
            }

            set
            {
                obeyTime = value;
            }
        }
        #endregion

        public void Solve()//求解问题的基本框架
        {
            Init();
            DoCalculate();

            while (!TestValidation())
            {
                ClearUnboundedConstraints();
                StepForward();
                DoCalculate();
            }
        }

        public abstract void Init();//初始化问题
        public abstract void StepForward();//迭代一次
        public abstract void DoCalculate();//计算一次
        public abstract bool TestValidation();//最优条件判断

        protected abstract void Add_Agg_Constraint(int i, IALPDecision a);
        protected abstract void Add_Dis_Constraint(int t, IALPDecision a);

        protected abstract void UpdateValues();
        protected abstract void UpdateBidPrice();

        public void ClearUnboundedConstraints()//清除没有bounded的约束
        {
            Dictionary<int, Dictionary<IALPDecision, IRange>> tempAgg = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            Dictionary<int, Dictionary<IALPDecision, IRange>> tempDis = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            foreach (var a in AggConstraints)
            {
                if (a.Value.Count() < Data.RS.Count * 1.2) continue;
                tempAgg.Add(a.Key, new Dictionary<IALPDecision, IRange>());
                foreach (IALPDecision d in a.Value.Keys)
                {
                    if (RMPModel.GetSlack(a.Value[d]) > 0)
                    {
                        tempAgg[a.Key].Add(d, a.Value[d]);
                    }
                }
            }
        }

        //protected abstract void InitRMPModel();
        ////{
        ////    #region //////////////初始化变量//////////////
        ////    InitVariables();
        ////    for (int i = alpha; i <= Data.TimeHorizon - 1; i++)
        ////    {
        ////        Add_Dis_Vars(i);
        ////    }
        ////    #endregion

        ////    #region //////////////生成目标//////////////
        ////    INumExpr exp5 = RMPModel.NumExpr();
        ////    exp5 = RMPModel.Sum(exp5, AggVar2[0]);
        ////    foreach (IALPResource re in Data.RS)
        ////    {
        ////        exp5 = RMPModel.Sum(exp5, RMPModel.Prod((Data.InitialState as IALPState)[re], AggVar1[0][Data.RS.IndexOf(re)]));
        ////    }
        ////    IObjective cost = RMPModel.AddMinimize(exp5);
        ////    #endregion

        ////    #region //////////////生成基本约束//////////////
        ////    for (int t = alpha + 1; t < Data.TimeHorizon; t++)
        ////    {
        ////        if (t < Data.TimeHorizon - 1)
        ////        {
        ////            Add_Dis_Squ_Constraint(t);
        ////        }
        ////    }
        ////    #endregion

        ////    RMPModel.SetOut(null);
        ////}

        //protected void Add_Dis_Squ_Constraint(int t)
        //{
        //    foreach (IALPResource re in Data.RS)
        //    {
        //        INumExpr exp2 = RMPModel.Sum(DisVar1[t][Data.RS.IndexOf(re)], RMPModel.Prod(-1, DisVar1[t + 1][Data.RS.IndexOf(re)]));
        //        RMPModel.AddGe(exp2, 0);
        //    }
        //    INumExpr exp3 = RMPModel.Sum(DisVar2[t], RMPModel.Prod(-1, DisVar2[t + 1]));
        //    RMPModel.AddGe(exp3, 0);
        //}
        //protected void Add_Dis_Vars(int i)
        //{
        //    DisConstraints.Add(i, new Dictionary<IALPDecision, IRange>());
        //    DisVar1.Add(i, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
        //    DisVar2.Add(i, RMPModel.NumVar(0, double.MaxValue));
        //    DisV.Add(i, new double[Data.RS.Count]);
        //    DisSita.Add(i, 0);
        //}



        //protected virtual bool Dis_CG(int t, out IALPDecision deci_a)
        //{
        //    deci_a = null;
        //    double temp = 0;
        //    foreach (IALPDecision d in Data.DS)
        //    {
        //        double temp1 = 0;
        //        if (t < Data.TimeHorizon - 1)
        //        {
        //            temp1 += DisSita[t] - DisSita[t + 1];
        //            foreach (IALPResource re in Data.RS)
        //            {
        //                if (d.UseResource(re))
        //                {
        //                    temp1 += DisV[t][Data.RS.IndexOf(re)] +
        //                       DisV[t + 1][Data.RS.IndexOf(re)] * (Data.Qti(t, re, d) - 1);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            temp1 += DisSita[t];
        //            foreach (IALPResource re in Data.RS)
        //            {
        //                if (d.UseResource(re))
        //                {
        //                    temp1 += DisV[t][Data.RS.IndexOf(re)];
        //                }
        //            }
        //        }
        //        temp1 = Data.Rt(t, d) - temp1;
        //        if (temp1 > temp)
        //        {
        //            deci_a = d;
        //            temp = temp1;
        //        }
        //    }
        //    if (temp <= Tolerance || DisConstraints[t].ContainsKey(deci_a))
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}


        //    for (int t = alpha + 1; t < Data.TimeHorizon; t++)
        //    {
        //        if (DisConstraints[t].Count() < Data.RS.Count * 1.2) continue;
        //        tempDis.Add(t, new Dictionary<IALPDecision, IRange>());
        //        foreach (IALPDecision d in DisConstraints[t].Keys)
        //        {
        //            if (RMPModel.GetSlack(DisConstraints[t][d]) > 0)
        //            {
        //                tempDis[t].Add(d, DisConstraints[t][d]);
        //            }
        //        }
        //    }
        //    foreach (var pair in tempAgg)
        //    {
        //        foreach (var child in tempAgg[pair.Key])
        //        {
        //            RMPModel.Remove(child.Value);
        //            AggConstraints[pair.Key].Remove(child.Key);
        //        }
        //    }
        //    foreach (var pair in tempDis)
        //    {
        //        foreach (var child in tempDis[pair.Key])
        //        {
        //            RMPModel.Remove(child.Value);
        //            DisConstraints[pair.Key].Remove(child.Key);
        //        }
        //    }
        //}
        //public virtual bool TestValidation()
        //{
        //    bool Val = true;
        //    //double[][] R = Rotate(BidPrice);
        //    int[] tp = findturnningpoint();
        //    foreach (IALPResource re in Data.RS)
        //    {
        //        if (tp[Data.RS.IndexOf(re)] > alpha + 1)
        //        {
        //            print("资源{0}的拐点是: {1}", Data.RS.IndexOf(re), tp[Data.RS.IndexOf(re)]);
        //        }
        //        else
        //        {
        //            Val = false;
        //            print("资源{0}无有效拐点", Data.RS.IndexOf(re), tp[Data.RS.IndexOf(re)]);
        //        }
        //    }
        //    return Val;
        //}

    }
}

