using System;
using Core.Portal.Scripts;
using TheOneFramework.Portals;
using UnityEngine;

public class Lift : MonoBehaviour
{
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Im just using pressureplateweight as the checker to keep it consistent in the code. I guess the lift has an "invisible pressureplate"
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PressurePlateWeight>() is PressurePlateWeight pressurePlate)
        {
            if (pressurePlate.WeightType == WeightType.Player)
            {
                LevelManager.Instance.LoadNextLevel();
            }
        }
    }
}
