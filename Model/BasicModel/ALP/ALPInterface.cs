using com.foxmail.wyyuan1991.NRM.Common;
using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.ALP
{
    public interface IALPResource:IResource, IMDPState
    {

    }
    public interface IALPResourceSpace : IEnumerable<IALPResource>
    {
        IALPResource this[int index] { get; }//返回资源
        int Count { get; }//资源数量
        int IndexOf(IALPResource item);//资源编号
    }

    public interface IALPState : IMDPState
    {
        int this[IALPResource key] { get; }
        bool Within(IALPState other);
    }

    public interface IALPDecision:IMDPDecision
    {
        bool UseResource(IALPResource r);
    }

    public interface IALPDecisionSpace : IMDPDecisionSpace
    {
        IMDPDecision CloseAllDecision();
    }
    /// <summary>
    /// 仿射近似的FTMDP问题
    /// </summary>
    public interface IALPFTMDP : IFTMDP
    {
        IALPResourceSpace RS { get; }
        double Rt(int time, IMDPDecision a);
        double Qti(int time, IALPResource re, IMDPDecision a);
        IALPState CreateOrFind(IDictionary<IALPResource, int> RecDic);
    }
}
