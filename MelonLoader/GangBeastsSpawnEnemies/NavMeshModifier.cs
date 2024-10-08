﻿using MelonLoader;
using System.Collections.Generic;

namespace UnityEngine.AI
{
    [RegisterTypeInIl2Cpp]
    public class NavMeshModifier : MonoBehaviour
    {
        bool m_OverrideArea;
        public bool overrideArea { get { return m_OverrideArea; } set { m_OverrideArea = value; } }

        int m_Area;
        public int area { get { return m_Area; } set { m_Area = value; } }

        bool m_IgnoreFromBuild;
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild; } set { m_IgnoreFromBuild = value; } }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        List<int> m_AffectedAgents = new List<int>(new int[] { -1 });    // Default value is All

        static readonly List<NavMeshModifier> s_NavMeshModifiers = new();

        public static List<NavMeshModifier> activeModifiers
        {
            get { return s_NavMeshModifiers; }
        }

        void OnEnable()
        {
            if (!s_NavMeshModifiers.Contains(this))
                s_NavMeshModifiers.Add(this);
        }

        void OnDisable()
        {
            s_NavMeshModifiers.Remove(this);
        }

        public bool AffectsAgentType(int agentTypeID)
        {
            if (m_AffectedAgents.Count == 0)
                return false;
            if (m_AffectedAgents[0] == -1)
                return true;
            return m_AffectedAgents.IndexOf(agentTypeID) != -1;
        }
    }
}