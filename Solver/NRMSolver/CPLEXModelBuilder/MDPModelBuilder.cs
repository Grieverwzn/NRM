using ILOG.Concert;
using ILOG.CPLEX;

namespace com.foxmail.wyyuan1991.MDP
{
    public static class MDPModelBuilder
    {
        public static Cplex BuildLPModel(IFTMDP Ida)
        {
            Cplex model = new Cplex();
            INumVar[][] v = new INumVar[Ida.TimeHorizon][];   //V(t,x)
            for (int t = 0; t < Ida.TimeHorizon; t++)
            {
                v[t] = model.NumVarArray(Ida.SS.Count, -double.MaxValue, double.MaxValue);
            }

            IObjective RevenueUB = model.AddMinimize(model.Prod(1, v[0][Ida.SS.IndexOf(Ida.InitialState)]));

            //time->State->Active-> Expression
            for (int t = 0; t < Ida.TimeHorizon; t++)
            {
                foreach (IMDPState s in Ida.SS)
                {
                    foreach (IMDPDecision a in Ida.GenDecisionSpace(s))
                    {
                        INumExpr expr = v[t][Ida.SS.IndexOf(s)];
                        if (t < Ida.TimeHorizon - 1)
                        {
                            foreach (IMDPState k in Ida.GenStateSpace(s, a))
                            {
                                expr = model.Sum(expr,
                                    model.Prod(-Ida.Prob(t, s, k, a), v[t + 1][Ida.SS.IndexOf(k)]));
                            }
                        }
                        model.AddGe(expr, Ida.Reward(t, s, a));
                    }
                }
            }
            //model.SetOut(null);
            return model;
        }
        public static Cplex BuildDualModel(IFTMDP Ida)
        {
            Cplex model = new Cplex();

            IObjective cost = model.AddMaximize();
            IRange[][] constraint = new IRange[Ida.TimeHorizon][];

            #region //////////////生成约束//////////////
            for (int i = 0; i < Ida.TimeHorizon; i++)
            {
                constraint[i] = new IRange[Ida.SS.Count];
                foreach (IMDPState s in Ida.SS)
                {
                    if (i == 0 && s.Equals(Ida.InitialState))
                    {
                        constraint[i][Ida.SS.IndexOf(s)] = model.AddRange(1, 1);
                    }
                    else
                    {
                        constraint[i][Ida.SS.IndexOf(s)] = model.AddRange(0, 0);
                    }
                }
            }
            #endregion

            #region //////////////生成变量//////////////
            for (int t = 0; t < Ida.TimeHorizon; t++)
            {
                    foreach (IMDPState s in Ida.SS)
                    {
                        foreach (IMDPDecision a in Ida.GenDecisionSpace(s))
                        {
                            Column col = model.Column(cost, Ida.Reward(t, s, a));
                            col = col.And(model.Column(constraint[t][Ida.SS.IndexOf(s)], 1));//插入Aj

                                if (t < Ida.TimeHorizon - 1)
                            {
                                foreach (IMDPState k in Ida.GenStateSpace(s, a))
                                {
                                    col = col.And(model.Column(constraint[t + 1][Ida.SS.IndexOf(k)], -Ida.Prob(t, s, k, a)));//插入Aj
                                    }
                            }
                            model.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                        }
                    }
                }
            #endregion
            model.SetOut(null);
            return model;
        }
    }
}
