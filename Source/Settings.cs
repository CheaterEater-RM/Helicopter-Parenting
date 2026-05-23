using UnityEngine;
using Verse;

namespace HelicopterParenting
{
    public class HelicopterParenting_Settings : ModSettings
    {
        private const int DefaultMapEdgeBuffer = 15;

        private const int DefaultPredatorScanRadius = 15;

        public int mapEdgeBuffer = DefaultMapEdgeBuffer;
        public bool avoidPredators;
        public int predatorScanRadius = DefaultPredatorScanRadius;
        public bool rejectDuringThreats = true;
        public bool enableFloorDrawFix = true;
        public bool enableNatureRunFix = true;
        public bool enableSkyDreamFix = true;
        public bool enableDiagnostics;

        public void ResetToDefaults()
        {
            mapEdgeBuffer = DefaultMapEdgeBuffer;
            avoidPredators = false;
            predatorScanRadius = DefaultPredatorScanRadius;
            rejectDuringThreats = true;
            enableFloorDrawFix = true;
            enableNatureRunFix = true;
            enableSkyDreamFix = true;
            enableDiagnostics = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref mapEdgeBuffer, "mapEdgeBuffer", DefaultMapEdgeBuffer);
            Scribe_Values.Look(ref avoidPredators, "avoidPredators", false);
            Scribe_Values.Look(ref predatorScanRadius, "predatorScanRadius", DefaultPredatorScanRadius);
            Scribe_Values.Look(ref rejectDuringThreats, "rejectDuringThreats", true);
            Scribe_Values.Look(ref enableFloorDrawFix, "enableFloorDrawFix", true);
            Scribe_Values.Look(ref enableNatureRunFix, "enableNatureRunFix", true);
            Scribe_Values.Look(ref enableSkyDreamFix, "enableSkyDreamFix", true);
            Scribe_Values.Look(ref enableDiagnostics, "enableDiagnostics", false);

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
            const float sliderLabelPct = 0.42f;

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("HelicopterParenting_EnableFloorDrawFixLabel".Translate(), ref Settings.enableFloorDrawFix, "HelicopterParenting_EnableFloorDrawFixTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_EnableNatureRunFixLabel".Translate(), ref Settings.enableNatureRunFix, "HelicopterParenting_EnableNatureRunFixTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_EnableSkyDreamFixLabel".Translate(), ref Settings.enableSkyDreamFix, "HelicopterParenting_EnableSkyDreamFixTooltip".Translate());
            listing.GapLine();
            listing.CheckboxLabeled("HelicopterParenting_RejectDuringThreatsLabel".Translate(), ref Settings.rejectDuringThreats, "HelicopterParenting_RejectDuringThreatsTooltip".Translate());
            listing.CheckboxLabeled("HelicopterParenting_AvoidPredatorsLabel".Translate(), ref Settings.avoidPredators, "HelicopterParenting_AvoidPredatorsTooltip".Translate());
            listing.GapLine();

            Settings.mapEdgeBuffer = Mathf.Clamp(
                Mathf.RoundToInt(listing.SliderLabeled("HelicopterParenting_MapEdgeBufferLabel".Translate(Settings.mapEdgeBuffer), Settings.mapEdgeBuffer, 5f, 50f, sliderLabelPct)),
                5,
                50);

            if (Settings.avoidPredators)
            {
                Settings.predatorScanRadius = Mathf.Clamp(
                    Mathf.RoundToInt(listing.SliderLabeled("HelicopterParenting_PredatorScanRadiusLabel".Translate(Settings.predatorScanRadius), Settings.predatorScanRadius, 5f, 50f, sliderLabelPct)),
                    5,
                    50);
            }

            listing.GapLine();
            listing.CheckboxLabeled("HelicopterParenting_EnableDiagnosticsLabel".Translate(), ref Settings.enableDiagnostics, "HelicopterParenting_EnableDiagnosticsTooltip".Translate());

            listing.GapLine();
            if (listing.ButtonText("HelicopterParenting_ResetSettingsLabel".Translate()))
            {
                Settings.ResetToDefaults();
            }

            listing.End();
        }
    }
}