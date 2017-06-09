﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld.Planet;
using RimWorld;
using System.Reflection;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.ohu.ships.main");

            harmony.Patch(AccessTools.Method(typeof(RimWorld.FactionGenerator), "GenerateFactionsIntoWorld"), null, new HarmonyMethod(typeof(HarmonyPatches), "GenerateFactionsIntoWorldPostFix"));

            harmony.Patch(AccessTools.Property(typeof(MapPawns), "AnyPawnBlockingMapRemoval").GetGetMethod(false), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(AnyColonistTameAnimalOrPrisonerOfColonyPostFix)), null);

            harmony.Patch(AccessTools.Property(typeof(TransferableOneWay), "MaxCount").GetGetMethod(false), new HarmonyMethod(typeof(HarmonyPatches), nameof(MaxCountTransferablePostFix)), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.GameEnder), "CheckGameOver"), null, new HarmonyMethod(typeof(HarmonyPatches), "CheckGameOverPostfix"));

            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldSelector), "AutoOrderToTileNow", new Type[] { typeof(Caravan), typeof(int) }), new HarmonyMethod(typeof(HarmonyPatches), "AutoOrderToTileNowPrefix"), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.Scenario), "GenerateIntoMap", new Type[] { typeof(Map) }), new HarmonyMethod(typeof(HarmonyPatches), "GenerateIntoMapPreFix"), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.TransferableOneWayWidget), "AddSection"), new HarmonyMethod(typeof(HarmonyPatches), "AddSectionPrefix"), null);

        }

        public static void AnyColonistTameAnimalOrPrisonerOfColonyPostFix(ref bool __result, MapPawns __instance)
        {
            if (!__result)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                if (map != null)
                {
                    List<Thing> list = map.listerThings.AllThings.FindAll(x => x is ShipBase_Traveling || x is ShipBase);
                    if (list.Count > 0)
                    {
                        __result = true;
                    }
                }
            }
        }

        public static void MaxCountTransferablePostFix(TransferableOneWay __instance)
        {
            Map map = Find.VisibleMap;
            List<ShipBase> ships = DropShipUtility.ShipsOnMap(map);
            for (int i=0; i < ships.Count; i++)
            {
                for (int j=0; j < ships[i].GetDirectlyHeldThings().Count; j++)
                {
                    __instance.things.RemoveAll(x => ships[i].GetDirectlyHeldThings().Contains(x));
                }
            }            
        }

        public static void AddSectionPrefix(TransferableOneWayWidget __instance, string title, IEnumerable<TransferableOneWay> transferables)
        {
            List<TransferableOneWay> tmp = transferables.ToList();
            for (int i = 0; i < tmp.Count; i++)
            {
                Dialog_LoadShipCargo.RemoveExistingTransferable(tmp[i], Find.VisibleMap);
                tmp[i].AdjustTo(0);
            }
        }

        public static void CheckGameOverPostfix()
        {
            List<TravelingShips> travelingShips = Find.WorldObjects.AllWorldObjects.FindAll(x => x is TravelingShips).Cast<TravelingShips>().ToList();
            for (int i=0; i < travelingShips.Count; i++)
            {
                TravelingShips ship = travelingShips[i];
                if (ship.containsColonists)
                {
                    Find.GameEnder.gameEnding = false;
                }
            }
        }

        public static void GenerateFactionsIntoWorldPostFix()
        {
            Log.Message("GeneratingShipTracker");
            ShipTracker shipTracker = (ShipTracker)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipTracker);
            int tile = 0;
            while (!(Find.WorldObjects.AnyWorldObjectAt(tile) || Find.WorldGrid[tile].biome == BiomeDefOf.Ocean))
            {
                tile = Rand.Range(0, Find.WorldGrid.TilesCount);
            }
            shipTracker.Tile = tile;
            Find.WorldObjects.Add(shipTracker);
        }

        public static bool AutoOrderToTileNowPrefix(Caravan c, int tile)
        {
            LandedShip ship = c as LandedShip;
            if (ship != null)
            {
                return false;
            }
            return true;
        }

        public static void GenerateIntoMapPreFix(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            else
            {
                ScenPart_StartWithShip scenPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_StartWithShip) as ScenPart_StartWithShip;
                if (scenPart != null)
                {
                    List<List<Thing>> list = new List<List<Thing>>();
                    foreach (Pawn current in Find.GameInitData.startingPawns)
                    {
                        list.Add(new List<Thing>
                {
                    current
                });
                    }
                    List<Thing> list2 = new List<Thing>();
                    foreach (ScenPart current2 in Find.Scenario.AllParts)
                    {
                        list2.AddRange(current2.PlayerStartingThings());
                    }
                    int num = 0;
                    foreach (Thing current3 in list2)
                    {
                        if (current3.def.CanHaveFaction)
                        {
                            current3.SetFactionDirect(Faction.OfPlayer);
                        }
                        list[num].Add(current3);
                        num++;
                        if (num >= list.Count)
                        {
                            num = 0;
                        }
                    }
                    foreach (List<Thing> current in list)
                    {
                        scenPart.AddToStartingCargo(current);
                    }
                    ScenPart_PlayerPawnsArriveMethod arrivalPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_PlayerPawnsArriveMethod) as ScenPart_PlayerPawnsArriveMethod;
                    if (arrivalPart != null)
                    {
                        Find.Scenario.RemovePart(arrivalPart);
                    }
                }
            }
        }
        
        
    }
}
