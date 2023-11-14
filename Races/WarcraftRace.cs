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
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using System.Reflection.Emit;
using System.Linq;
using System.ComponentModel;

namespace WCS.Races
{
    public abstract class WarcraftSkill
    {
        // ATTRIBUTES
        public abstract string InternalName { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        public int Level;

        abstract public int MaxLevel { get; }
        abstract public int RequiredLevel { get; }

        public WarcraftPlayer Player { get; set; }

        // CONSTRUCTOR
        public WarcraftSkill(int level = 0)
        {
            Level = level;
        }

        // LOADING
        public abstract void Load(WarcraftPlayer player);

        // EVENTS
        private Dictionary<string, Action<GameEvent>> _eventHandlers = new();
        private Dictionary<int, Action> _abilityHandlers = new Dictionary<int, Action>();
        protected void HookEvent<T>(string eventName, Action<GameEvent> handler) where T : GameEvent
        {
            _eventHandlers[eventName] = handler;
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
    }

    public abstract class WarcraftRace
    {
        // ATTRIBUTES
        public abstract string InternalName { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public int Requirement => 0;

        public int Level = 0;
        public int Experience = 0;

        abstract public int MaxLevel { get; }

        public bool IsMaxLevel => (Level == MaxLevel);

        public WarcraftPlayer Player { get; set; }

        private Dictionary<string, WarcraftSkill> _skills = new Dictionary<string, WarcraftSkill>();

        // LOADING
        public abstract void Load(WarcraftPlayer player);

        // EXPERIENCE
        public int RequiredExperience { get { return Level == MaxLevel ? 9999999 : 80 * (Level + 1); } }
        public void AddExperience(int experience)
        {
            int a = experience;
            while (Experience + a > RequiredExperience)
            {
                a -= RequiredExperience - Experience;
                AddLevels(1);
                Experience = 0;
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
        public WarcraftSkill GetSkillByName(string name)
        {
            return _skills[name];
        }
        public WarcraftSkill GetSkillByDisplayName(string name)
        {
            return _skills.Where(x => name.Equals(x.Value.DisplayName)).First().Value;
        }
        public WarcraftSkill[] GetSkills()
        {
            return _skills.Values.ToArray();
        }
        public int GetSkillCount()
        {
            return _skills.Values.Count;
        }
        protected void AddSkill(WarcraftSkill skill)
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
        private Dictionary<int, Action> _abilityHandlers = new Dictionary<int, Action>();
        protected void HookEvent<T>(string eventName, Action<GameEvent> handler) where T : GameEvent
        {
            _eventHandlers[eventName] = handler;
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

            foreach (WarcraftSkill skill in _skills.Values)
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

            foreach (WarcraftSkill skill in _skills.Values)
            {
                skill.InvokeAbility(abilityIndex);
            }
        }
    }
}