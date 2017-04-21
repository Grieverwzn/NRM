using System;
using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.ALP
{
    /// <summary>
    /// MDP问题中的状态
    /// </summary>
    public interface IMDPState: IEquatable<IMDPState>
    {
        IMDPState Clone();
        //bool CanSupportDecision(IDecision _d);
    }
    /// <summary>
    /// MDP问题中的状态空间
    /// </summary>
    public interface IMDPStateSpace :IList<IMDPState>,ICollection<IMDPState>, IEnumerable<IMDPState>
    {
      
    }
    /// <summary>
    /// MDP问题中的决策
    /// </summary>
    public interface IMDPDecision
    {
       
    }
    /// <summary>
    /// MDP问题中的决策空间
    /// </summary>
    public interface IMDPDecisionSpace : IList<IMDPDecision>,ICollection<IMDPDecision>, IEnumerable<IMDPDecision>
    {
        void UnionWith(IMDPDecisionSpace tempSet);
    }
    /// <summary>
    /// 有限时间的MDP问题
    /// </summary>
    public interface IFTMDP
    {
        //时间长度
        int TimeHorizon { get; }
        //状态空间
        IMDPStateSpace SS { get; }
        //决策空间
        IMDPDecisionSpace DS { get; }
        //状态s下的决策子空间
        IMDPDecisionSpace GenDecisionSpace(IMDPState s);
        //状态s，决策a下的状态子空间
        IMDPStateSpace GenStateSpace(IMDPState s, IMDPDecision a);
        //从状态(t,i)到(t+1,j),通过行动a的概率  
        double Prob(int time,IMDPState s1, IMDPState s2, IMDPDecision a);
        double Reward(int time,IMDPState s, IMDPDecision a);
        //起始状态
        IMDPState InitialState { get; set; }
    }
}