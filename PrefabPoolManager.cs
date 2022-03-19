using System;
using UnityEngine;
using System.Collections.Generic;

public interface IPrefabInstantiate
{
    int MaxSize { get; }
    int RemainingSize { get; }
    GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent);
    GameObject Instantiate();
}
public class PrefabPoolManager : MonoBehaviour
{
    [Serializable]
    public struct PreloadPrefabInfo
    {
        [Header("Key")] public string key;
        [Header("���O�ǂݍ��݂���Prefab")] public GameObject prefab;
        [Header("���O�ǂݍ��݂���Prefab�̐�")] [Range(0, 255)] public int amount;
        [Header("�e")] public Transform Parent;
        public enum InitPos { No, Parent, Random }
        [Header("�����ʒu")] public InitPos pos;
    }

    [SerializeField]
    private PreloadPrefabInfo[] preloadPrefabInfo;
    [NonSerialized]
    private static readonly Dictionary<string, PrefabFamiliy> prefabFamilies = new Dictionary<string, PrefabFamiliy>();

    void Awake()
    {
        if (preloadPrefabInfo == null) return;
        PreloadPrefab(preloadPrefabInfo);
    }
    void OnDisable()
    {
        prefabFamilies.Clear();
        preloadPrefabInfo = default(PreloadPrefabInfo[]);
        Resources.UnloadUnusedAssets();
    }
    public static bool ContainsKey(in string key) => prefabFamilies.ContainsKey(key);
    public static IPrefabInstantiate Get(in string key)
    {
        if (!prefabFamilies.ContainsKey(key))
            return new SAFENULL();

        return prefabFamilies[key];
    }
    public static void UnloadPrefab(in string key)
    {
        if (prefabFamilies.ContainsKey(key))
        {
            prefabFamilies[key].Dispose();
            prefabFamilies.Remove(key);
            Resources.UnloadUnusedAssets();
        }
    }
    public void PreloadPrefab(in PreloadPrefabInfo[] ppis)
    {
        foreach (PreloadPrefabInfo ppi in ppis)
        {
            if (ppi.prefab == null) continue;
            if (prefabFamilies.ContainsKey(ppi.key)) continue;
            PrefabFamiliy prefabFamiliy = new PrefabFamiliy(ppi.amount);
            switch (ppi.pos)
            {
                case PreloadPrefabInfo.InitPos.No:
                    prefabFamiliy.PreloadPrefab(ppi.prefab, transform.position, ppi.Parent == null ? transform : ppi.Parent);
                    break;
                case PreloadPrefabInfo.InitPos.Parent:
                    prefabFamiliy.PreloadPrefab(ppi.prefab, ppi.Parent == null ? transform.position : ppi.Parent.position, ppi.Parent == null ? transform : ppi.Parent);
                    break;
                case PreloadPrefabInfo.InitPos.Random:
                    prefabFamiliy.PreloadPrefab(ppi.prefab, transform.position, ppi.Parent == null ? transform.position : ppi.Parent.position, ppi.Parent == null ? transform : ppi.Parent);
                    break;
            }
            prefabFamiliy.Commit();
            prefabFamilies.Add(ppi.key, prefabFamiliy);
        }
    }
    ///<summary>�G���[��</summary>
    struct SAFENULL : IPrefabInstantiate
    {
        public int MaxSize => 0;
        public int RemainingSize => 0;
        public GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent) => null;
        public GameObject Instantiate() => null;
    }
    /// <summary>�I�u�W�F�N�g���v�[�����Ă���</summary>
    struct PrefabFamiliy : IPrefabInstantiate, IDisposable
    {
        LinkedList<GameObject>.Enumerator enumerator;
        readonly LinkedList<GameObject> instancies;
        readonly HashSet<int> poolInstanceID;
        readonly int capacity;
        ///<summary>�ȈՃI�u�W�F�N�g�v�[��</summary>
        GameObject Prefab
        {
            get
            {
                GameObject prefab = New();
                if (poolInstanceID.Remove(prefab.GetInstanceID())) prefab.SetActive(true);
                return prefab;
            }
            set
            {
                if (value == null) return;
                if (!poolInstanceID.Add(value.GetInstanceID())) return;
                value.SetActive(false);
                instancies.AddLast(value);
            }
        }
        GameObject New()
        {
            if (!enumerator.MoveNext())
            {
                //�����������
                enumerator = instancies.GetEnumerator();
                while (enumerator.MoveNext()) enumerator.Current.SetActive(false);
                enumerator = instancies.GetEnumerator();
                return New();
            }
            return enumerator.Current;
        }

        ///<summary>���O�i�[�p�i1�_�w��j</summary>
        public void PreloadPrefab(GameObject obj, Vector3 pos, Transform parent)
        {
            for (int i = 0; i < this.capacity; i++) 
                Prefab = GameObject.Instantiate(obj, pos, Quaternion.identity, parent);
        }
        ///<summary>���O�i�[�p�i2�_�w��(�����_��)�j</summary>
        public void PreloadPrefab(GameObject obj, Vector3 pos1, Vector3 pos2, Transform parent)
        {
            for (int i = 0; i < this.capacity; i++)
                Prefab = GameObject.Instantiate(obj, new Vector3(UnityEngine.Random.Range(pos1.x, pos2.x), 
                    UnityEngine.Random.Range(pos1.y, pos2.y), 
                    UnityEngine.Random.Range(pos1.z, pos2.z)),
                    UnityEngine.Random.rotation, parent);
        }
        ///<summary>�C���X�^���X</summary>
        public GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject prefab = Prefab;
            if (position != null) prefab.transform.position = position;
            if (rotation != null) prefab.transform.rotation = rotation;
            if (parent != null) prefab.transform.parent = parent;
            return prefab;
        }
        public GameObject Instantiate() => Prefab;
        ///<summary>���łɒǉ��ς��m�F����</summary>
        public bool Contains(GameObject obj) => obj == null ? false : poolInstanceID.Contains(obj.GetInstanceID());
        public void Dispose() { instancies.Clear(); poolInstanceID.Clear(); }
        ///<summary>�C�ӂ̃^�C�~���O�ŃR�~�b�g</summary>
        public void Commit() { enumerator = instancies.GetEnumerator(); }
        public int MaxSize { get => instancies.Count; }
        public int RemainingSize { get => poolInstanceID.Count; }
        public PrefabFamiliy(int capacity)
        {
            instancies = new LinkedList<GameObject>();
            poolInstanceID = new HashSet<int>();
            this.capacity = capacity;
        }
    }
}
