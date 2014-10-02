using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MissionPlanner.HIL
{
    public delegate void ProgressEventHandler(int progress, string status);
}
