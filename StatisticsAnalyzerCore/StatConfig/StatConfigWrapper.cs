using System;
using System.Collections.Generic;

namespace StatisticsAnalyzerCore.StatConfig
{
    public abstract class ConfigObject
    {
        private readonly Dictionary<string, object> _objectCache;
        protected StatConfig StatConfig;

        protected T RetreiveValue<T>(Func<T> retreiveAction, string objectKey)
        {
            if (_objectCache.ContainsKey(objectKey))
            {
                return (T)_objectCache[objectKey];
            }

            var obj = retreiveAction();
            _objectCache[objectKey] = obj;
            return obj;
        }

        protected double ReadDouble(string key)
        {
            return RetreiveValue(() => StatConfig.ReadDecimal(key), key);
        }

        protected string ReadString(string key)
        {
            return RetreiveValue(() => StatConfig.ReadString(key), key);
        }

        protected bool ReadBool(string key)
        {
            return RetreiveValue(() => StatConfig.ReadBool(key), key);
        }

        protected ConfigObject(StatConfig statConfig)
        {
            _objectCache = new Dictionary<string, object>();
            StatConfig = statConfig;
        }        
    }

    public class FixedEffectConfig : ConfigObject
    {
        public FixedEffectConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.FixedEffects.SigLevel");
            }
        }

        public double MinNumericalLevels
        {
            get
            {
                return ReadDouble("MixedConfig.FixedEffects.MinNumericalLevels");
            }
        }
    }

    public class RandomEffectsConfig : ConfigObject
    {
        public RandomEffectsConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.RandomEffects.SigLevel");
            }
        }

        public double RandomLevelCountsWarn
        {
            get
            {
                return ReadDouble("MixedConfig.RandomEffects.RandomLevelCountsWarn");
            }
        }
        
        public double RandomLevelCountExclude
        {
            get
            {
                return ReadDouble("MixedConfig.RandomEffects.RandomLevelCountExclude");
            }
        }
        
        public string FixedEffectTest
        {
            get
            {
                return ReadString("MixedConfig.RandomEffects.FixedEffectTest");
            }
        }
        
        public double LargeSampleN
        {
            get
            {
                return ReadDouble("MixedConfig.RandomEffects.LargeSampleN");
            }
        }
        
        public double MinCoveringScoreCovariate
        {
            get
            {
                return ReadDouble("MixedConfig.RandomEffects.MinCoveringScoreCovariate");
            }
        }
    }

    public class LeveneTestConfig : ConfigObject
    {
        public LeveneTestConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.AssumptionTests.LeveneTest.SigLevel");
            }
        }
    }

    public class BrueshPaganTestConfig : ConfigObject
    {
        public BrueshPaganTestConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.AssumptionTests.BrueshPaganTest.SigLevel");
            }
        }
}

    public class DurbinWatsonTestConfig : ConfigObject
    {
        public DurbinWatsonTestConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.AssumptionTests.DurbinWatsonTest.SigLevel");
            }
        }
    }

    public class ShapiroWilkTestTestConfig : ConfigObject
    {
        public ShapiroWilkTestTestConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double SigLevel
        {
            get
            {
                return ReadDouble("MixedConfig.AssumptionTests.ShapiroWilkTest.SigLevel");
            }
        }
    }

    public class InfluenceTestsConfig : ConfigObject
    {
        public InfluenceTestsConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double OutlierVariance
        {
            get
            {
                return ReadDouble("MixedConfig.AssumptionTests.InfluenceTests.OutlierVariance");
            }
        }

        public string CooksDistance
        {
            get
            {
                return ReadString("MixedConfig.AssumptionTests.InfluenceTests.CooksDistance");
            }
        }
    }

    public class AssumptionTestsConfig : ConfigObject
    {
        private readonly LeveneTestConfig _leveneTestConfig;
        private readonly BrueshPaganTestConfig _brueshPaganTestConfig;
        private readonly DurbinWatsonTestConfig _durbinWatsonTestConfig;
        private readonly ShapiroWilkTestTestConfig _shapiroWilkTestTestConfig;
        private readonly InfluenceTestsConfig _influenceTestsConfig;

        public AssumptionTestsConfig(StatConfig statConfig) : base(statConfig)
        {
            _leveneTestConfig = new LeveneTestConfig(statConfig);
            _brueshPaganTestConfig = new BrueshPaganTestConfig(statConfig);
            _durbinWatsonTestConfig = new DurbinWatsonTestConfig(statConfig);
            _shapiroWilkTestTestConfig = new ShapiroWilkTestTestConfig(statConfig);
            _influenceTestsConfig = new InfluenceTestsConfig(statConfig);
        }

        public LeveneTestConfig LeveneTestConfig { get { return _leveneTestConfig; } }
        public BrueshPaganTestConfig BrueshPaganTestConfig { get { return _brueshPaganTestConfig; } }
        public DurbinWatsonTestConfig DurbinWatsonTestConfig { get { return _durbinWatsonTestConfig; } }
        public ShapiroWilkTestTestConfig ShapiroWilkTestTestConfig { get { return _shapiroWilkTestTestConfig; } }
        public InfluenceTestsConfig InfluenceTestsConfig { get { return _influenceTestsConfig; } }
    }

    public class ModelSuggestionConfig : ConfigObject
    {
        public ModelSuggestionConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public bool SuggestRandomEffectForNestedVariables
        {
            get
            {
                return ReadBool("MixedConfig.ModelSuggestion.SuggestRandomEffectForNestedVariables");
            }
        }

        public bool SuggestInteractionForBalancedVariables
        {
            get
            {
                return ReadBool("MixedConfig.ModelSuggestion.SuggestInteractionForBalancedVariables");
            }
        }

        public bool AllowRandomCovariateWithoutMainEffect
        {
            get
            {
                return ReadBool("MixedConfig.ModelSuggestion.AllowRandomCovariateWithoutMainEffect");
            }
        }
    }

    public class BenjaminiHochbergConfig : ConfigObject
    {
        public BenjaminiHochbergConfig(StatConfig statConfig) : base(statConfig)
        {
        }

        public double QLevel
        {
            get
            {
                return ReadDouble("MixedConfig.MultipleComparison.BenjaminiHochberg.QLevel");
            }
        }

        public bool AssumeIndependence
        {
            get
            {
                return ReadBool("MixedConfig.MultipleComparison.BenjaminiHochberg.AssumeIndependence");
            }
        }
    }

    public class MultipleComparisonConfig : ConfigObject
    {
        private readonly BenjaminiHochbergConfig _benjaminiHochbergConfig;

        public MultipleComparisonConfig(StatConfig statConfig) : base(statConfig)
        {
            _benjaminiHochbergConfig = new BenjaminiHochbergConfig(statConfig);
        }

        public BenjaminiHochbergConfig BenjaminiHochbergConfig
        {
            get
            {
                return _benjaminiHochbergConfig;
            }
        }
    }

    public class MixedConfig : ConfigObject
    {
        private readonly FixedEffectConfig _fixedEffects;
        private readonly RandomEffectsConfig _randomEffects;
        private readonly AssumptionTestsConfig _assumptionTests;
        private readonly ModelSuggestionConfig _modelSuggestion;
        private readonly MultipleComparisonConfig _multipleComparison;

        public MixedConfig(StatConfig statConfig) : base(statConfig)
        {
            _fixedEffects = new FixedEffectConfig(statConfig);
            _randomEffects = new RandomEffectsConfig(statConfig);
            _assumptionTests = new AssumptionTestsConfig(statConfig);
            _modelSuggestion = new ModelSuggestionConfig(statConfig);
            _multipleComparison = new MultipleComparisonConfig(statConfig);
        }

        public FixedEffectConfig FixedEffectConfig { get { return _fixedEffects; }}
        public RandomEffectsConfig RandomEffectsConfig { get { return _randomEffects; }}
        public AssumptionTestsConfig AssumptionTestsConfig { get { return _assumptionTests; }}
        public ModelSuggestionConfig ModelSuggestionConfig { get { return _modelSuggestion; }}
        public MultipleComparisonConfig MultipleComparisonConfig { get { return _multipleComparison; }}
    }

    public static class StatConfigWrapper
    {
        private static readonly MixedConfig Config = new MixedConfig(new StatConfig(@"StatConfig\StatConfig.xml"));
        public static MixedConfig MixedConfig { get { return Config; } }
    }
}
