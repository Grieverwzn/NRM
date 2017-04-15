using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using System;
using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    public abstract class GCDLP_Solver : NRM_Solver
    {
        #region variables
        public Cplex RMPModel = new Cplex();//主问题（LP问题）

        //变量
        protected Dictionary<int, INumVar[]> AggVar1;
        protected Dictionary<int, INumVar> AggVar2;
        //值
        protected Dictionary<int, double[]> AggV;
        protected Dictionary<int, double> AggSita;
        //约束集合
        protected Dictionary<int, Dictionary<IALPDecision, IRange>> AggConstraints;
        #endregion

        #region Attributes
        //停止参数
        private double m_Tolerance = 1e-2;
        private double threshold = 1e-3;
        private int obeyTime = 5;

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
        }

        public abstract void Init();//初始化问题
        public abstract void StepForward();//迭代一次
        public abstract void DoCalculate();//计算一次
        public abstract bool TestValidation();//最优条件判断

        protected abstract void Add_Agg_Constraint(int i, IALPDecision a);
        //protected abstract void Add_Dis_Constraint(int t, IALPDecision a);
      
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
    }
}

