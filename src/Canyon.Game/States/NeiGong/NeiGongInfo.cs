using Microsoft.Extensions.Configuration;
using static Canyon.Game.States.NeiGong.InnerStrength;

namespace Canyon.Game.States.NeiGong
{
    public class NeiGongInfo
    {
        private List<StrengthType> secretTypes = new();

        public NeiGongInfo()
        {
            var configuration = new ConfigurationBuilder()
                .AddIniFile(Path.Combine(Environment.CurrentDirectory, "ini", "NeiGongInfo.ini"))
                .Build();

            int neiGongCount = int.Parse(configuration["NeiGong:Num"]);
            for (int i = 1; i <= neiGongCount; i++)
            {
                int neiGongId = int.Parse(configuration[$"{i}:id"]);
                string neiGongName = configuration[$"{i}:Name"];
                int neiGongNum = int.Parse(configuration[$"{i}:NeiGongNum"]);

                for (int x = 1; x <= neiGongNum; x++)
                {
                    int type = int.Parse(configuration[$"{i}-{x}:Type"]);
                    string name = configuration[$"{i}-{x}:Name"];
                    int maxLevel = int.Parse(configuration[$"{i}-{x}:MaxLev"]);
                    string[] attrTypesString = configuration[$"{i}-{x}:AttriType"].Split('-');
                    InnerStrengthAttrType[] attrTypes = new InnerStrengthAttrType[attrTypesString.Length];
                    for (int y = 0; y < attrTypesString.Length; y++)
                    {
                        attrTypes[y] = (InnerStrengthAttrType)int.Parse(attrTypesString[y]);
                    }

                    string[] attrValuesString = configuration[$"{i}-{x}:AttriValue"].Split('-');
                    int[] attrValues = new int[attrValuesString.Length];
                    for (int y = 0; y < attrValuesString.Length; y++)
                    {
                        attrValues[y] = int.Parse(attrValuesString[y]);
                    }

                    string[] neiGongValueString = configuration[$"{i}-{x}:NeiGongValue"].Split('-');
                    int[] neiGongValues = new int[neiGongValueString.Length];
                    for (int y = 0; y < neiGongValueString.Length; y++)
                    {
                        neiGongValues[y] = int.Parse(neiGongValueString[y]);
                    }

                    int requiredLevel = int.Parse(configuration[$"{i}-{x}:ReqLev"]);
                    int requiredNeiGongValue = int.Parse(configuration[$"{i}-{x}:ReqNeiGongValue"]);
                    int requiredPreNeiGong = int.Parse(configuration[$"{i}-{x}:ReqPreNeiGong"]);
                    uint requiredItemType = uint.Parse(configuration[$"{i}-{x}:ReqItemType"]);

                    secretTypes.Add(new StrengthType
                    {
                        Id = type,
                        Name = name,
                        MaxLevel = maxLevel,
                        AttrTypes = attrTypes,
                        AttrValues = neiGongValues,
                        NeiGongValue = neiGongValues,
                        RequiredLevel = requiredLevel,
                        RequiredNeiGongValue = requiredNeiGongValue,
                        RequiredPreNeiGong = requiredPreNeiGong,
                        RequiredItemType = requiredItemType
                    });
                }
            }
        }

        public StrengthType GetStrengthType(byte type)
        {
            return secretTypes.FirstOrDefault(x => x.Id == type);
        }

        public class StrengthType
        {
            public int Id { get; init; }
            public string Name { get; init; }
            public int MaxLevel { get; init; }
            public InnerStrengthAttrType[] AttrTypes { get; init; }
            public int[] AttrValues { get; init; }
            public int[] NeiGongValue { get; init; }
            public int RequiredLevel { get; init; }
            public int RequiredNeiGongValue { get; init; }
            public int RequiredPreNeiGong { get; init; }
            public uint RequiredItemType { get; init; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
