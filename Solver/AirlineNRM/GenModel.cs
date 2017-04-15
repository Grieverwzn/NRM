using MDP;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Linq;

namespace AirlineNRM
{
    public static class GenModel
    {
        /*
        //线性规划模型
        public static Cplex GenLPModel(IDataAdapter Ida)
        {
            Cplex NRMSolver = new Cplex();
            INumVar[][] v = new INumVar[Ida.TimeHorizon + 1][];   //V(t,x)
            for (int t = 0; t < Ida.TimeHorizon + 1; t++)
            {
                v[t] = NRMSolver.NumVarArray(Ida.SS.Count,//状态数,
                    -double.MaxValue, double.MaxValue);
            }
            IObjective RevenueUB = NRMSolver.AddMinimize(NRMSolver.Prod(1, v[0][Ida.SS.IndexOf(Ida.SS.InitialState)]));
            //int cons = 0;
            //int totalcons = Ida.TimeHorizon * Ida.SS.Count * Ida.DS.Count;
            //time->State->Active-> Expression
            //这一块的效率是很低的，可以通过并行来提高
            for (int t = 0; t < Ida.TimeHorizon; t++)
            {
                foreach (IState s in Ida.SS)
                {
                    foreach (IDecision a in Ida.GenDecisionSpace(s))
                    {
                        double temp = a.OpenProductSet.Sum(i => Ida.Ro(t) * Ida.P(t, i, a) * Ida.f(i));
                        double a1 = a.OpenProductSet.Sum(i => Ida.Ro(t) * Ida.P(t, i, a));
                        INumExpr expr = NRMSolver.Sum(
                            v[t][Ida.SS.IndexOf(s)],
                            NRMSolver.Prod(a1 - 1, v[t + 1][Ida.SS.IndexOf(s)]               
                            ));
                        foreach (IProduct p in a.OpenProductSet)
                        {
                            expr = NRMSolver.Sum(expr,
                                NRMSolver.Prod(-Ida.Ro(t) * Ida.P(t, p, a), v[t + 1][Ida.SS.IndexOf(Ida.SS.MinusOneUnit(s, p))]));
                        }
                        NRMSolver.AddGe(expr, temp);
                        //Console.WriteLine("增加了一个约束，目前共{0}/{1}个", cons++, totalcons);
                    }
                }
            }

            //Boundry condition
            foreach (IState s in Ida.SS)
            {
                NRMSolver.AddEq(v[Ida.TimeHorizon][Ida.SS.IndexOf(s)], 0);
            }

            return NRMSolver;
        }
        //对偶线性规划模型
        public static Cplex GenDualLPModel(IDataAdapter Ida)
        {
            Cplex NRMSolver = new Cplex();

            IObjective cost = NRMSolver.AddMaximize();
            IRange[][] constraint = new IRange[Ida.TimeHorizon][];
            for (int i = 0; i < Ida.TimeHorizon; i++)
            {
                constraint[i] = new IRange[Ida.SS.Count];
                foreach (IState s in Ida.SS)
                {
                    if (i == 0 && s.Equals(Ida.SS.InitialState))
                    {
                        constraint[i][Ida.SS.IndexOf(s)] = NRMSolver.AddRange(1, 1);
                    }
                    else
                    {
                        constraint[i][Ida.SS.IndexOf(s)] = NRMSolver.AddRange(0, 0);
                    }
                }
            }

            for (int t = 0; t < Ida.TimeHorizon; t++)
            {
                foreach (IState s in Ida.SS)
                {
                    foreach (IDecision a in Ida.GenDecisionSpace(s))
                    {
                        Column col = NRMSolver.Column(cost, a.OpenProductSet.Sum(p => Ida.Ro(t) * Ida.P(t, p, a) * Ida.f(p)));
                        col = col.And(NRMSolver.Column(constraint[t][Ida.SS.IndexOf(s)], 1));//插入Aj

                        if (t < Ida.TimeHorizon - 1)
                        {
                            //实现遍历从状态s出发，在决策集a下，能到达下一时刻的所有状态 及 概率
                            foreach (IState _s in Ida.GenStateSpace(s, a))
                            {
                                col = col.And(NRMSolver.Column(constraint[t + 1][Ida.SS.IndexOf(_s)], -Ida.Prob(t, s, _s, a)));//插入Aj
                            }
                        }
                        NRMSolver.NumVar(col, 0, double.MaxValue, NumVarType.Float);
                    }

                }
            }

            return NRMSolver;
        }
        */
    }
}
