using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using GameNetcodeStuff;

namespace LCAmmoCheck.Patches;

[HarmonyPatch(typeof(ShotgunItem))]
sealed class ShotgunItemPatch
{
    static IEnumerator CheckAmmoAnimation(ShotgunItem s)
    {
        s.isReloading = true;
        switch (s.shellsLoaded)
        {
            case 0:
                s.shotgunShellLeft.enabled = false;
                s.shotgunShellRight.enabled = false;
                break;
            case 1:
                s.shotgunShellLeft.enabled = true;
                s.shotgunShellRight.enabled = false;
                break;
            default:
                s.shotgunShellLeft.enabled = true;
                s.shotgunShellRight.enabled = true;
                break;
        }
        // Start hand animation
        s.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: true);
        yield return new WaitForSeconds(0.3f);
        // Start gun animation and sound
        s.gunAudio.clip = s.gunReloadSFX;
        s.gunAudio.Play();
        s.gunAnimator.SetBool("Reloading", value: true);
        yield return new WaitForSeconds(0.45f);
        // Stop gun reload sound at time of shell insertion
        s.gunAudio.Stop();
        // Stop hand animation at time of shell insertion
        s.playerHeldBy.playerBodyAnimator.speed = 0.2f;
        // Start gun reload sound at time after shell insertion
        s.gunAudio.time = 0.70f;
        s.gunAudio.Play();

        yield return new WaitForSeconds(0.95f);
        s.playerHeldBy.playerBodyAnimator.speed = 0.6f;
        s.playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: false);
        s.gunAnimator.SetBool("Reloading", value: false);

        yield return new WaitForSeconds(0.3f);
        s.gunAudio.time = 0.0f;
        s.gunAudio.Stop();
        s.playerHeldBy.playerBodyAnimator.speed = 1.0f;
        s.isReloading = false;
        s.ReloadGunEffectsServerRpc(start: false);
        yield break;
    }

    [HarmonyPrefix]
    [HarmonyPatch("StopUsingGun")]
    public static bool StopUsingGunPrefix(ShotgunItem __instance)
    {
        // playerHeldBy is null when the gun is dropped so we use previousPlayerHeldBy
        PlayerControllerB playerHeldBy = __instance.playerHeldBy ?? __instance.previousPlayerHeldBy;
        if (playerHeldBy == null)
        {
            return true;
        }
        if (playerHeldBy.playerBodyAnimator.speed != 1.0f)
        {
            playerHeldBy.playerBodyAnimator.speed = 1.0f;
        }
        if (__instance.gunAudio.time != 0.0f)
        {
            __instance.gunAudio.time = 0.0f;
        }
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
