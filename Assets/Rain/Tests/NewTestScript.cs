using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using UnityEngine.SceneManagement;

public class NewTestScript
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public void NewTestScriptSimplePasses()
    {
        using (Measure.Scope(new SampleGroup("Test2")))
        {
            SceneManager.LoadScene("Hodographe");
        }

        //yield return Measure.Frames().Run();
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
