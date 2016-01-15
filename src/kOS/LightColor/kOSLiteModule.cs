using System;
using UnityEngine;
using KSP.IO;

namespace kOS.LightColor
{
    class kOSLiteModule : PartModule
        
    {
        
        [KSPField(isPersistant = true, guiName = "Required Power for Lights", guiActive = true)]
        public float resourceAmount ;
        
        [KSPField(isPersistant = true, guiActive = true, guiName = "Light R")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float red = 1;
        
        [KSPField(isPersistant = true, guiActive = true, guiName = "Light G")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float green = 1;
        
        [KSPField(isPersistant = true, guiActive = true, guiName = "Light B")]
        [UI_FloatRange(maxValue = 1, minValue = 0, scene = UI_Scene.Flight, stepIncrement = 0.01f)]
        protected float blue = 1;
        
        
        
        
        public void Update()
        {
            resourceAmount = ((red + green + blue)/10);
            
            
            if (HighLogic.LoadedSceneIsEditor)
            {
                red = part.FindModelComponents<Light>()[0].color.r;
                green = part.FindModelComponents<Light>()[0].color.g;
                blue = part.FindModelComponents<Light>()[0].color.b;
            }
            
            
            else
            {
                setColor();
            }
        }
        
        public void setColor()
        {
            float electricCharge = 0;
            float electricMax = 0;
            var myVessel = FlightGlobals.ActiveVessel;
            foreach (Part p in myVessel.parts)
            {
                if (p.Resources.Contains("ElectricCharge"))
                {
                    foreach (PartResource pr in p.Resources)
                    {
                        if (pr.resourceName.Equals("ElectricCharge") && (pr.flowState))
                        {
                            electricCharge += (float)pr.amount;
                            electricMax += (float)pr.maxAmount;
                            break;
                        }
                    }
                }
            }
            
            var EnergyRemaining = (electricCharge / electricMax) * 100;
            
            if (EnergyRemaining < 0.5f || part.FindModelComponents<Light>()[0].enabled == false ) 
            {
                resourceAmount = 0;
                foreach (Light lgt in part.FindModelComponents<Light>())
                    lgt.color = new Color (0, 0, 0);
                
                foreach (Renderer emi in part.FindModelComponents<Renderer>())
                    emi.material.SetColor ("_EmissiveColor", new Color (0, 0, 0, 0));
            }
            else {
                foreach (Light lgt in part.FindModelComponents<Light>())
                    lgt.color = new Color (red, green, blue);
                
                foreach (Renderer emi in part.FindModelComponents<Renderer>())
                    emi.material.SetColor ("_EmissiveColor", new Color (red, green, blue, 1));
            }
            part.RequestResource("ElectricCharge", resourceAmount);
        }
    }
}