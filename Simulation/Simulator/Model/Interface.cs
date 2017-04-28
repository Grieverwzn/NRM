using com.foxmail.wyyuan1991.NRM.Common;
using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public interface IController
    {
        IConOL GenConOL();
        List<IProduct> OpenProductList(int time, MetaResouceState r, IConOL cl);
        List<Ticket> PrintTickets(MetaResouceState rs, List<IProduct> pro, IConOL cl);//打印车票
    }
    //control需要的在线内容
    public interface IConOL
    {
        void Update(MetaResouceState r);
    }
}

