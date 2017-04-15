using com.foxmail.wyyuan1991.NRM.Common;
using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public interface IController
    {
        IConOL GenConOL();
        List<IProduct> OpenProductList(int time, ResouceState r, IConOL cl);
        List<Ticket> PrintTickets(ResouceState rs, List<IProduct> pro, IConOL cl);//打印车票
    }
    //control需要的在线内容
    public interface IConOL
    {
        void Update(ResouceState r);
    }
}

