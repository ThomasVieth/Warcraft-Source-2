using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System;
using System.Collections.Generic;

namespace WCS
{
    public class Restrictions
    {
        private static Dictionary<IntPtr, List<string>> Players = new Dictionary<IntPtr, List<string>>();

        public static List<string> ALL_WEAPONS = new List<string> {
            "weapon_ak47",
            "weapon_aug",
            "weapon_awp",
            "weapon_bizon",
            "weapon_c4",
            "weapon_cz75a",
            "weapon_deagle",
            "weapon_decoy",
            "weapon_elite",
            "weapon_famas",
            "weapon_fiveseven",
            "weapon_flashbang",
            "weapon_g3sg1",
            "weapon_galilar",
            "weapon_glock",
            "weapon_healthshot",
            "weapon_hegrenade",
            "weapon_hkp2000",
            "weapon_incgrenade",
            "weapon_knife",
            "weapon_m249",
            "weapon_m4a1",
            "weapon_m4a1_silencer",
            "weapon_mac10",
            "weapon_mag7",
            "weapon_molotov",
            "weapon_mp5sd",
            "weapon_mp7",
            "weapon_mp9",
            "weapon_negev",
            "weapon_nova",
            "weapon_p250",
            "weapon_p90",
            "weapon_revolver",
            "weapon_sawedoff",
            "weapon_scar20",
            "weapon_sg556",
            "weapon_smokegrenade",
            "weapon_ssg08",
            "weapon_tagrenade",
            "weapon_taser",
            "weapon_tec9",
            "weapon_ump45",
            "weapon_usp_silencer",
            "weapon_xm1014"
        };

        private static WCS _plugin = null;

        public Restrictions()
        {
            if (_plugin == null)
            {
                Server.PrintToConsole($"{WCS.Instance.ModuleChatPrefix} Something went wrong loading Restriction System.");
            }
        }

        public Restrictions(WCS plugin)
        {
            _plugin = plugin;
        }

        public void Initialize(bool hotLoad)
        {
            MemoryFunctionWithReturn<CCSPlayer_WeaponServices, CBasePlayerWeapon, bool> CCSPlayer_WeaponServices_CanUseFunc = new(GameData.GetSignature("CCSPlayer_WeaponServices_CanUse"));
            CCSPlayer_WeaponServices_CanUseFunc.Hook(OnWeaponCanUse, HookMode.Pre);
            
        }

        private HookResult OnWeaponCanUse(DynamicHook hook)
        {
            var weaponservices = hook.GetParam<CCSPlayer_WeaponServices>(0);
            var clientweapon = hook.GetParam<CBasePlayerWeapon>(1);

            IntPtr identifier = weaponservices!.Pawn.Value.Controller.Value!.Handle;

            if (identifier == IntPtr.Zero) return HookResult.Continue;

            if (!Players.ContainsKey(identifier))
            {
                Players.Add(identifier, new List<string>());
            }

            if (Players[identifier].Contains(clientweapon.DesignerName))
            {
                hook.SetReturn(false);
                return HookResult.Handled;
            }

            return HookResult.Continue;
        }

        public void Restrict(CCSPlayerController controller, List<string> weapons)
        {
            if (!Players.ContainsKey(controller.Handle))
            {
                Players.Add(controller.Handle, new List<string>());
            }

            Players[controller.Handle].AddRange(weapons);
        }

        public void Unrestrict(CCSPlayerController controller, List<string> weapons)
        {
            if (!Players.ContainsKey(controller.Handle))
            {
                Players.Add(controller.Handle, new List<string>());
            }

            foreach (string weapon in weapons)
            {
                Players[controller.Handle].Remove(weapon);
            }
        }

        public void Clear(CCSPlayerController controller)
        {
            if (!Players.ContainsKey(controller.Handle))
            {
                Players.Add(controller.Handle, new List<string>());
            }
            else
            {
                Players[controller.Handle].Clear();
            }
        }

        public List<string> GetAllWeapons()
        {
            return ALL_WEAPONS;
        }
    }
}
