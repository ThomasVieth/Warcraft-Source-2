/*
 *  This file is part of CounterStrikeSharp.
 *  CounterStrikeSharp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  CounterStrikeSharp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with CounterStrikeSharp.  If not, see <https://www.gnu.org/licenses/>. *
 */

using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Modules.Events;
using System.Linq;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using WCS.API;

namespace WCS.Races
{
    public abstract class WarcraftSkill : IWarcraftSkill
    {
        // ATTRIBUTES
        public abstract string InternalName { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        public int Level { get; set; }

        abstract public int MaxLevel { get; }
        abstract public int RequiredLevel { get; }

        public IWarcraftPlayer Player { get; set; }

        // CONSTRUCTOR
        public WarcraftSkill(int level = 0)
        {
            Level = level;
        }

        // LOADING
        public abstract void Load(IWarcraftPlayer player);

        // EVENTS
        private Dictionary<string, Action<GameEvent>> _eventHandlers = new();
        private Dictionary<string, Action<DynamicHook>> _virtualHandlers = new();
        private Dictionary<int, Action> _abilityHandlers = new Dictionary<int, Action>();
        protected void HookEvent<T>(string eventName, Action<GameEvent> handler) where T : GameEvent
        {
            _eventHandlers[eventName] = handler;
        }

        protected void HookVirtual(string eventName, Action<DynamicHook> handler)
        {
            _virtualHandlers[eventName] = handler;
        }

        protected void HookAbility(int abilityIndex, Action handler)
        {
            _abilityHandlers[abilityIndex] = handler;
        }

        public void InvokeEvent(string eventName, GameEvent @event)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName].Invoke(@event);
            }
        }

        public void InvokeAbility(int abilityIndex)
        {
            if (_abilityHandlers.ContainsKey(abilityIndex))
            {
                _abilityHandlers[abilityIndex].Invoke();
            }
        }

        public void InvokeVirtual(string eventName, DynamicHook hookData)
        {
            if (_virtualHandlers.ContainsKey(eventName))
            {
                _virtualHandlers[eventName].Invoke(hookData);
            }
        }
    }

    public abstract class WarcraftRace : IWarcraftRace
    {
        // ATTRIBUTES
        public abstract string InternalName { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public int Requirement => 0;

        public int Level { get; set; } = 0;
        public int Experience { get; set; } = 0;

        abstract public int MaxLevel { get; }

        public bool IsMaxLevel => (Level == MaxLevel);

        public IWarcraftPlayer Player { get; set; }

        private Dictionary<string, IWarcraftSkill> _skills = new Dictionary<string, IWarcraftSkill>();

        // LOADING
        public abstract void Load(IWarcraftPlayer player);

        // EXPERIENCE
        public int RequiredExperience { get { return Level == MaxLevel ? 9999999 : 80 * (Level + 1); } }
        public void AddExperience(int experience)
        {
            int a = experience;
            int l = Level;
            while (Experience + a > RequiredExperience)
            {
                a -= RequiredExperience - Experience;
                AddLevels(1);
                Experience = 0;
            }

            if (l < Level)
            {
                if (!Player.Controller.IsBot)
                    WCS.Instance.ShowSkillPointMenu(Player);
                else
                {
                    IWarcraftSkill availableSkill = GetSkills().FirstOrDefault<IWarcraftSkill>((skill) => skill.Level < skill.MaxLevel, null);
                    if (availableSkill != null)
                        availableSkill.Level += 1;
                }
            }

            Experience += a;
            if (Experience > RequiredExperience)
            {
                Experience = RequiredExperience;
            }
        }
        public void AddLevels(int levelsToGain)
        {
            int newLevel = Level + levelsToGain;

            if (newLevel > MaxLevel)
            {
                newLevel = MaxLevel;
            }

            Level = newLevel;
        }

        // SKILL MANAGEMENT
        public IWarcraftSkill GetSkillByName(string name)
        {
            return _skills[name];
        }
        public IWarcraftSkill GetSkillByDisplayName(string name)
        {
            return _skills.Where(x => name.Equals(x.Value.DisplayName)).First().Value;
        }
        public IWarcraftSkill[] GetSkills()
        {
            return _skills.Values.ToArray();
        }
        public int GetSkillCount()
        {
            return _skills.Values.Count;
        }
        protected void AddSkill(IWarcraftSkill skill)
        {
            skill.Load(Player);
            _skills.Add(skill.InternalName, skill);
        }
        public int GetUnusedSkillPoints()
        {
            int usedSkillPoints = Level - _skills.Values.Sum(item => item.Level);
            return usedSkillPoints;
        }

        // EVENTS
        private Dictionary<string, Action<GameEvent>> _eventHandlers = new();
        private Dictionary<string, Action<DynamicHook>> _virtualHandlers = new();
        private Dictionary<int, Action> _abilityHandlers = new Dictionary<int, Action>();
        protected void HookEvent<T>(string eventName, Action<GameEvent> handler) where T : GameEvent
        {
            _eventHandlers[eventName] = handler;
        }

        protected void HookVirtual(string eventName, Action<DynamicHook> handler)
        {
            _virtualHandlers[eventName] = handler;
        }

        protected void HookAbility(int abilityIndex, Action handler)
        {
            _abilityHandlers[abilityIndex] = handler;
        }

        public void InvokeEvent(string eventName, GameEvent @event)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName].Invoke(@event);
            }

            foreach (IWarcraftSkill skill in _skills.Values)
            {
                skill.InvokeEvent(eventName, @event);
            }
        }

        public void InvokeAbility(int abilityIndex)
        {
            if (_abilityHandlers.ContainsKey(abilityIndex))
            {
                _abilityHandlers[abilityIndex].Invoke();
            }

            foreach (IWarcraftSkill skill in _skills.Values)
            {
                skill.InvokeAbility(abilityIndex);
            }
        }

        public void InvokeVirtual(string eventName, DynamicHook hookData)
        {
            if (_virtualHandlers.ContainsKey(eventName))
            {
                _virtualHandlers[eventName].Invoke(hookData);
            }

            foreach (IWarcraftSkill skill in _skills.Values)
            {
                skill.InvokeVirtual(eventName, hookData);
            }
        }
    }
}