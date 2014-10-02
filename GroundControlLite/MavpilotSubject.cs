using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MissionPlanner
{
    public class MavpilotSubject
    {
        static MavpilotSubject instance = null;
        public static MavpilotSubject Default
        {
            get
            {
                if (instance == null)
                    instance = new MavpilotSubject();

                return instance;
            }
        }
        private MavpilotSubject()
        { }
    }
}
