using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SlimeSenpai.EndlessGrowth
{
    public class EndlessGrowthModSettings : ModSettings
    {
        public int maxLevel = -1;

        public string maxLevelString;

        public bool unlimitedPrice = false;

        public int gapForMaxBill = 20;

        public string gapForMaxBillString;

        public bool craftNotificationsEnabled = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref maxLevel, "maxLevel");
            Scribe_Values.Look(ref unlimitedPrice, "unlimitedPrice");
            Scribe_Values.Look(ref gapForMaxBill, "gapForMaxBill");
            Scribe_Values.Look(ref craftNotificationsEnabled, "craftNotificationsEnabled");
            base.ExposeData();
        }
    }
}
