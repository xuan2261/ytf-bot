using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class SleepTime
    {
        //private DateTime last 
        public bool IsWorkTime(DateTime dateTimeUtc)
        {
            if (dateTimeUtc.Hour >= 22 | (dateTimeUtc.Hour >= 0 & dateTimeUtc.Hour <= 8))
            {
                if (dateTimeUtc.Minute >= 1 & dateTimeUtc.Minute <= 4)
                {
                    return true;
                }
            }
            else
            {
                
            }
            var currentUtc = DateTime.UtcNow;
            return true; //TimeSpan.FromMinutes(10);
        }
    }
}
