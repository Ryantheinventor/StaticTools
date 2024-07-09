using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StaticTools
{
    internal static class Initializer
    {
        /// <summary>
        /// Hook all our custom subsystems
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void HookIntoPlayerLoop()
        {
            AddToPlayerLoop("Update", 0, StaticUpdate.GetPlayerLoopSystem());
            AddToPlayerLoop("Update", 1, StaticCoroutines.GetPlayerLoopSystem());
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += StaticUpdate.EndPlayMode;
            EditorApplication.playModeStateChanged += StaticCoroutines.EndPlayMode;
#endif
        }

        /// <summary>
        /// Hooks a custom subsystem into the target Unity subsystem
        /// </summary>
        /// <param name="targetSubSystem"></param>
        /// <param name="targetIndex"></param>
        /// <param name="newSystem"></param>
        internal static void AddToPlayerLoop(string targetSubSystem, int targetIndex, PlayerLoopSystem newSystem)
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

    /// <summary>
    /// Enables update methods to be run without an owner
    /// </summary>
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

        /// <summary>
        /// Add a method to the static update loop
        /// </summary>
        /// <param name="update"></param>
        public static void AddUpdate(UpdateDelegate update)
        {
            if (!update.Method.IsStatic)//Force the method to be static for saftey and consistency 
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

        /// <summary>
        /// Remove a method from the static update loop
        /// </summary>
        /// <param name="update"></param>
        public static void RemoveUpdate(UpdateDelegate update)
        {
            updates.Remove(update);
        }

        //Runs the update delegates
        private static void Update()
        {
            if (!Application.isPlaying) { return; }
            foreach (var update in updates)
            {
                //try-catch each method so that an error doesn't prevent others from running
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

#if UNITY_EDITOR
        public static void EndPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                updates.Clear();
            }
        }
#endif
    }

    /// <summary>
    /// Stores the currently running routine and the routine info to resume (if any) once the currently running finishes
    /// </summary>
    internal class RoutineInfo
    {
        public IEnumerator enumerator;
        public RoutineInfo routineToResume;
    }

    /// <summary>
    /// A StaticCoroutine is StaticTools' equivalent to Unity's Coroutine class
    /// </summary>
    public class StaticCoroutine
    {
        internal RoutineInfo routine;
    }

    /// <summary>
    /// Enables coroutines to run without an owner
    /// </summary>
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
        private static LinkedList<RoutineInfo> routines = new LinkedList<RoutineInfo>();

        /// <summary>
        /// Starts an ownerless coroutine
        /// </summary>
        /// <param name="routine"></param>
        /// <returns>A StaticCoroutine that can be used to stop the coroutine started here</returns>
        public static StaticCoroutine StartCoroutine(IEnumerator routine)
        {
            RoutineInfo routineInfo = new RoutineInfo() { enumerator = routine };
            var node = routines.AddLast(routineInfo);
            if (!MoveNext(routineInfo))
            {
                routines.Remove(node);
            }
            return new StaticCoroutine() { routine = routineInfo };
        }

        /// <summary>
        /// Stops the given coroutine if it is still running
        /// </summary>
        /// <param name="coroutine"></param>
        public static void StopCoroutine(StaticCoroutine coroutine)
        {
            if (coroutine == null) { return; }
            var curRoutine = routines.First;
            if (curRoutine == null) { return; }
            var nextRoutine = curRoutine.Next;
            do
            {
                if (DoesCoroutineMatch(coroutine, curRoutine.Value))
                {
                    routines.Remove(curRoutine);
                    return;
                }
                curRoutine = nextRoutine;
                if (curRoutine != null)
                {
                    nextRoutine = curRoutine.Next;
                }
            } while (curRoutine != null);
        }

        /// <summary>
        /// Recursive search for if the RoutineInfo specified was started by the specified StaticCoroutine
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        private static bool DoesCoroutineMatch(StaticCoroutine marker, RoutineInfo toCheck)
        {
            if (toCheck == marker.routine)
            {
                return true;
            }
            else
            {
                if (toCheck.routineToResume != null)
                {
                    return DoesCoroutineMatch(marker, toCheck.routineToResume);//Searches recursively because of chain coroutines
                }
                else
                {
                    return false;
                }
            }
        }

        //perform steps on all coroutines
        private static void Update()
        {
            if (!Application.isPlaying) { return; }
            var curRoutine = routines.First;
            if (curRoutine == null) { return; }
            var nextRoutine = curRoutine.Next;
            do
            {
                //try-catch each coroutine so that an error doesn't prevent others from running
                try
                {
                    if (!MoveNext(curRoutine.Value))
                    {
                        routines.Remove(curRoutine);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    routines.Remove(curRoutine);// If a coroutine errors out we cancel it's whole chain
                }
                finally
                {
                    //increment coroutines
                    curRoutine = nextRoutine;
                    if (curRoutine != null)
                    {
                        nextRoutine = curRoutine.Next;
                    }
                }
            } while (curRoutine != null);
        }

        /// <summary>
        /// Performs MoveNext call in the specified routine
        /// </summary>
        /// <param name="routine"></param>
        /// <returns>
        /// True: the coroutine continues
        /// False: the coroutine should be removed
        /// </returns>
        private static bool MoveNext(RoutineInfo routine)
        {
            if (routine.enumerator.MoveNext())
            {
                //If the result is a YieldInstruction we check if it's supported
                if (routine.enumerator.Current != null && typeof(YieldInstruction).IsAssignableFrom(routine.enumerator.Current.GetType()))
                {
                    if (typeof(WaitForSeconds) == routine.enumerator.Current.GetType())//Start a chain with custom WaitForSeconds
                    {
                        float seconds = (float)typeof(WaitForSeconds).GetField("m_Seconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(routine.enumerator.Current);

                        //start a chain
                        routines.AddLast(new RoutineInfo() { enumerator = WaitForSeconds(seconds), routineToResume = routine });

                        return false;
                    }
                    else
                    {
                        Debug.LogError("Static Coroutines does not support the YieldInstruction(" + routine.enumerator.Current.GetType() + ") the routine will continue as normal");
                        return true;
                    }
                }
                // If the result is another routine we start that one and mark the current one to be resumed uppon completion of the new routine
                else if (routine.enumerator.Current != null && typeof(IEnumerator).IsAssignableFrom(routine.enumerator.Current.GetType()))
                {
                    routines.AddLast(new RoutineInfo() { enumerator = (IEnumerator)routine.enumerator.Current, routineToResume = routine });
                    return false;
                }
                // If the result is not a YieldInstruction or another routine it is ignored
                else
                {
                    return true;
                }
            }
            else
            {
                if (routine.routineToResume != null)// Resume the previous routine if there is one
                {
                    routines.AddLast(routine.routineToResume);
                }
                return false;
            }
        }

        /// <summary>
        /// A custom implementation of WaitForSeconds to enable support for Unity's YieldInstruction based version.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        internal static IEnumerator WaitForSeconds(float seconds)
        {
            float curTime = 0;
            while (curTime < seconds)
            {
                yield return null;
                curTime += Time.deltaTime;
            }
        }

#if UNITY_EDITOR
        public static void EndPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                routines.Clear();
            }
        }
#endif
    }
}