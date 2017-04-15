using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    public static class MDPALPModelBuilder
    {
        //Piece-Wise Function Approximation
        public static Cplex Build_Dual_PW_Model(IALPFTMDP aff)
        {
            Cplex model = new Cplex();
            IObjective cost = model.AddMaximize();

            #region //////////////生成约束//////////////
            IRange[][][] constraint1 = new IRange[aff.TimeHorizon][][];
            IRange[] constraint2 = new IRange[aff.TimeHorizon];
            for (int i = 0; i < aff.TimeHorizon; i++)
            {
                constraint1[i] = new IRange[aff.RS.Count][];
                foreach (IALPResource re in aff.RS)
                {
                    constraint1[i][aff.RS.IndexOf(re)] = new IRange[(aff.InitialState as IALPState)[re]];
                    for (int k = 1; k < (aff.InitialState as IALPState)[re]; k++)
                    {
                        if (i == 0)
                        {
                            constraint1[i][aff.RS.IndexOf(re)][k - 1] = model.AddRange(1, 1);
                        }
                        else
                        {
                            constraint1[i][aff.RS.IndexOf(re)][k - 1] = model.AddRange(0, 0);
                        }
                    }
                }
                constraint2[i] = model.AddRange(1, 1);
            }
            #endregion

            #region //////////////生成变量//////////////
            //System.Threading.Tasks.Parallel.For(0, aff.TimeHorizon, (t) =>
            for (int t = 0; t < aff.TimeHorizon; t++)
            {
                foreach (IALPState s in aff.SS)
                {
                    foreach (IMDPDecision a in aff.GenDecisionSpace(s))
                    {
                        //目标函数
                        Column col = model.Column(cost, aff.Reward(t, s, a));
                        // System.Console.WriteLine(aff.Reward(t, s, a));
                        //第一类约束
                        foreach (IALPResource re in aff.RS)
                        {
                            for (int k = 1; k < (aff.InitialState as IALPState)[re]; k++)
                            {
                                if (s[re] >= k)
                                {
                                    col = col.And(model.Column(constraint1[t][aff.RS.IndexOf(re)][k - 1], 1));
                                    if (t < aff.TimeHorizon - 1)
                                    {
                                        col = col.And(model.Column(constraint1[t + 1][aff.RS.IndexOf(re)][k - 1], -1));
                                        if (s[re] == k)
                                        {
                                            col = col.And(model.Column(constraint1[t + 1][aff.RS.IndexOf(re)][k - 1], (aff.Qti(t, re, a))));
                                        }
                                    }
                                }
                            }
                        }
                        //第二类约束
                        col = col.And(model.Column(constraint2[t], 1));
                        model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                    }
                }
            }
            ///);
            #endregion

            model.SetOut(null);
            return model;
        }

        //Affine Funciton Approximation
        public static Cplex Build_CD1_Model(IALPFTMDP aff)
        {
            Cplex model = new Cplex();

            IObjective cost = model.AddMaximize();
            IRange[][] constraint1 = new IRange[aff.TimeHorizon][];
            IRange[] constraint2 = new IRange[aff.TimeHorizon];

            #region //////////////生成约束//////////////
            for (int i = 0; i < aff.TimeHorizon; i++)
            {
                constraint1[i] = new IRange[aff.RS.Count];
                foreach (IALPResource re in aff.RS)
                {
                    if (i == 0)
                    {
                        constraint1[i][aff.RS.IndexOf(re)] = model.AddRange((aff.InitialState as IALPState)[re], (aff.InitialState as IALPState)[re]);
                    }
                    else
                    {
                        constraint1[i][aff.RS.IndexOf(re)] = model.AddRange(0, 0);
                    }
                }
                constraint2[i] = model.AddRange(1, 1);
            }
            #endregion

            #region //////////////生成变量//////////////
            for (int t = 0; t < aff.TimeHorizon; t++)
            {
                foreach (IALPState s in aff.SS)
                {
                    foreach (IMDPDecision a in aff.GenDecisionSpace(s))
                    {
                        Column col = model.Column(cost, aff.Rt(t, a));
                        foreach (IALPResource re in aff.RS)
                        {
                            col = col.And(model.Column(constraint1[t][aff.RS.IndexOf(re)], (s as IALPState)[re]));
                            if (t < aff.TimeHorizon - 1)
                            {
                                col = col.And(model.Column(constraint1[t + 1][aff.RS.IndexOf(re)], (aff.Qti(t, re, a)) - (s as IALPState)[re]));
                            }
                        }
                        col = col.And(model.Column(constraint2[t], 1));
                        model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                    }
                }
            }
            #endregion
            return model;
        }
        public static Cplex Build_CD2_Model(IALPFTMDP aff)
        {
            Cplex model = new Cplex();
            IObjective cost = model.AddMaximize();

            #region //////////////生成约束//////////////
            IRange[][] constraint1 = new IRange[aff.TimeHorizon][];
            IRange[][] constraint2 = new IRange[aff.TimeHorizon][];
            IRange[] constraint3 = new IRange[aff.TimeHorizon];
            for (int i = 0; i < aff.TimeHorizon; i++)
            {
                constraint1[i] = new IRange[aff.RS.Count];
                constraint2[i] = new IRange[aff.RS.Count];
                foreach (IALPResource re in aff.RS)
                {
                    if (i == 0)
                    {
                        constraint1[i][aff.RS.IndexOf(re)] = model.AddRange((aff.InitialState as IALPState)[re], (aff.InitialState as IALPState)[re]);
                        constraint2[i][aff.RS.IndexOf(re)] = model.AddRange(double.MinValue, 0);
                    }
                    else
                    {
                        constraint1[i][aff.RS.IndexOf(re)] = model.AddRange(0, 0);
                        constraint2[i][aff.RS.IndexOf(re)] = model.AddRange(double.MinValue, 0);
                    }
                }
                constraint3[i] = model.AddRange(1, 1);
            }
            #endregion

            #region //////////////生成变量//////////////
            //生成h
            for (int t = 0; t < aff.TimeHorizon; t++)
            {
                foreach (IALPDecision a in aff.DS)
                {
                    //目标函数
                    Column col = model.Column(cost, aff.Rt(t, a));
                    foreach (IALPResource re in aff.RS)
                    {
                        //第一类约束
                        if (t < aff.TimeHorizon - 1)
                        {
                            col = col.And(model.Column(constraint1[t + 1][aff.RS.IndexOf(re)], aff.Qti(t + 1, re, a)));
                        }
                        //第二类约束
                        if (a.UseResource(re))
                        {
                            col = col.And(model.Column(constraint2[t][aff.RS.IndexOf(re)], 1));
                        }
                    }
                    //第三类约束
                    col = col.And(model.Column(constraint3[t], 1));
                    model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                }
                foreach (IALPResource r in aff.RS)
                {
                    Column col = model.Column(cost, 0);// cost, aff.Rt(t, a));
                    if (t < aff.TimeHorizon - 1)
                    {
                        col = col.And(model.Column(constraint1[t + 1][aff.RS.IndexOf(r)], -1));
                    }
                    col = col.And(model.Column(constraint1[t][aff.RS.IndexOf(r)], 1));
                    col = col.And(model.Column(constraint2[t][aff.RS.IndexOf(r)], -1));
                    model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                }
            }
            ///);
            #endregion

            return model;
        }
        public static Cplex Build_CD3_Model(IALPFTMDP aff)
        {
            Cplex model = new Cplex();
            IObjective cost = model.AddMaximize();
            INumVar[][] var = new INumVar[aff.TimeHorizon][];

            #region //////////////生成约束//////////////
            IRange[][] constraint1 = new IRange[aff.TimeHorizon][];
            IRange[] constraint2 = new IRange[aff.TimeHorizon];
            for (int i = 0; i < aff.TimeHorizon; i++)
            {
                var[i] = new INumVar[aff.DS.Count];
                constraint1[i] = new IRange[aff.RS.Count];
                foreach (IALPResource re in aff.RS)
                {
                    constraint1[i][aff.RS.IndexOf(re)] = model.AddRange(double.MinValue, (aff.InitialState as IALPState)[re]);
                }
                constraint2[i] = model.AddRange(1, 1);
            }
            #endregion

            #region //////////////生成变量//////////////
            for (int t = 0; t < aff.TimeHorizon; t++)
            {
                foreach (IALPDecision a in aff.DS)
                {
                    //目标函数
                    Column col = model.Column(cost, aff.Rt(t, a));
                    //第一类约束
                    foreach (IALPResource re in aff.RS)
                    {
                        for (int k = t + 1; k < aff.TimeHorizon; k++)
                        {
                            col = col.And(model.Column(constraint1[k][aff.RS.IndexOf(re)], aff.Qti(t, re, a)));
                        }
                        if (a.UseResource(re))
                        {
                            col = col.And(model.Column(constraint1[t][aff.RS.IndexOf(re)], 1));
                        }
                    }
                    //第二类约束
                    col = col.And(model.Column(constraint2[t], 1));
                    var[t][aff.DS.IndexOf(a)] = model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                }
            }
            #endregion

            return model;
        }
        public static Cplex Build_CLP1_Model(IALPFTMDP aff)
        {
            Cplex model = new Cplex();

            INumVar[][] Var1 = new INumVar[aff.TimeHorizon][];
            INumVar[] Var2 = model.NumVarArray(aff.TimeHorizon, 0, double.MaxValue);

            #region //////////////生成变量//////////////
            for (int i = 0; i < aff.TimeHorizon; i++)
            {
                Var1[i] = model.NumVarArray(aff.RS.Count, 0, double.MaxValue);// new INumVar[aff.DS.Count];
            }
            #endregion

            #region //////////////生成约束//////////////
            for (int t = 0; t < aff.TimeHorizon; t++)
            {
                foreach (IALPDecision a in aff.DS)
                {
                    INumExpr exp1 = model.NumExpr();
                    if (t < aff.TimeHorizon - 1)
                    {
                        exp1 = model.Sum(Var2[t], model.Prod(-1, Var2[t + 1]));
                        foreach (IALPResource re in aff.RS)
                        {
                            if (a.UseResource(re))
                            {
                                exp1 = model.Sum(exp1, Var1[t][aff.RS.IndexOf(re)], model.Prod(aff.Qti(t, re, a) - 1, Var1[t + 1][aff.RS.IndexOf(re)]));
                            }
                        }
                    }
                    else
                    {
                        exp1 = model.Sum(exp1, Var2[t]);
                        foreach (IALPResource re in aff.RS)
                        {
                            if (a.UseResource(re))
                            {
                                exp1 = model.Sum(exp1, Var1[t][aff.RS.IndexOf(re)]);
                            }
                        }
                    }
                    model.AddGe(exp1, aff.Rt(t, a));
                }
                if (t < aff.TimeHorizon - 1)
                {
                    foreach (IALPResource re in aff.RS)
                    {
                        INumExpr exp2 = model.NumExpr();
                        exp2 = model.Sum(Var1[t][aff.RS.IndexOf(re)], model.Prod(-1, Var1[t + 1][aff.RS.IndexOf(re)]));
                        model.AddGe(exp2, 0);
                    }
                    INumExpr exp3 = model.NumExpr();
                    exp3 = model.Sum(exp3, Var2[t], model.Prod(-1, Var2[t + 1]));
                    model.AddGe(exp3, 0);
                }
            }
            #endregion

            #region //////////////生成目标//////////////
            INumExpr exp5 = model.NumExpr();
            exp5 = model.Sum(exp5, Var2[0]);
            foreach (IALPResource re in aff.RS)
            {
                exp5 = model.Sum(exp5, model.Prod((aff.InitialState as IALPState)[re], Var1[0][aff.RS.IndexOf(re)]));
            }
            IObjective cost = model.AddMinimize(exp5);
            #endregion

            return model;
        }
        /// <summary>
        /// 求解load factor
        /// </summary>
        public class S_StarSolver
        {
            IALPFTMDP aff;// { get; set; }
            Cplex model = new Cplex();
            IObjective cost;
            IRange constraint;
            INumVar[] vars;

            public S_StarSolver()
            {
            }
            public S_StarSolver(IALPFTMDP _aff)
            {
                aff = _aff;
            }

            public void SetData(IALPFTMDP _aff)
            {
                aff = _aff;
            }

            private void InitModel()
            {
                constraint = model.AddRange(1, 1);
                vars = new INumVar[aff.DS.Count];
                model.SetOut(null);
                //P = new IALPDecision[aff.TimeHorizon];
            }
            private void SetTime(int t)
            {
                model.Remove(model.GetObjective());
                cost = model.AddMaximize();

                foreach (IALPDecision a in aff.DS)
                {
                    Column col = model.Column(cost, aff.Rt(t, a));

                    col = col.And(model.Column(constraint, 1));

                    vars[aff.DS.IndexOf(a)] = model.NumVar(col, 0, 1, NumVarType.Int);
                }

            }

            public double DoWork()
            {
                model.ClearModel();
                InitModel();
                double a = 0;
                for (int t = 0; t < aff.TimeHorizon; t++)
                {
                    SetTime(t);
                    if (model.Solve())
                    {
                        for (int i = 0; i < aff.DS.Count; i++)
                        {
                            if (model.GetValue(vars[i]) == 1)
                            {
                                a += aff.RS.Sum(r => aff.Qti(t, r, aff.DS[i]));
                            }
                        }
                    }
                }
                double b = 0;
                foreach (IALPResource r in aff.RS)
                {
                    b += 1;//b += r.Capacity;
                }
                return a / b;
            }
        }

    }
}