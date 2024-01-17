using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using UnityEngine;

namespace LCAmmoCheck.Patches;

[HarmonyPatch(typeof(ShotgunItem))]
sealed class ShotgunItemPatch
{
    private static readonly Dictionary<int, AnimationClip> originalClips = [];

    static AnimatorOverrideController GetOrSetOverrideController(Animator animator)
    {
        if (animator.runtimeAnimatorController is AnimatorOverrideController controller)
        {
            return controller;
        }
        AnimatorOverrideController overrideController = new(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
        return overrideController;
    }
    static IEnumerator CheckAmmoAnimation(ShotgunItem s)
    {
        s.isReloading = true;
        s.shotgunShellLeft.enabled = s.shellsLoaded > 0;
        s.shotgunShellRight.enabled = s.shellsLoaded > 1;

        AnimatorOverrideController overrideController = GetOrSetOverrideController(s.playerHeldBy.playerBodyAnimator);
        int playerAnimatorId = s.playerHeldBy.playerBodyAnimator.GetInstanceID();
        originalClips[playerAnimatorId] = overrideController["ShotgunReloadOneShell"];
        overrideController["ShotgunReloadOneShell"] = LCAmmoCheckPlugin.ShotgunInspectClip!;

        s.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: true);

        yield return new WaitForSeconds(0.3f);

        s.gunAudio.PlayOneShot(LCAmmoCheckPlugin.ShotgunInspectSFX!);
        s.gunAnimator.SetBool("Reloading", value: true);

        yield return new WaitForSeconds(2.05f);
        s.gunAnimator.SetBool("Reloading", value: false);
        yield return new WaitForSeconds(0.25f);
        s.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: false);
        yield return new WaitForSeconds(0.25f);
        originalClips.Remove(playerAnimatorId, out AnimationClip clip);
        overrideController["ShotgunReloadOneShell"] = clip;
        s.isReloading = false;
        yield break;
    }

    private static void CleanUp(Animator animator)
    {
        if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
        {
            if (originalClips.Remove(animator.GetInstanceID(), out AnimationClip? clip))
            {
                overrideController["ShotgunReloadOneShell"] = clip;
            }
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch("StopUsingGun")]
    public static bool StopUsingGunPrefix(ShotgunItem __instance)
    {
        PlayerControllerB playerHeldBy = __instance.playerHeldBy ?? __instance.previousPlayerHeldBy;
        CleanUp(playerHeldBy.playerBodyAnimator);
        return true;
    }


    [HarmonyPrefix]
    [HarmonyPatch("StartReloadGun")]
    public static bool StartReloadGunPrefix(ShotgunItem __instance)
    {
        if (!__instance.ReloadedGun() || __instance.shellsLoaded >= 2)
        {
            if (__instance.gunCoroutine != null)
            {
                __instance.StopCoroutine(__instance.gunCoroutine);
            }
            __instance.gunCoroutine = __instance.StartCoroutine(CheckAmmoAnimation(__instance));
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ItemInteractLeftRight")]
    public static bool ItemInteractLeftRightPrefix(ShotgunItem __instance, bool right)
    {
        // right = true -> Interact (E)
        if (!right)
        {
            return true;
        }
        if (__instance.playerHeldBy.hit.collider != null && __instance.playerHeldBy.hit.collider.tag == "InteractTrigger")
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    public static bool StartPrefix(ShotgunItem __instance)
    {
        string[] toolTips = __instance.itemProperties.toolTips;
        toolTips[1] = "Reload / Check ammo : [E]";
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ShotgunItem), "StartReloadGun")]
    public static IEnumerable<CodeInstruction> StartReloadGunTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && ((MethodInfo)codes[i].operand).Name == "ReloadedGun")
            {
                // replace this with nop
                codes[i - 1].opcode = OpCodes.Nop;
                // Replace the call to ReloadedGun with ldc.i4.0 to load false
                codes[i].opcode = OpCodes.Ldc_I4_1;
                codes[i].operand = null;

                break;
            }
        }

        return codes.AsEnumerable();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ShotgunItem), "ItemInteractLeftRight")]
    public static IEnumerable<CodeInstruction> ItemInteractLeftRightTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("shellsLoaded"))
            {
                codes[i + 1].opcode = OpCodes.Ldc_I4_3;
                break;
            }
        }
        return codes.AsEnumerable();
    }
}
