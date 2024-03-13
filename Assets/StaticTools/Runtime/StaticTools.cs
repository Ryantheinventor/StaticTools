using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace StaticTools 
{
    internal static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void HookIntoPlayerLoop() 
        {
            AddToPlayerLoop("Update", 0, StaticUpdate.GetPlayerLoopSystem());
            AddToPlayerLoop("Update", 1, StaticCoroutines.GetPlayerLoopSystem());
        }
        
        public static void AddToPlayerLoop(string targetSubSystem, int targetIndex, PlayerLoopSystem newSystem)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem foundTargetSubSystem = new PlayerLoopSystem();
            int systemIndex = 0;
            bool foundSystem = false;
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type.Name == targetSubSystem)
                {
                    foundTargetSubSystem = playerLoop.subSystemList[i];
                    foundSystem = true;
                    systemIndex = i;
                    break;
                }
            }
            if (!foundSystem)
            {
                Debug.LogError($"Could not find {targetSubSystem} subsystem, {newSystem.type.Name} will not be configured");
                return;
            }
            var tempList = new List<PlayerLoopSystem>(foundTargetSubSystem.subSystemList);

            tempList.Insert(Mathf.Clamp(targetIndex, 0, tempList.Count), newSystem);

            foundTargetSubSystem.subSystemList = tempList.ToArray();

            playerLoop.subSystemList[systemIndex] = foundTargetSubSystem;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }

    public static class StaticUpdate
    {
        public delegate void UpdateDelegate();

        private static List<UpdateDelegate> updates = new List<UpdateDelegate>();

        internal static PlayerLoopSystem GetPlayerLoopSystem() 
        {
            return new PlayerLoopSystem()
            {
                type = typeof(StaticUpdate),
                updateDelegate = Update
            };
        }

        public static void AddUpdate(UpdateDelegate update)
        {
            if (!update.Method.IsStatic) 
            {
                Debug.LogError("StaticUpdate only supports running static methods.");
                return;
            }
            if (updates.Contains(update)) 
            {
                Debug.LogWarning($"{update.Method.Name} has already been added to StaticUpdate.");
                return;
            }
            updates.Add(update);
        }

        public static void RemoveUpdate(UpdateDelegate update) 
        {
            updates.Remove(update);
        }

        private static void Update() 
        {
            foreach (var update in updates) 
            {
                try
                {
                    update.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    public static class StaticCoroutines
    {
        internal static PlayerLoopSystem GetPlayerLoopSystem()
        {
            return new PlayerLoopSystem()
            {
                type = typeof(StaticCoroutines),
                updateDelegate = Update
            };
        }

        //using a linked list to make removing random routines faster
        private static LinkedList<IEnumerator> routines = new LinkedList<IEnumerator>();

        public static void StartCoroutine(IEnumerator routine) 
        {
            if (routine.MoveNext()) 
            {
                routines.AddLast(routine);
            }
        }

        private static void Update()
        { 
            var curRoutine = routines.First;
            if (curRoutine == null) { return; }
            var nextRoutine = curRoutine.Next;
            do
            {
                try
                {
                    if (!curRoutine.Value.MoveNext())
                    {
                        routines.Remove(curRoutine);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    routines.Remove(curRoutine);
                }
                finally 
                {
                    curRoutine = nextRoutine;
                    if (curRoutine != null) 
                    {
                        nextRoutine = curRoutine.Next;
                    }
                }
            } while (curRoutine != null);
        }
    }
}