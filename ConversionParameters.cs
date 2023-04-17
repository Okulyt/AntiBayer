using Antibayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AntiBeyer
{
    [DataContractAttribute]
    public class ConversionParameters
    {
        public ConversionParameters()
        { }

        public ConversionParameters(BayerFilter bayerFilter, ushort maxGrayLevel)
        {
            BayerFilter = bayerFilter;
            MaxGreyLevel = maxGrayLevel;
        }

        [DataMember]
        public List<StrongPoint> StrongPoints = new List<StrongPoint>();
        [DataMember]
        public bool RandomizeFractionalPart;
        [DataMember]
        public BayerFilter BayerFilter;
        
        [DataMember]        
        public Dictionary<ushort, ushort> ConversionDictionary { get; private set; }
        
        [DataMember]
        public Dictionary<ushort, double> ConversionFrationDictionary { get; private set; }
        
        [DataMember]
        private ushort MaxGreyLevel;

        public void RemovePreviewStrongPoints()
        {
            StrongPoints = StrongPoints?.Where(sp => sp.PreviewStrongPoint == false)?.ToList();
            CreateConversionDictionary(MaxGreyLevel);
        }

        public void CreateConversionDictionary(ushort maxGreyLevel = 255)
        {
            MaxGreyLevel = maxGreyLevel;
            if (StrongPoints == null || StrongPoints?.Count == 0)
            {
                StrongPoints = new List<StrongPoint>() { new StrongPoint(BayerFilter, 0, 0,false), new StrongPoint(BayerFilter, maxGreyLevel, maxGreyLevel, false) };
            }
            if (!StrongPoints.Any(s => s.OriginalGreyValue == 0))
            {
                StrongPoints.Add(new StrongPoint(BayerFilter, 0, 0, false));
            }
            if (!StrongPoints.Any(s => s.OriginalGreyValue == maxGreyLevel))
            {
                StrongPoints.Add(new StrongPoint(BayerFilter, maxGreyLevel, maxGreyLevel, false));
            }
            StrongPoints.Sort(StrongPoint.Comparison);
            ConversionDictionary = new Dictionary<ushort, ushort>();
            ConversionFrationDictionary = new Dictionary<ushort, double>();


            for (int sp = 0; sp < StrongPoints.Count - 1; sp++)
            {
                var baseGrey = StrongPoints[sp].OriginalGreyValue;
                var levelDifference = StrongPoints[sp + 1].OriginalGreyValue - StrongPoints[sp].OriginalGreyValue;

                var increment = (StrongPoints[sp + 1].NewGreyValue - StrongPoints[sp].NewGreyValue) / (double)(StrongPoints[sp + 1].OriginalGreyValue - StrongPoints[sp].OriginalGreyValue);
                //for (ushort i = StrongPoints[sp].OriginalGreyValue; i <= StrongPoints[sp + 1].OriginalGreyValue; i++)
                for (ushort i=0;i<=levelDifference;i++)
                {               
                    var greyFraction = (increment * i) - Math.Floor(increment * i);
                    if (!ConversionDictionary.ContainsKey((ushort)(baseGrey+i)))
                    {
                        ConversionDictionary.Add((ushort)(baseGrey +i), (ushort)(baseGrey + increment * i));
                        ConversionFrationDictionary.Add((ushort)(baseGrey +i), greyFraction);
                    }
                    //Fraction will later be realized by iterating over multiple frames and adding a randomized value "hit" to the respective value from Conversiondictionary.
                    //var randomizer = new Random();
                    //var hit = randomizer.NextDouble() <= greyFraction?1:0;
                }
            }
            for (ushort g = 0; g <= maxGreyLevel; g++)
            {
                if (!ConversionDictionary.ContainsKey(g))
                {
                    DebugTools.Log("SEVERE CONVERSIONDICTIONARY CREATION FAILURE! Missing grey value " + g);
                    DebugTools.Log("DUMP:");
                    DebugTools.Log("BayerFilter=" + BayerFilter);
                    foreach (var g2 in ConversionDictionary) DebugTools.Log(g2.Key + "->" + g2.Value);
                    DebugTools.Log("StrongPoints:");
                    foreach (var sp in StrongPoints)
                    {
                        DebugTools.Log(sp.OriginalGreyValue + "->" + sp.NewGreyValue);
                    }
                }
            }
        }
    }

    [DataContractAttribute]
    public class ConversionParameterSet
    {
        public ConversionParameterSet()
        { }

        public ConversionParameterSet(bool CurrentImageIsRGB,ushort maxGrayLevel)
        {
            if (CurrentImageIsRGB)
            {
                Sets.Add(BayerFilter.red, new ConversionParameters(BayerFilter.red,maxGrayLevel));
                Sets[BayerFilter.red].CreateConversionDictionary();
                Sets.Add(BayerFilter.green1, new ConversionParameters(BayerFilter.green1, maxGrayLevel));
                Sets[BayerFilter.green1].CreateConversionDictionary();
                Sets.Add(BayerFilter.green2, new ConversionParameters(BayerFilter.green2, maxGrayLevel));
                Sets[BayerFilter.green2].CreateConversionDictionary();
                Sets.Add(BayerFilter.blue, new ConversionParameters(BayerFilter.blue, maxGrayLevel));
                Sets[BayerFilter.blue].CreateConversionDictionary();
            }
            else
            {
                Sets.Add(BayerFilter.None, new ConversionParameters(BayerFilter.None, maxGrayLevel));
                Sets[BayerFilter.None].CreateConversionDictionary();
            }
        }

        public void RemovePreviewStrongPoints()
        {
            foreach(var set in Sets)
            {
                set.Value.RemovePreviewStrongPoints();
            }
        }

        internal void RemoveStrongPoint(StrongPoint pointToDelete)
        {
            if (pointToDelete == null) return;
            var pointInList = Sets[pointToDelete.BayerFilter].StrongPoints?.FirstOrDefault(sp => sp.OriginalGreyValue == pointToDelete.OriginalGreyValue);
            if (pointInList == null) return;
            {
                Sets[pointToDelete.BayerFilter].StrongPoints.Remove(pointInList);
            }
        }

        [DataMember]
        public Dictionary<BayerFilter, ConversionParameters> Sets=new Dictionary<BayerFilter, ConversionParameters>();
    }


    [DataContractAttribute]
    public class StrongPoint
    {
        public StrongPoint(BayerFilter bayerFilter, ushort originalGreyValue, ushort newGreyValue, bool previewStrongPoint)
        {
            BayerFilter = bayerFilter;
            OriginalGreyValue = originalGreyValue;
            NewGreyValue = newGreyValue;
            PreviewStrongPoint = previewStrongPoint;
        }
        [DataMember]
        public BayerFilter BayerFilter;
        [DataMember]
        public ushort OriginalGreyValue;
        [DataMember]
        public ushort NewGreyValue;
        [DataMember]
        public bool PreviewStrongPoint=false;

        internal static int Comparison(StrongPoint x, StrongPoint y)
        {
            return x.OriginalGreyValue.CompareTo(y.OriginalGreyValue);
        }
    }
}

