using com.foxmail.wyyuan1991.NRM.Data;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using com.foxmail.wyyuan1991.NRM.RailwaySolver;
using com.foxmail.wyyuan1991.NRM.Simulator;

namespace com.foxmail.wyyuan1991.NRM.Command
{
    public class Warpper
    {
        public NRMDataAdapter da = new NRMDataAdapter();
        public RailwayNRMSolver_DD solver = new RailwayNRMSolver_DD();
        public ArrivalSimulator arrSim = new ArrivalSimulator();
        public BookingSimulator bookSim = new BookingSimulator();
        public SimAnalysisor SA = new SimAnalysisor();
    }
}
