﻿using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using RimWorld;
using Verse;

namespace CombatEffectsCE
{
    public enum AmmoType
    {
        HP = 0,
        FMJ,
        AP,
        API,
        HE,
        CHARGED,
        CONC,
        ION,
        SABOT,
        EMP,
        BEAN,
        BUCK,
        SLUG,
        UNDEFINED
    }

    public enum Caliber
    {
        CAL_12x64CH = 0,
        CAL_5x35CH,
        CAL_6x24CH,
        CAL_8x35CH,
        CAL_30x29,
        CAL_40x46,
        CAL_145x114,
        CAL_50,
        CAL_44M,
        CAL_45ACP,
        CAL_45C,
        CAL_9x19,
        CAL_303,
        CAL_545x39,
        CAL_556x45,
        CAL_762x39,
        CAL_762x51,
        CAL_762x54,
        CAL_12G,
        CAL_410B,
        UNDEFINED
    }

    public enum CaliberCategory
    {
        SMALL = 0,
        MEDIUM,
        LARGE,
        ANTIMAT
    }

    public enum Material
    {
        WOOD = 0,
        SOFTSTONE,
        HARDSTONE,
        SOFTMETAL,
        STEEL,
        PLASTEEL,
        URANIUM,
        MEAT,
        UNDEFINED,
        IGNORABLE
    }

    public enum ImpactType
    {
        STOP = 0,
        PEN,
        RICOCHET
    }

    public static class ImpactHelper
    {
        private static readonly List<Caliber> small = new List<Caliber>
            {Caliber.CAL_9x19, Caliber.CAL_45C, Caliber.CAL_45ACP};

        private static readonly List<Caliber> medium = new List<Caliber>
            {Caliber.CAL_762x39, Caliber.CAL_556x45, Caliber.CAL_545x39, Caliber.CAL_44M};

        private static readonly List<Caliber> large = new List<Caliber>
            {Caliber.CAL_12G, Caliber.CAL_762x54, Caliber.CAL_762x51};

        private static readonly List<Caliber> antimat = new List<Caliber> {Caliber.CAL_145x114, Caliber.CAL_50};


        private static readonly List<Material> materialOrder = new List<Material>
        {
            Material.IGNORABLE,
            Material.WOOD,
            Material.SOFTSTONE,
            Material.HARDSTONE,
            Material.SOFTMETAL,
            Material.STEEL,
            Material.PLASTEEL,
            Material.URANIUM,
            Material.MEAT,
            Material.UNDEFINED
        };

        private static readonly List<CaliberCategory> caliberCategoryOrder = new List<CaliberCategory>
            {CaliberCategory.SMALL, CaliberCategory.MEDIUM, CaliberCategory.LARGE, CaliberCategory.ANTIMAT};

        // This encompasses all threeparamteres for every caliber material matchup. [Material, CaliberCatergory, (BaseChance, HighestChance, HP threshold)]
        // For example the [2,2,:] cell tells us that : If the hitThing is at 100% health We have a 20% to pen, and at 60%HP we have 80% to pen.
        private static readonly float[,,] penChanceParamTable =
        {
            {{100, 100, 100}, {100, 100, 100}, {100, 100, 100}, {100, 100, 100}}, //Ignorable
            {{10, 80, 50}, {20, 80, 75}, {30, 80, 90}, {100, 100, 100}}, //Wood
            {{5, 80, 35}, {10, 80, 50}, {30, 80, 75}, {100, 100, 100}}, //SoftStone
            {{0, 80, 15}, {5, 80, 30}, {20, 80, 60}, {40, 80, 90}}, //HardStone
            {{0, 80, 20}, {15, 80, 20}, {25, 80, 50}, {60, 80, 85}}, //SoftMetal
            {{0, 80, 10}, {0, 80, 15}, {0, 80, 20}, {60, 80, 75}}, //Steel
            {{0, 10, 0}, {0, 20, 10}, {0, 30, 20}, {50, 80, 40}}, //Plasteel
            {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {30, 50, 25}}, //Uranium
            {{20, 20, 0}, {30, 30, 0}, {35, 35, 0}, {80, 80, 0}}, //Meat
            {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {0, 0, 0}} //UNDEFINED
        };

        private static readonly float[,,] penChanceAPModifierTable =
        {
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}}, //Ignorable
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{20, 0, 10}, {20, 0, 10}, {20, 0, 10}, {0, 0, 0}},
            {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {0, 0, 0}},
            {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {0, 0, 0}}
        };

        private static Material DetermineMaterial(Thing hitThing)
        {
            string label = null;
            if (!hitThing.def.MadeFromStuff)
            {
                //Log.Message($"Hit a {hitThing.Label}");
                if (hitThing is Plant)
                {
                    return hitThing.def.ingestible?.foodType == FoodTypeFlags.Tree ? Material.WOOD : Material.IGNORABLE;
                }

                if (hitThing.def.costList != null)
                {
                    hitThing.def.costList.SortByDescending(listItem => listItem.count);
                    var mostlyMadeOf = hitThing.def.costList.First().thingDef;
                    //Log.Message($"Hit {hitThing.Label}, mostly made of {mostlyMadeOf.label}");
                    label = mostlyMadeOf.label;
                }

                if (string.IsNullOrEmpty(label) && hitThing.SmeltProducts(1f).Any())
                {
                    var mostlyMadeOf = hitThing.SmeltProducts(1f).First();
                    //Log.Message($"Hit {hitThing.Label}, will smelt to {mostlyMadeOf.def.label}");
                    label = mostlyMadeOf.def.label;
                }

                if (string.IsNullOrEmpty(label))
                {
                    return Material.UNDEFINED;
                }
            }
            else
            {
                label = hitThing.Stuff.label;
            }

            //Log.Message($"Hit stuff label : {hitThing.Stuff.label}");
            var returnValue = Material.UNDEFINED;
            switch (label)
            {
                case "wood":
                    return Material.WOOD;
                case "gold":
                case "silver":
                    return Material.SOFTMETAL;
                case "steel":
                    return Material.STEEL;
                case "plasteel":
                    return Material.PLASTEEL;
                case "uranium":
                    return Material.URANIUM;
                case "jade":
                case "jade blocks":
                case "granite":
                case "granite blocks":
                case "slate":
                case "slate blocks":
                    return Material.HARDSTONE;
                case "sandstone":
                case "sandstone blocks":
                case "limestone":
                case "limestone blocks":
                case "marble":
                case "marble blocks":
                    return Material.SOFTSTONE;
            }

            //Log.Message($"Hit {hitThing.Stuff.label}, unrecognised stuff");
            return returnValue;

            //Log.Message($"Hit something {hitThing.Label}");
        }

        private static CaliberCategory DeterminetCaliberCategory(Caliber caliber)
        {
            if (small.Contains(caliber))
            {
                return CaliberCategory.SMALL;
            }

            if (medium.Contains(caliber))
            {
                return CaliberCategory.MEDIUM;
            }

            if (large.Contains(caliber))
            {
                return CaliberCategory.LARGE;
            }

            if (antimat.Contains(caliber))
            {
                return CaliberCategory.ANTIMAT;
            }

            // if unknown
            return CaliberCategory.SMALL;
        }

        private static float ComputeEnergyRemainingAfterPen(float exponent, float scale, float limit, float score)
        {
            /*
             * This is a power function. 'Exponents' smaller than one raise the function above the linear values that make sense (0,...,2]
             * And the 'scale' make a kind of ceiling to the function. Value [0,1]
             * Limit is the chance of penetration. I use as a limit. If we score at the limit that means we barely penetrated and lost all the energy.
             * The lower we score (the farther we are from this barrier) the more energy we manage to preserve.
             */
            if (exponent == 1f)
            {
                return (limit - score) / limit * scale;
            }

            return (float) Math.Pow((limit - score) / limit, exponent) * scale;
        }

        private static bool ConsideredAPType(AmmoType ammoType)
        {
            return ammoType == AmmoType.AP || ammoType == AmmoType.API || ammoType == AmmoType.SLUG ||
                   ammoType == AmmoType.SABOT;
        }

        /*
         * Determine if impact resulted in bullet stop or penetration.
         * The main decision if based on a chance table of calibers and ammotypes vs materials.
         * Then if the bullet penetrates an energy loss is computed based on the margin by the bullet got through.
         * In theory this favors higher base penetration chances and explicitly favors AP type bullets.
         * The energy conservation function is skewed in favor of AP bullets by having a lower exponent for the power function.
         */
        public static ImpactType DetermineImpactType(BulletCESparky bullet, Thing hitThing, ref float energy,
            bool deflectedByPawn = false)
        {
            if (hitThing == null)
            {
                // we hit ground
                // for now just stop.
                energy = 0f;
                return ImpactType.STOP;
            }

            var bulletProps = bullet.projectileProperties;
            var calCat = DeterminetCaliberCategory(bulletProps.caliber);
            switch (hitThing)
            {
                case Pawn when deflectedByPawn:
                    // we were deflected by armor or shield
                    // for now just stop.
                    energy = 0f;
                    return ImpactType.STOP;
                case Pawn:
                {
                    var caliberIndex = caliberCategoryOrder.FindIndex(cat => cat == calCat);
                    var penChance = penChanceParamTable[7, caliberIndex, 0];
                    //Log.Message($"Pawn hit, not deflected. Penetration chance {penChance}");

                    var score = Rand.Value;
                    penChance = penChance * (energy * 0.01f) * 0.01f;
                    if (penChance > 0f && (penChance >= 1f || score < penChance))
                    {
                        // Really energy loss should be computed around here. But for now I'll use a fix energy loss within the Bullet Impact function.
                        //Log.Message("Pawn hit, bullet went through.");
                        var exponent = 1f;
                        var maxEnergy = 0.8f;
                        if (ConsideredAPType(bulletProps.ammoType))
                        {
                            exponent = 0.7f;
                            maxEnergy = 0.9f;
                        }

                        var limit = penChance; // Use penchance as a limit of total energy loss
                        energy *= ComputeEnergyRemainingAfterPen(exponent, maxEnergy, limit, score);
                        return ImpactType.PEN;
                    }

                    break;
                }
                default:
                {
                    var percentage_HP = (float) hitThing.HitPoints / hitThing.MaxHitPoints;
                    //Log.Message($"Hit thing HP percentage : {percentage_HP}");

                    var thingMat = DetermineMaterial(hitThing);
                    if (thingMat == Material.UNDEFINED)
                    {
                        //Log.Message($"{hitThing.Label} if made of UNDEFINED material.");
                        energy = 0f;
                        return ImpactType.STOP;
                    }

                    var indices = new[]
                    {
                        materialOrder.FindIndex(mat => mat == thingMat), caliberCategoryOrder.FindIndex(
                            cat => cat == calCat)
                    };

                    var basePenChance = penChanceParamTable[indices[0], indices[1], 0];
                    var highPenChance = penChanceParamTable[indices[0], indices[1], 1];
                    var penChanceThreshold = penChanceParamTable[indices[0], indices[1], 2];

                    //Log.Message($"Params to pen function : {basePenChance} {highPenChance} {penChanceThreshold}");

                    if (bulletProps.ammoType == AmmoType.AP || bulletProps.ammoType == AmmoType.API ||
                        bulletProps.ammoType == AmmoType.SLUG || bulletProps.ammoType == AmmoType.SABOT)
                    {
                        basePenChance += penChanceAPModifierTable[indices[0], indices[1], 0];
                        highPenChance += penChanceAPModifierTable[indices[0], indices[1], 1];
                        penChanceThreshold += penChanceAPModifierTable[indices[0], indices[1], 2];
                        //Log.Message($"AP modified Params to pen function : {basePenChance} {highPenChance} {penChanceThreshold}");
                    }

                    float penChance;

                    // NOTE : Chances are stored in nominal percentages. So 80% is 80 and not 0.8. Easier to type.
                    if (percentage_HP <= penChanceThreshold * 0.01f)
                    {
                        //Log.Message("Maximum penchance applied");
                        penChance = highPenChance;
                    }
                    else
                    {
                        //Linear function 
                        penChance = basePenChance +
                                    ((1 - percentage_HP) / (1 - penChanceThreshold) * (highPenChance - basePenChance));
                        //Log.Message($"Interpolated penChance : {penChance}");
                    }

                    penChance += Rand.Gaussian(0f, 5f);
                    penChance = penChance * (energy * 0.01f) * 0.01f;
                    var score = Rand.Value;
                    if (penChance > 0f && (penChance >= 1f || score < penChance))
                    {
                        //Log.Message("Stuff penetrated.");

                        var exponent = 1f;
                        var maxEnergy = 0.8f;
                        if (ConsideredAPType(bulletProps.ammoType))
                        {
                            exponent = 0.5f;
                            maxEnergy = 0.9f;
                        }

                        var limit = penChance; // Use penchance as a limit of total energy loss
                        energy *= ComputeEnergyRemainingAfterPen(exponent, maxEnergy, limit, score);
                        return ImpactType.PEN;
                    }
                    // TODO : The penetration should use the angle of impact. Also here should come the Ricoche computation}

                    //Log.Message("Stuff stopped the bullet");
                    energy = 0f;
                    return ImpactType.STOP;
                }
            }

            // If nothing else. Just stop.
            energy = 0f;
            return ImpactType.STOP;
        }
    }
}