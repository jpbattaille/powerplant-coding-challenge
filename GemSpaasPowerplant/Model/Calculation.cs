﻿using System.Collections.Immutable;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace GemSpaasPowerplant.Model
{

    public class Calculation : ICalculation
    {
        private PowerPlants myOrderedPowerPlants;
        private Fuels fuels;
        private int load;
        public Calculation()
        {
            
        }
        private IEnumerable<PowerLoad> GeneratePlan()
        {
            myOrderedPowerPlants.UpdateCosts(fuels);
            myOrderedPowerPlants.Sort();// uses the implemented CompareTo method to order by merit
            int pMinTotal = 0;
            int RemainingLoadToFulfill;
            int deltaPMin;
            int i = 0;
            int missingPMin = 0;
            while (i < myOrderedPowerPlants.Count)
            {
                PowerPlant powerplant = myOrderedPowerPlants.GetPlant(i);
                RemainingLoadToFulfill = load - myOrderedPowerPlants.MatchedLoad();
                deltaPMin = load - myOrderedPowerPlants.PMinTotal();

                if (RemainingLoadToFulfill == 0) //load is  fulfilled
                {
                    break;
                }
              
                if (RemainingLoadToFulfill > 0) //still load to compensate, select max extra power from this pp
                {
                    if (powerplant.pmin > deltaPMin)
                    {
                        powerplant.p = 0; //not selected, too much PMin - optimization required
                                          // maybe need to unselect previous big gasfired 
                        i++;
                        continue;
                    }

                    powerplant.p = (int)Math.Min(RemainingLoadToFulfill, powerplant.availablePMax);
                    if (powerplant.pmin > RemainingLoadToFulfill) // not enough power to be generated by this pp, remove some from previous
                    {
                        powerplant.p = powerplant.pmin;
                        missingPMin =  powerplant.pmin - RemainingLoadToFulfill;
                        i--;
                        continue;
                    }
                    i++;
                    continue;
                }


                if (RemainingLoadToFulfill < 0) //need to drop the power of  plant
                {
                    int deltaPower = powerplant.p - powerplant.pmin;
                    if (deltaPower >= missingPMin)
                    {
                        powerplant.p -= missingPMin;
                        missingPMin = 0;
                        i++;
                        continue;
                    }
                    else
                    {
                        powerplant.p = powerplant.pmin;
                        missingPMin -= deltaPower;
                        i--;
                        continue;
                    }
                }
            }

            return myOrderedPowerPlants.GetAll().Select(pp => new PowerLoad(pp));

        }



        public IEnumerable<PowerLoad> GetProductionPlan(payload thePayload)
        {
            this.myOrderedPowerPlants = new PowerPlants(thePayload.powerplants);
            this.fuels = thePayload.fuels;
            this.load = thePayload.load;
            return GeneratePlan();
        }
    }
}
