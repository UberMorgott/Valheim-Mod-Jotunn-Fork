using System;
using System.Collections.Generic;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace Jotunn.Utils
{
    internal class ExtEquipment : MonoBehaviour
    {
        private static readonly int RightItemVariantHash = "RightItemVariant".GetStableHashCode();
        private static readonly int RightBackItemVariantHash = "RightBackItemVariant".GetStableHashCode();
        private static readonly int ChestItemVariantHash = "ChestItemVariant".GetStableHashCode();

        private static bool Enabled;

        private static readonly Dictionary<VisEquipment, ExtEquipment> Instances =
            new Dictionary<VisEquipment, ExtEquipment>();

        public static void Enable()
        {
            if (!Enabled)
            {
                Enabled = true;
                Main.Harmony.PatchAll(typeof(ExtEquipment));
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.Awake)), HarmonyPostfix]
        private static void VisEquipment_Awake(VisEquipment __instance)
        {
            if (!__instance.gameObject.TryGetComponent(out ExtEquipment _))
            {
                __instance.gameObject.AddComponent<ExtEquipment>();
            }
        }

        /// <summary>
        ///     Get non-vanilla variant indices from the ZDO
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals)), HarmonyPrefix]
        private static void VisEquipment_UpdateEquipmentVisuals(VisEquipment __instance)
        {
            if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
            {
                if (Instances.TryGetValue(__instance, out var instance))
                {
                    instance.NewRightItemVariant = zdo.GetInt(RightItemVariantHash);
                    instance.NewChestVariant = zdo.GetInt(ChestItemVariantHash);
                    instance.NewRightBackItemVariant = zdo.GetInt(RightBackItemVariantHash);
                }
            }
        }

        private static ExtEquipment CurrentRightHandInstance;
        private static ExtEquipment CurrentRightBackInstance;
        private static ExtEquipment CurrentChestInstance;

        private static int ApplyRightHandHash(int hash, VisEquipment self)
        {
            if (Instances.TryGetValue(self, out CurrentRightHandInstance) && hash != 0 &&
                CurrentRightHandInstance.CurrentRightItemVariant != CurrentRightHandInstance.NewRightItemVariant)
            {
                CurrentRightHandInstance.CurrentRightItemVariant = CurrentRightHandInstance.NewRightItemVariant;
                return hash + CurrentRightHandInstance.CurrentRightItemVariant;
            }
            return hash;
        }

        private static int GetRightHandVariant(int variant) =>
            CurrentRightHandInstance != null ? CurrentRightHandInstance.CurrentRightItemVariant : variant;

        private static int ApplyRightBackHash(int hash, VisEquipment self)
        {
            if (Instances.TryGetValue(self, out CurrentRightBackInstance) && hash != 0 &&
                CurrentRightBackInstance.CurrentRightBackItemVariant != CurrentRightBackInstance.NewRightBackItemVariant)
            {
                CurrentRightBackInstance.CurrentRightBackItemVariant = CurrentRightBackInstance.NewRightBackItemVariant;
                return hash + CurrentRightBackInstance.CurrentRightBackItemVariant;
            }
            return hash;
        }

        private static int GetRightBackVariant(int variant) =>
            CurrentRightBackInstance != null ? CurrentRightBackInstance.CurrentRightBackItemVariant : variant;

        private static int ApplyChestHash(int hash, VisEquipment self)
        {
            if (Instances.TryGetValue(self, out CurrentChestInstance) && hash != 0 &&
                CurrentChestInstance.CurrentChestVariant != CurrentChestInstance.NewChestVariant)
            {
                CurrentChestInstance.CurrentChestVariant = CurrentChestInstance.NewChestVariant;
                return hash + CurrentChestInstance.CurrentChestVariant;
            }
            return hash;
        }

        private static int GetChestVariant(int variant) =>
            CurrentChestInstance != null ? CurrentChestInstance.CurrentChestVariant : variant;

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachItem
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetRightHandEquiped(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>(ApplyRightHandHash);
            }

            // Pass current variant to AttachItem
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(0)))
            {
                c.EmitDelegate<Func<int, int>>(GetRightHandVariant);
            }
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachBackItem
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetBackEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetBackEquiped(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(2)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>(ApplyRightBackHash);
            }

            // Pass current variant to AttachItem
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(2),
                x => x.MatchLdcI4(0)))
            {
                c.EmitDelegate<Func<int, int>>(GetRightBackVariant);
            }
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachArmor
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetChestEquiped(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>(ApplyChestHash);
            }

            // Pass current variant to AttachArmor
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(-1)))
            {
                c.EmitDelegate<Func<int, int>>(GetChestVariant);
            }
        }

        /// <summary>
        ///     Store the variant index of the right hand item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightItem)), HarmonyPrefix]
        private static void VisEquipment_SetRightItem(VisEquipment __instance, int itemHash)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_rightItem != null &&
                !(__instance.m_rightItem == itemHash && instance.MyHumanoid.m_rightItem.m_variant == instance.CurrentRightItemVariant))
            {
                instance.NewRightItemVariant = instance.MyHumanoid.m_rightItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set(RightItemVariantHash, itemHash != 0 ? instance.NewRightItemVariant : 0);
                }
            }
        }

        /// <summary>
        ///     Store the variant index of the right back item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightBackItem)), HarmonyPrefix]
        private static void VisEquipment_SetRightBackItem(VisEquipment __instance, int itemHash)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_hiddenRightItem != null &&
                !(__instance.m_rightBackItem == itemHash && instance.MyHumanoid.m_hiddenRightItem.m_variant == instance.CurrentRightBackItemVariant))
            {
                instance.NewRightBackItemVariant = instance.MyHumanoid.m_hiddenRightItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set(RightBackItemVariantHash, itemHash != 0 ? instance.NewRightBackItemVariant : 0);
                }
            }
        }

        /// <summary>
        ///     Store the variant index of the chest item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestItem)), HarmonyPrefix]
        private static void VisEquipment_SetChestItem(VisEquipment __instance, int itemHash)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_chestItem != null &&
                !(__instance.m_chestItem == itemHash && instance.MyHumanoid.m_chestItem.m_variant == instance.CurrentChestVariant))
            {
                instance.NewChestVariant = instance.MyHumanoid.m_chestItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set(ChestItemVariantHash, itemHash != 0 ? instance.NewChestVariant : 0);
                }
            }
        }

        private Humanoid MyHumanoid;

        private int NewRightItemVariant;
        private int CurrentRightItemVariant;
        private int NewRightBackItemVariant;
        private int CurrentRightBackItemVariant;
        private int NewChestVariant;
        private int CurrentChestVariant;

        private void Awake()
        {
            MyHumanoid = gameObject.GetComponent<Humanoid>();
            Instances.Add(gameObject.GetComponent<VisEquipment>(), this);
        }

        private void OnDestroy()
        {
            Instances.Remove(gameObject.GetComponent<VisEquipment>());
        }
    }
}
