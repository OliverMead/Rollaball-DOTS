using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor.Tests
{
    partial class SystemScheduleWindowIntegrationTests
    {
        List<string> m_ExpandedNodeNamesToTest =
            new List<string> {
                "Live Link Editor System Group",
                "Scene System Group",
                "Simulation System Group",
                "System Schedule Test Group",
                "Presentation System Group"
                };

        [UnityTest]
        public IEnumerator SystemScheduleWindow_TreeViewFoldingState_PlayToEditorMode()
        {
            yield return new SystemScheduleTestUtilities.UpdateSystemGraph(typeof(SystemScheduleTestSystem1));

            // Play mode
            yield return new EnterPlayMode();
            yield return new SystemScheduleTestUtilities.UpdateSystemGraph(typeof(SystemScheduleTestSystem1));

            var systemTreeViewPlayMode = m_SystemScheduleWindow.rootVisualElement.Q<SystemTreeView>();
            foreach(var item in systemTreeViewPlayMode.m_TreeViewRootItems)
            {
                SystemScheduleTestUtilities.ExpandAllGroupNodes(systemTreeViewPlayMode, item);
            }

            // Editor mode
            yield return new ExitPlayMode();
            var systemTreeViewEditorMode = m_SystemScheduleWindow.rootVisualElement.Q<SystemTreeView>();

            m_DefaultWorld = World.DefaultGameObjectInjectionWorld;
            m_TestSystemGroup = m_DefaultWorld.GetOrCreateSystem<SystemScheduleTestGroup>();
            m_TestSystem1 = m_DefaultWorld.GetOrCreateSystem<SystemScheduleTestSystem1>();
            m_TestSystem2 = m_DefaultWorld.GetOrCreateSystem<SystemScheduleTestSystem2>();
            m_TestSystemGroup.AddSystemToUpdateList(m_TestSystem1);
            m_TestSystemGroup.AddSystemToUpdateList(m_TestSystem2);
            m_DefaultWorld.GetOrCreateSystem<SimulationSystemGroup>().AddSystemToUpdateList(m_TestSystemGroup);

            yield return new SystemScheduleTestUtilities.UpdateSystemGraph(typeof(SystemScheduleTestSystem1));

            var resultList = new List<string>();
            foreach(var item in systemTreeViewPlayMode.m_TreeViewRootItems)
            {
                SystemScheduleTestUtilities.CollectExpandedGroupNodeNames(systemTreeViewEditorMode, item, resultList);
            }

            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(m_ExpandedNodeNamesToTest.Count));
            Assert.That(m_ExpandedNodeNamesToTest, Is.SubsetOf(resultList));
        }

        [UnityTest]
        public IEnumerator SystemScheduleWindow_TreeViewFoldingState_EditorToPlayMode()
        {
            // Editor mode
            yield return new SystemScheduleTestUtilities.UpdateSystemGraph(typeof(SystemScheduleTestSystem1));

            var systemTreeViewEditorMode = m_SystemScheduleWindow.rootVisualElement.Q<SystemTreeView>();
            foreach(var item in systemTreeViewEditorMode.m_TreeViewRootItems)
            {
                SystemScheduleTestUtilities.ExpandAllGroupNodes(systemTreeViewEditorMode, item);
            }

            // Play mode
            yield return new EnterPlayMode();
            yield return new SystemScheduleTestUtilities.UpdateSystemGraph(typeof(SystemScheduleTestSystem1));

            var systemTreeViewPlayMode = m_SystemScheduleWindow.rootVisualElement.Q<SystemTreeView>();
            var resultList = new List<string>();

            foreach(var item in systemTreeViewPlayMode.m_TreeViewRootItems)
            {
                SystemScheduleTestUtilities.CollectExpandedGroupNodeNames(systemTreeViewPlayMode, item, resultList);
            }

            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(m_ExpandedNodeNamesToTest.Count));
            Assert.That(m_ExpandedNodeNamesToTest, Is.SubsetOf(resultList));

            yield return new ExitPlayMode();
        }
    }
}
