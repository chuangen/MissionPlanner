using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls.BackstageView;
using log4net;
using MissionPlanner.Controls;
using System.IO;
using System.Collections.Generic;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// Helper class for the stylng 'theming' of forms and controls, and provides MessageBox
    /// replacements which are also styled
    /// </summary>
    public class ThemeManager
    {
        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Themes _currentTheme = Themes.BurntKermit;
        public static Themes CurrentTheme { get { return _currentTheme; } }

        public enum Themes
        {
            /// <summary>
            /// no theme - standard Winforms appearance
            /// </summary>
            None,
   
            /// <summary>
            /// Standard Planner Charcoal & Green colours
            /// </summary>
            BurntKermit,
            HighContrast,
            Test,
            Custom,
        }

        /// <summary>
        /// Change the current theme. Existing controls are not affected
        /// </summary>
        /// <param name="theme"></param>
        public static void SetTheme(Themes theme)
        {
        }

        public static void CustomColor()
        {
        }

        /// <summary>
        /// Will recursively apply the current theme to 'control'
        /// </summary>
        /// <param name="control"></param>
        public static void ApplyThemeTo(Control control)
        {     
        }


        public static void doxamlgen()
        {
        }

        public static void xaml(Control control)
        {
        }

        private static void doxamlctls(Control control, StreamWriter st)
        {
        }

        private static void ApplyCustomTheme(Control temp, int level)
        {
        }

        private static void ApplyTestTheme(Control temp, int level)
        {
        }

        public static Color BGColor, ControlBGColor, TextColor, ButBG, ButBorder;

        private static void ApplyHighContrast(Control temp, int level)
        {
        }

        private static void ApplyBurntKermitTheme(Control temp, int level)
        {
        }

    }
}