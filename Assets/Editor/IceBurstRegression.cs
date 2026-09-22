#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// The probe uses the production damage dispatcher and HP API, without scene-wide defeat side effects.
public sealed class IceBurstDamageProbe : MonoBehaviour, IDamagable
{
    public int Hits;
    public void OnDamage(GameObject attacker, Weapon causer, Vector3 point, DamageEvent damage)
    {
        Hits++;
        GetComponent<HealthPointComponent>().Damage(damage.BaseDamage);
    }
}

[InitializeOnLoad]
public static class IceBurstRegression
{
    const string Request = "Library/IceBurst.request";
    const string Report = "Library/IceBurst-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static IceBurstRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { File.ReadAllText(Request); File.Delete(Request); }
        catch (IOException) { return; }
        try { Run(); }
        catch (Exception e) { File.AppendAllText(Report, e + "\n"); Debug.LogException(e); }
    }
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Hidden).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Hidden).SetValue(owner, value);
    static void Check(bool valid, string message)
    {
        File.AppendAllText(Report, (valid ? "PASS " : "FAIL ") + message + "\n");
        if (!valid) throw new Exception(message);
    }
    static Vector3 Direction(float angle) => Quaternion.Euler(0, angle, 0) * Vector3.forward;
    static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;
    static bool InMesh(MeshFilter mesh, Vector3 worldPoint)
    {
        var p = mesh.transform.InverseTransformPoint(worldPoint); p.y = 0;
        var vertices = mesh.sharedMesh.vertices;
        var triangles = mesh.sharedMesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i+1]]; var c = vertices[triangles[i+2]];
            float x = Cross(b-a,p-a), y = Cross(c-b,p-b), z = Cross(a-c,p-c);
            if ((x >= 0 && y >= 0 && z >= 0) || (x <= 0 && y <= 0 && z <= 0)) return true;
        }
        return false;
    }

    [MenuItem("Tools/Skill/Check IceBurst Geometry (Sandbox Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires isolated-save ShopRegression Play Mode.");
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " actual Play Mode\n");
        var data = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>("Assets/10.ScriptableObjects/Skills/MonsterSkills/iceburst.asset");
        var warning = data.phaseList.SelectMany(p=>p.modules).OfType<Module_SpawnWarningSign>().Single();
        var spawn = data.phaseList.SelectMany(p=>p.modules).OfType<Module_SpawnObject>().Single();
        var root = new GameObject("IceBurst regression fixture");
        root.transform.position = new Vector3(2000,0,2000);
        var meshes = new List<Mesh>();
        try
        {
            var caster = new GameObject("caster"); caster.SetActive(false); caster.transform.SetParent(root.transform, false);
            var status = caster.AddComponent<StatusComponent>();
            var character = caster.AddComponent<Character>();
            Set(character, "status", status);
            var effect = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/Effects/IceBurst.prefab"), root.transform);
            effect.transform.localPosition = Vector3.zero;
            var radial = effect.GetComponent<RadialAoEProjectile>();
            radial.SetDamageInfo(character, spawn.damageData);
            Set(radial, "currentRadius", Field<float>(radial,"maxRadius"));
            var signs = new List<MeshFilter>();
            var rotation = typeof(Module_SpawnWarningSign).GetMethod("GetSpawnRotation", Hidden);
            for (int i = 0; i < warning.fallbackSpawnCount; i++)
            {
                var sign = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/WaringSign_Fan.prefab"), root.transform).GetComponent<WarningSign_Fan>();
                sign.transform.localPosition = Vector3.zero;
                sign.Setup(warning, warning.duration);
                var main = typeof(WarningSign).GetField("mainPlane", Hidden).GetValue(sign) as Transform;
                signs.Add(main.GetComponent<MeshFilter>()); meshes.Add(signs.Last().sharedMesh);
            }
            Check(Mathf.Approximately(Field<float>(radial,"maxRadius"), warning.fanRadius * signs[0].transform.lossyScale.x) &&
                Mathf.Approximately(Field<float>(radial,"shardAngle"), warning.fanAngle) &&
                Field<int>(radial,"shardCount") == signs.Count, "actual warning mesh radius/angle/count match damage prefab");
            void Rotate(float yaw)
            {
                caster.transform.rotation = Quaternion.Euler(0,yaw,0);
                effect.transform.rotation = PositionHelpers.GetDirection(caster.transform,0,spawn.baseSpawnCount,spawn.baseAngleBetween,0);
                for(int i=0;i<signs.Count;i++) signs[i].transform.parent.rotation = (Quaternion)rotation.Invoke(warning,
                    new object[] { caster.transform,i,signs.Count,warning.fallbackAngleBetween });
            }
            bool Warned(Vector3 point) => signs.Any(m=>InMesh(m,point));
            var inside = typeof(RadialAoEProjectile).GetMethod("IsInsideRadialAttack",Hidden);
            bool Damaged(Vector3 point) => (bool)inside.Invoke(radial,new object[] { point });
            int samples=0, mismatches=0;
            foreach(float yaw in new[] {0f,18f,45f,90f,137f,180f,270f,359f})
            {
                Rotate(yaw);
                foreach(float radius in new[] {0.5f,5f,13.9f,14.1f,16f,17.1f})
                    for(int i=0;i<720;i++)
                    {
                        var point=root.transform.position+Direction(i*0.5f+0.13f)*radius;
                        samples++;
                        if(Warned(point)!=Damaged(point)) mismatches++;
                    }
            }
            Check(mismatches==0,$"warning mesh vs damage across {samples} positions / 8 caster rotations: {mismatches} mismatches");

            var target = new GameObject("player collider probe"); target.transform.SetParent(root.transform,false); target.layer=3;
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/Characters/Namsaengyi.prefab");
            var sourceBox = playerPrefab.GetComponent<BoxCollider>();
            var box=target.AddComponent<BoxCollider>(); box.size=sourceBox.size; box.center=sourceBox.center;
            target.transform.localScale=playerPrefab.transform.localScale;
            var hp=target.AddComponent<HealthPointComponent>(); hp.SetHealthPoint(100);
            var probe=target.AddComponent<IceBurstDamageProbe>();
            var scan=typeof(RadialAoEProjectile).GetMethod("CheckRadialHit",Hidden);
            void Scan(Vector3 point)
            {
                target.transform.position=point; Physics.SyncTransforms(); scan.Invoke(radial,null);
            }
            void ResetHit()
            {
                Field<HashSet<GameObject>>(radial,"hitTargets").Clear(); probe.Hits=0; hp.RestoreCurrentHealth(100);
            }
            Rotate(18);
            var nine=root.transform.position+Vector3.left*5;
            Check(!Warned(nine), "9 o'clock reproduction is in a warning gap at caster yaw 18");
            hp.RestoreCurrentHealth(1); Scan(nine);
            Check(probe.Hits==0 && hp.GetCurrentHP==1, "9 o'clock warning gap keeps 1 HP with actual player collider dimensions");
            box.size *= 3; Scan(nine);
            Check(probe.Hits==0 && hp.GetCurrentHP==1, "even a triple-size collider cannot widen angular damage into a gap");
            box.size=sourceBox.size;
            ResetHit(); Rotate(0); Scan(nine); Scan(nine);
            Check(Warned(nine) && probe.Hits==1 && hp.GetCurrentHP<100 && hp.GetCurrentHP>0,
                "9 o'clock is hit once only when covered by a warning; repeated scans do not multiply damage");
            ResetHit(); Scan(root.transform.position+Direction(warning.startAngleOffset+8)*5);
            Check(probe.Hits==0 && hp.GetCurrentHP==100, "old 17-degree overhang outside the 15-degree warning is safe");
            ResetHit(); Scan(root.transform.position+Direction(warning.startAngleOffset)*14.5f);
            Check(probe.Hits==0 && hp.GetCurrentHP==100, "old radius-17 overhang outside the radius-14 warning is safe");
            ResetHit(); Rotate(137); Scan(root.transform.position+Direction(137+warning.startAngleOffset)*5);
            Check(probe.Hits==1 && hp.GetCurrentHP<100, "rotated warning center still dispatches real damage through the HP API");
            File.AppendAllText(Report,"ICEBURST_REGRESSION_PASS\n");
        }
        finally
        {
            Object.DestroyImmediate(root);
            foreach(var mesh in meshes) if(mesh!=null) Object.DestroyImmediate(mesh);
        }
    }
}
#endif
