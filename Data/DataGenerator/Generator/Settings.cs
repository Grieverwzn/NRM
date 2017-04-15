using System;
using System.Data;

namespace com.foxmail.wyyuan1991.NRM.Data
{
    public class Settings
    {
        public Settings(DataTable dt)
        {
            DataRow dr = dt.Rows[0];
            NormalizedTicketPrice = Convert.ToDouble(dr["区间标准价格"]);
            PriceDeclineRatio = Convert.ToDouble(dr["票价递减率"]);
        }
        public double NormalizedTicketPrice { get; set; }
        public double PriceDeclineRatio { get; set; }
    }
}
