using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace WCS.API
{
    public interface IWarcraftRace
    {
        string InternalName { get; }
        string DisplayName { get; }
        string Description { get; }
        int Requirement { get; }
        int Level { get; set; }
        int Experience { get; set; }
        int MaxLevel { get; }
        bool IsMaxLevel { get; }
        IWarcraftPlayer Player { get; set; }
        void Load(IWarcraftPlayer player);
        int RequiredExperience { get; }
        void AddExperience(int experience);
        void AddLevels(int levelsToGain);
        IWarcraftSkill GetSkillByName(string name);
        IWarcraftSkill GetSkillByDisplayName(string name);
        IWarcraftSkill[] GetSkills();
        int GetSkillCount();
        int GetUnusedSkillPoints();
        void InvokeEvent(string eventName, GameEvent @event);
        void InvokeAbility(int abilityIndex);
        void InvokeVirtual(string eventName, DynamicHook hookData);
    }
}
