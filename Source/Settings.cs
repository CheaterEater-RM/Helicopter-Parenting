using UnityEngine;
using Verse;

namespace HelicopterParenting
{
    public class HelicopterParenting_Settings : ModSettings
    {
        public int mapEdgeBuffer = 15;
        public bool avoidPredators;
        public int predatorScanRadius = 15;
        public bool rejectDuringThreats = true;
        public bool enableFloorDrawFix = true;
        public bool enableNatureRunFix = true;
        public bool enableSkyDreamFix = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref mapEdgeBuffer, "mapEdgeBuffer", 15);
            Scribe_Values.Look(ref avoidPredators, "avoidPredators", false);
            Scribe_Values.Look(ref predatorScanRadius, "predatorScanRadius", 15);
            Scribe_Values.Look(ref rejectDuringThreats, "rejectDuringThreats", true);
            Scribe_Values.Look(ref enableFloorDrawFix, "enableFloorDrawFix", true);
            Scribe_Values.Look(ref enableNatureRunFix, "enableNatureRunFix", true);
            Scribe_Values.Look(ref enableSkyDreamFix, "enableSkyDreamFix", true);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                mapEdgeBuffer = Mathf.Clamp(mapEdgeBuffer, 5, 50);
                predatorScanRadius = Mathf.Clamp(predatorScanRadius, 5, 50);
            }
        }
    }

    public class HelicopterParenting_Mod : Mod
    {
        public static HelicopterParenting_Settings Settings { get; private set; }

        public HelicopterParenting_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HelicopterParenting_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Helicopter Parenting";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("HelicopterParenting_EnableFloorDrawFixLabel".Translate(), ref Settings.enableFloorDrawFix, "HelicopterParenting_EnableFloorDrawFixTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_EnableNatureRunFixLabel".Translate(), ref Settings.enableNatureRunFix, "HelicopterParenting_EnableNatureRunFixTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_EnableSkyDreamFixLabel".Translate(), ref Settings.enableSkyDreamFix, "HelicopterParenting_EnableSkyDreamFixTooltip".Translate());
            listing.GapLine();
            listing.CheckboxLabeled("HelicopterParenting_RejectDuringThreatsLabel".Translate(), ref Settings.rejectDuringThreats, "HelicopterParenting_RejectDuringThreatsTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_AvoidPredatorsLabel".Translate(), ref Settings.avoidPredators, "HelicopterParenting_AvoidPredatorsTooltip".Translate());
            listing.GapLine();

            Settings.mapEdgeBuffer = Mathf.Clamp(Mathf.RoundToInt(listing.Slider(Settings.mapEdgeBuffer, 5f, 50f)), 5, 50);
            listing.Label("HelicopterParenting_MapEdgeBufferLabel".Translate(Settings.mapEdgeBuffer));

            if (Settings.avoidPredators)
            {
                Settings.predatorScanRadius = Mathf.Clamp(Mathf.RoundToInt(listing.Slider(Settings.predatorScanRadius, 5f, 50f)), 5, 50);
                listing.Label("HelicopterParenting_PredatorScanRadiusLabel".Translate(Settings.predatorScanRadius));
            }

            listing.End();
        }
    }
}