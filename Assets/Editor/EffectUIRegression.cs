#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class EffectUIRegression
{
    const string Request = "Library/EffectUI.request";
    const string Report = "Library/EffectUI-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static EffectUIRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { File.ReadAllText(Request); File.Delete(Request); }
        catch (IOException) { return; }
        try { Run(); }
        catch (Exception e) { File.AppendAllText(Report, e + "\n"); Debug.LogException(e); }
    }
    sealed class ProbeEffect : BaseEffect
    {
        public ProbeEffect(Sprite icon) : base("EffectUIRegression", "test", 10) { FxIcon = icon; }
    }
    static Delegate Listeners(object owner, string name) => (Delegate)owner.GetType().GetField(name, Hidden).GetValue(owner);
    static int Count(Delegate listeners) => listeners?.GetInvocationList().Length ?? 0;
    static int RemovalCount(BaseEffect effect) => Count((Delegate)typeof(BaseEffect).GetField("OnRemovedUI",Hidden).GetValue(effect));
    static Dictionary<string, EffectIconUI> Icons(EffectGroupUI group) =>
        (Dictionary<string, EffectIconUI>)typeof(EffectGroupUI).GetField("icons",Hidden).GetValue(group);
    static void Check(bool valid, string message)
    {
        File.AppendAllText(Report, (valid ? "PASS " : "FAIL ") + message + "\n");
        if (!valid) throw new Exception(message);
    }
    [MenuItem("Tools/Effect/Check UI Lifecycle (Sandbox Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires isolated-save ShopRegression Play Mode.");
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " actual Play Mode\n");
        var roots = new List<GameObject>();
        var handler = ScriptableObject.CreateInstance<SO_HUDHandler>();
        var texture = new Texture2D(2,2);
        var sprite = Sprite.Create(texture,new Rect(0,0,2,2),Vector2.zero);
        try
        {
            foreach(bool boss in new[] {true,false})
            {
                string label=boss?"boss":"player";
                string channel=boss?"OnChangedBossEffect_OneParam":"OnEffect";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(boss?
                    "Assets/5.Prefabs/UI/HUD/BossGauge.prefab":"Assets/5.Prefabs/UI/HUD/GaugeGroup.prefab");
                var source=prefab.GetComponentInChildren<EffectGroupUI>(true);
                var template=(EffectIconUI)typeof(EffectGroupUI).GetField("effectIconUI",Hidden).GetValue(source);
                Check(template!=null,label+" authored prefab icon reference resolves");
                EffectGroupUI Create()
                {
                    var root=new GameObject("Effect UI regression "+label,typeof(RectTransform)); roots.Add(root); root.SetActive(false);
                    var group=(EffectGroupUI)root.AddComponent(boss?typeof(BossEffectGroupUI):typeof(EffectGroupUI));
                    typeof(EffectGroupUI).GetField("handler",Hidden).SetValue(group,handler);
                    typeof(EffectGroupUI).GetField("effectIconUI",Hidden).SetValue(group,template);
                    root.SetActive(true); return group;
                }
                void Raise(BaseEffect effect)
                {
                    if(boss) handler.OnChangedBossEffect(null,effect); else handler.OnApplyEffect(effect);
                }
                var ui=Create();
                Check(Count(Listeners(handler,channel))==1,label+" enable subscribes once");
                var effect=new ProbeEffect(sprite); Raise(effect); Raise(effect);
                Check(Icons(ui).Count==1 && RemovalCount(effect)==1,label+" repeated apply creates one icon and one removal listener");
                ui.gameObject.SetActive(false);
                Check(Count(Listeners(handler,channel))==0 && Icons(ui).Count==1,label+" disable detaches HUD while preserving existing icon");
                effect.OnRemove();
                Check(Icons(ui).Count==0 && RemovalCount(effect)==0,label+" removal while hidden cleans icon and listener");
                for(int i=0;i<3;i++) { ui.gameObject.SetActive(true); ui.gameObject.SetActive(false); }
                ui.gameObject.SetActive(true);
                Check(Count(Listeners(handler,channel))==1,label+" repeated enable does not accumulate HUD listeners");
                effect=new ProbeEffect(sprite); Raise(effect);
                var replacement=new ProbeEffect(sprite); Raise(replacement); effect.OnRemove();
                Check(Icons(ui).Count==1 && RemovalCount(effect)==0 && RemovalCount(replacement)==1,
                    label+" replacing same effect ID detaches old removal callback");
                Object.DestroyImmediate(Icons(ui)[replacement.ID].gameObject); Raise(replacement);
                Check(Icons(ui)[replacement.ID]!=null && RemovalCount(replacement)==1,label+" destroyed icon is safely recreated");
                var stale=Listeners(handler,channel);
                Object.DestroyImmediate(ui.gameObject);
                Check(Count(Listeners(handler,channel))==0 && RemovalCount(replacement)==0,label+" destroy detaches HUD and effect callbacks");
                Raise(new ProbeEffect(sprite)); replacement.OnRemove();
                if(boss) ((Action<Character,BaseEffect>)stale)(null,replacement);
                else ((Action<BaseEffect>)stale)(replacement);
                Check(true,label+" late and captured callbacks after destruction do not throw");
                for(int i=0;i<3;i++)
                {
                    ui=Create(); effect=new ProbeEffect(sprite); Raise(effect);
                    Check(Icons(ui).Count==1 && Count(Listeners(handler,channel))==1,label+" UI recreation cycle "+i);
                    ui.gameObject.SetActive(false); Object.DestroyImmediate(ui.gameObject);
                    Check(RemovalCount(effect)==0 && Count(Listeners(handler,channel))==0,label+" disabled UI destruction cleans all subscriptions "+i);
                }
            }
            File.AppendAllText(Report,"EFFECT_UI_REGRESSION_PASS\n");
        }
        finally
        {
            foreach(var root in roots) if(root!=null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(handler); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
    }
}
#endif
