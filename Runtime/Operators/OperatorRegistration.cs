using Unity.Muse.Common;
using UnityEngine;

namespace Unity.Muse.Sprite.Operators
{
    internal class OperatorRegistration
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterOperators()
        {
            OperatorsFactory.RegisterOperator<SpriteGeneratorSettingsOperator>();
            OperatorsFactory.RegisterOperator<KeyImageOperator>();
            OperatorsFactory.RegisterOperator<SpriteRefiningMaskOperator>();
            OperatorsFactory.RegisterOperator<SessionOperator>();
        }
    }
}
