using System.Runtime.InteropServices;
using static Canyon.Game.Services.Managers.JiangHuManager;

namespace Canyon.Game.States
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public struct JiangHuStar
    {
        public JiangHuStar(
            JiangHuQuality quality,
            JiangHuAttrType type,
            byte stage,
            byte star
            )
        {
            Quality = quality;
            Type = type;
            PowerLevel = stage;
            Star = star;
        }

        public JiangHuQuality Quality { get; init; }
        public JiangHuAttrType Type { get; init; }
        public byte PowerLevel { get; init; }
        public byte Star { get; init; }
    }
}
