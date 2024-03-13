using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StaticTools;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Pre Start");
        StartCoroutine(TestRoutine());
        StaticCoroutines.StartCoroutine(TestStatic.TestStaticRoutine());
        Debug.Log("Post Start");
    }

    private void Update()
    {
        Debug.Log("Update");
    }

    private IEnumerator TestRoutine() 
    {
        Debug.Log("Routine Started");
        yield return null;
        Debug.Log("Routine Ended");
    }
}

public static class TestStatic 
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Setup() 
    {
        StaticUpdate.AddUpdate(OnUpdate);
    }

    private static void OnUpdate() 
    {
        Debug.Log("Static Update");
    }

    public static IEnumerator TestStaticRoutine() 
    {
        Debug.Log("Static Routine Started");
        yield return null;
        Debug.Log("Static Routine Ended");
    }
}
