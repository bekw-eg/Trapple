using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Trapple.Editor
{
    // A local request lets the already-open Editor perform the same documented
    // menu command. No second Editor process touches the project's asset database.
    [InitializeOnLoad]
    public static class StorybookUIAutomation
    {
        private static double next;
        private static readonly MethodInfo ActiveRun=typeof(TestRunnerApi).GetMethod("IsRunActive",BindingFlags.Static|BindingFlags.NonPublic);
        static StorybookUIAutomation()
        {
            EditorApplication.update+=Tick;
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Results());
        }
        private static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            next=EditorApplication.timeSinceStartup+1;
            // RunFinished precedes the framework's scene restoration by several
            // frames. Wait for its job to finish before opening another scene.
            if((bool)(ActiveRun?.Invoke(null,null) ?? false)) return;
            const string request="Temp/storybook-build-request.txt";
            if(!File.Exists(request)) return;
            string command=File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if(command=="capture")
                { SessionState.SetBool("Storybook.Capture",true); EditorApplication.isPlaying=true; }
                else if(command=="player")
                {
                    Directory.CreateDirectory("Temp/StorybookPlayer");
                    var result=BuildPipeline.BuildPlayer(EditorBuildSettings.globalScenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                        "Temp/StorybookPlayer/Trapple.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                    File.WriteAllText("Temp/storybook-player-build.txt",result.summary.result.ToString());
                }
                else if(command=="edit-tests" || command=="play-tests" || command=="ui-tests" || command=="regression-tests")
                {
                    string mode=command=="edit-tests"?"edit":"play"; SessionState.SetString("Storybook.TestMode",mode);
                    var filter=new Filter {testMode=mode=="edit"?TestMode.EditMode:TestMode.PlayMode};
                    if(command=="ui-tests") filter.testNames=new[]{"Trapple.Tests.StorybookUITests"};
                    if(command=="regression-tests")
                    {
                        var assembly=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Trapple.PlayModeTests");
                        filter.testNames=assembly.GetTypes().SelectMany(type=>type.GetMethods().Where(method=>
                            method.GetCustomAttributes(true).Any(attribute=>attribute.GetType().Name=="TestAttribute" || attribute.GetType().Name=="UnityTestAttribute"))
                            .Select(method=>type.FullName+"."+method.Name))
                            .Where(name=>!name.Contains("NormalControlsCanClearTheEntireCourse") && !name.Contains("EntireTutorialIsReachableWithMovementAndJumps") && !name.Contains("CaptureSectionScenery")).ToArray();
                    }
                    ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
                }
                else StorybookUIBuilder.Build();
            }
            catch(Exception exception)
            {
                File.WriteAllText("Temp/storybook-build-result.txt","FAILED\n"+exception);
                Debug.LogException(exception);
            }
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string mode=SessionState.GetString("Storybook.TestMode","other");
                TestRunnerApi.SaveResultToFile(result,"Logs/storybook-"+mode+"-tests.xml");
                File.WriteAllText("Temp/storybook-tests-result.txt",result.TestStatus+" "+result.PassCount+" passed, "+result.FailCount+" failed");
            }
        }
    }
}
