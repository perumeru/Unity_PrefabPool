using System;
using UnityEngine;
using System.Collections.Generic;

public interface IPrefabInstantiate
{
    GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent);
    GameObject Instantiate();
}
public class PrefabPoolManager : Singleton.SingletonMonoBehaviour<PrefabPoolManager>
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
    private readonly Dictionary<string, PrefabFamiliy> prefabFamilies = new Dictionary<string, PrefabFamiliy>();
    //PreAwake > Awake
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void PreAwake()
    {
        if (Instance.preloadPrefabInfo == null) return;
        Instance.PreloadPrefab(Instance.preloadPrefabInfo);
    }
    protected override void Awake()
    {
        base.Awake();
    }
    void OnDisable()
    {
        prefabFamilies.Clear();
        preloadPrefabInfo = default(PreloadPrefabInfo[]);
        Resources.UnloadUnusedAssets();
    }
    public bool ContainsKey(in string key) => prefabFamilies.ContainsKey(key);
    public IPrefabInstantiate Get(in string key)
    {
        if (!prefabFamilies.ContainsKey(key))
            return new SAFENULL();

        return prefabFamilies[key];
    }
    public void UnloadPrefab(in string key)
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
                    prefabFamiliy.PreloadPrefab(ppi.prefab, transform.position, ppi.Parent == null ? transform : ppi.Parent).Commit();
                    break;
                case PreloadPrefabInfo.InitPos.Parent:
                    prefabFamiliy.PreloadPrefab(ppi.prefab, ppi.Parent == null ? transform.position : ppi.Parent.position, ppi.Parent == null ? transform : ppi.Parent).Commit();
                    break;
                case PreloadPrefabInfo.InitPos.Random:
                    prefabFamiliy.PreloadPrefab(ppi.prefab, transform.position, ppi.Parent == null ? transform.position : ppi.Parent.position, ppi.Parent == null ? transform : ppi.Parent).Commit();
                    break;
            }
            prefabFamilies.Add(ppi.key, prefabFamiliy);
        }
    }
    ///<summary>�G���[��</summary>
    struct SAFENULL : IPrefabInstantiate
    {
        public GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent) => null;
        public GameObject Instantiate() => null;
    }
    interface ICommit
    {
        void Commit();
    }
    /// <summary>�I�u�W�F�N�g���v�[�����Ă���</summary>
    class PrefabFamiliy : IPrefabInstantiate, ICommit, IDisposable
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
                GameObject prefab;
                GetPrefab(out prefab);
                if (!prefab.activeSelf) prefab.SetActive(true);
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
        void GetPrefab(out GameObject game)
        {
            if (!enumerator.MoveNext()) {
                enumerator = instancies.GetEnumerator();
                enumerator.MoveNext();
            }
            game = enumerator.Current;
        }
        ///<summary>���O�i�[�p�i1�_�w��j</summary>
        public ICommit PreloadPrefab(GameObject obj, Vector3 pos, Transform parent)
        {
            for (int i = 0; i < this.capacity; i++) 
                Prefab = GameObject.Instantiate(obj, pos, Quaternion.identity, parent);

            return this;
        }
        ///<summary>���O�i�[�p�i2�_�w��(�����_��)�j</summary>
        public ICommit PreloadPrefab(GameObject obj, Vector3 pos1, Vector3 pos2, Transform parent)
        {
            for (int i = 0; i < this.capacity; i++)
                Prefab = GameObject.Instantiate(obj, new Vector3(UnityEngine.Random.Range(pos1.x, pos2.x), 
                    UnityEngine.Random.Range(pos1.y, pos2.y), 
                    UnityEngine.Random.Range(pos1.z, pos2.z)),
                    UnityEngine.Random.rotation, parent);
            return this;
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
        void ICommit.Commit() => enumerator = instancies.GetEnumerator();
        public PrefabFamiliy(int capacity)
        {
            instancies = new LinkedList<GameObject>();
            poolInstanceID = new HashSet<int>();
            this.capacity = capacity;
        }
    }
}
