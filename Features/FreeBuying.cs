using HisTools.Features.Controllers;

namespace HisTools.Features;

public class FreeBuying : FeatureBase
{
    // Patches/App_PerkPagePatch.cs
    // Patches/ENV_VendingMachinePatch.cs
    public FreeBuying() : base("FreeBuying", "Buy something for free")
    {
        AddSetting(new BoolSetting(this, "Items", "Free items in the vending machine", true));
        AddSetting(new BoolSetting(this, "Refresh perks", "Free refresh perks", true));
        AddSetting(new BoolSetting(this, "Perks", "Free paid perks", true));
    }
}