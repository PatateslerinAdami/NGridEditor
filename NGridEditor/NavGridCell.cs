namespace LoLNGRIDConverter
{
    public class NavGridCell
    {
        public int index;
        public VisionPathingFlags visionPathingFlags;
        public RiverRegionFlags riverRegionFlags;
        public JungleQuadrantFlags jungleQuadrantFlags;
        public MainRegionFlags mainRegionFlags;
        public NearestLaneFlags nearestLaneFlags;
        public POIFlags poiFlags;
        public RingFlags ringFlags;
        public UnknownSRXFlags srxFlags;

        public int x;
        public int z;

        public bool hasOverride;
    }

    [System.Flags]
    public enum VisionPathingFlags : short
    {
        Walkable = 0,
        Brush = 1,
        Wall = 2,
        StructureWall = 4,
        Unobserved8 = 8,
        Unobserved16 = 16,
        Unobserved32 = 32,
        TransparentWall = 64,
        Unknown128 = 128,
        AlwaysVisible = 256,
        Unknown512 = 512,
        BlueTeamOnly = 1024,
        RedTeamOnly = 2048,
        NeutralZoneVisiblity = 4096,
    }

    [System.Flags]
    public enum RiverRegionFlags : byte
    {
        NonJungle = 0,
        JungleQuadrant = 1,
        BaronPit = 2,
        Unobserved4 = 4,
        Unobserved8 = 8,
        River = 16,
        Unknown32 = 32,
        RiverEntrance = 64,
    }

    public enum JungleQuadrantFlags : byte
    {
        None = 0,
        NorthJungleQuadrant = 1,
        EastJungleQuadrant = 2,
        WestJungleQuadrant = 3,
        SouthJungleQuadrant = 4,
        Unobserved8 = 8,
    }

    public enum MainRegionFlags : byte
    {
        Spawn = 0,
        Base = 1,
        TopLane = 2,
        MidLane = 3,
        BotLane = 4,
        TopSideJungle = 5,
        BotSideJungle = 6,
        TopSideRiver = 7,
        BotSideRiver = 8,
        TopSideBasePerimeter = 9,
        BotSideBasePerimeter = 10,
        TopSideLaneAlcove = 11,
        BotSideLaneAlcove = 12,
    }

    public enum NearestLaneFlags : byte
    {
        BlueSideTopLane = 0,
        BlueSideMidLane = 1,
        BlueSideBotLane = 2,
        RedSideTopLane = 3,
        RedSideMidLane = 4,
        RedSideBotLane = 5,
        BlueSideTopNeutralZone = 6,
        BlueSideMidNeutralZone = 7,
        BlueSideBotNeutralZone = 8,
        RedSideTopNeutralZone = 9,
        RedSideMidNeutralZone = 10,
        RedSideBotNeutralZone = 11,
    }

    public enum POIFlags : byte
    {
        None = 0,
        NearTurret = 1,
        CloudDrakeWindTunnel = 2,
        BaronPit = 3,
        DragonPit = 4,
        CampRedBuff = 5,
        CampBlueBuff = 6,
        CampGromp = 7,
        CampKrugs = 8,
        CampRaptors = 9,
        CampMurkWolves = 10,
    }

    public enum RingFlags : byte
    {
        BlueSpawnToNexus = 0,
        BlueNexusToInhib = 1,
        BlueInhibToInner = 2,
        BlueInnerToOuter = 3,
        BlueOuterToNeutral = 4,
        RedSpawnToNexus = 5,
        RedNexusToInhib = 6,
        RedInhibToInner = 7,
        RedInnerToOuter = 8,
        RedOuterToNeutral = 9,
    }

    public enum UnknownSRXFlags : byte
    {
        Walkable = 0,
        Wall = 1,
        TransparentWall = 2,
        Brush = 3,
        Unobserved4 = 4,
        TopSideOceanDrakePuddle = 5,
        BotSideOceanDrakePuddle = 6,
        BlueTeamOnly = 7,
        RedTeamOnly = 8,
        Unobserved9 = 9,
        Unobserved10 = 10,
        BlueTeamOnlyNeutralZoneVisibility = 11,
        RedTeamOnlyNeutralZoneVisibility = 12,
        BrushWall = 13,
    }
}