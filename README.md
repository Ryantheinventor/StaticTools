# Static Tools
 
## Setup
Install using the UnityPackage file

Include a using in any files you want to use these tools in
```csharp
using StaticTools
```

## Usage
### StaticUpdates:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Setup() 
{
    StaticUpdate.AddUpdate(OnUpdate);//add OnUpdate to the update loop
}

private static void OnUpdate() 
{
    //This runs in the update loop...
}
```

### StaticCoroutines:
```csharp
private static StaticCoroutine routineRef;

public static void StartItSomewhere()
{
    routineRef = StaticCoroutines.StartCoroutine(TestStaticRoutine());
}

public static void StopItSomewhereElse()
{
    StaticCoroutines.StopCoroutine(routineRef);
}

public static IEnumerator StaticCoroutine()
{
    while(true)
    {
        yield return null;
    }
}
```