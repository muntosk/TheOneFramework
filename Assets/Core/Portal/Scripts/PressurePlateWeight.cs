using UnityEngine;

namespace TheOneFramework.Portals
{
    public enum WeightType
    {
        All,
        Player,
        Cube      
    }

    public class PressurePlateWeight : MonoBehaviour
    {
        [SerializeField] private WeightType weightType = WeightType.All;
        public WeightType WeightType => weightType;
    }
}