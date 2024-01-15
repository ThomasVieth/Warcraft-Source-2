using WCS.API;
using WCS.Races;

namespace WCS.Core.Races.Races
{
    public class SkillEarthquake : WarcraftSkill
    {
        public override string InternalName => "earthquake";
        public override string DisplayName => "Earthquake";
        public override string Description => $"0 - 12% chance upon damaging an enemy to shake them.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillBroomOfVelocity : WarcraftSkill
    {
        public override string InternalName => "broom_of_velocity";
        public override string DisplayName => "Broom Of Velocity";
        public override string Description => $"Grants you 10-34% more movement speed.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

        }
    }

    public class SkillWeaponOfTheSorcerer : WarcraftSkill
    {
        public override string InternalName => "weapon_sorc";
        public override string DisplayName => "Weapon of the Sorcerer";
        public override string Description => $"30-60% Chance to receive a Deagle and M4A4.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 0;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class SkillLiftOff : WarcraftSkill
    {
        public override string InternalName => "lift_off";
        public override string DisplayName => "Lift Off";
        public override string Description => $"You can fly, while flying you\'ll recieve 8 - 20 extra health. Ultimate.";

        public override int MaxLevel => 6;
        public override int RequiredLevel => 8;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;
        }
    }

    public class RaceArchmageProudmore : WarcraftRace
    {
        public override string InternalName => "archmage_proudmore";
        public override string DisplayName => "Archmage Proudmore";
        public override string Description => "Archmage Proudmore";

        public override int MaxLevel => 24;

        public override void Load(IWarcraftPlayer player)
        {
            Player = player;

            AddSkill(new SkillEarthquake());
            AddSkill(new SkillBroomOfVelocity());
            AddSkill(new SkillWeaponOfTheSorcerer());
            AddSkill(new SkillLiftOff());
        }
    }
}
