using com.foxmail.wyyuan1991.NRM.RailwayModel;
using com.foxmail.wyyuan1991.NRM.Simulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimSolver
{
    public interface SimSolver
    {
        BookingSimulator Simulator { get; set; }
        NRMDataAdapter Data { get; set; }

    }
}
