// --~~~~======# Author : Lupon Dylan #======~~~~~~--- //
// --~~~~======# Date   : 02 / 10 / 2026 #======~~~~-- //

using System.Collections.Generic;
using UnityEngine;

namespace UnBocal.CookingProject.Utilities
{
    // -------~~~~~~~~~~================# // Interfaces
    public interface IUBPool { public void Reset(); }

    public interface IUBPoolRef
    {
        public abstract void Store();
    }

    public interface IUBPooledObject
    {
        public IUBPoolRef PoolSelf {get; set;}
    }

    public class UBPool<T> : IUBPoolRef where T : Component
    {
        #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# //  Instance
        public bool isValid => instance != null;

        public T _prefab = null;
        public T prefab => _prefab;

        public T instance { get; private set; }
        public GameObject gameObject { get; private set; }
        public Transform transform { get; private set; }
        public RectTransform rectTransform { get; private set; }

        public UBPool(T pPrefab, T pInstance)
        {
            _prefab = pPrefab;

            instance = pInstance;
            gameObject = instance.gameObject;
            transform = instance.transform;
            transform.TryGetComponent(out RectTransform lRectTransform);
            rectTransform = lRectTransform;

            Store();
        }

        public UBPool<T> GetInstance()
        {
            return this;
        }

        public void Store() => Store(this);

        private void Reset()
        {
            instance = null;
            gameObject = null;
            transform = null;
            rectTransform = null;
        }

        #endregion

        #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Pool Container
        private class Pool<TObjType> : Dictionary<TObjType, List<TObjType>>, IUBPool where TObjType : Component
        {
            TObjType _component = null;

            // Get Instance
            public UBPool<TObjType> Call(TObjType pPrefab)
            {
                if (pPrefab == null) return null;

                UBPool<TObjType> lObj;

                // If Container Exist Get Object From Pool
                if (ContainsKey(pPrefab)) lObj = GetPoolObject(pPrefab);

                // If No Create Container
                else lObj = CreatePoolAndReturnInstance(pPrefab);

                return lObj;
            }

            // 
            private UBPool<TObjType> CreatePoolAndReturnInstance(TObjType pPrefab)
            {
                // Create Container
                this[pPrefab] = new();

                // Create Instances
                Instantiate(pPrefab);

                // Return First In Pool
                return GetPoolObject(pPrefab);
            }

            public UBPool<TObjType> Call()
            {
                UBPool<TObjType> lObj;

                if (_component) lObj = GetPoolObject(_component);
                else lObj = CreatePoolAndReturnInstanceOfComponent();

                return lObj;
            }

            private UBPool<TObjType> CreatePoolAndReturnInstanceOfComponent()
            {
                // Create Component
                _component = new GameObject(typeof(TObjType).Name).AddComponent<TObjType>();
                _component.transform.parent = UBPool<TObjType>._poolContainer.transform;
                _component.gameObject.SetActive(false);

                // Create Container
                this[_component] = new();

                // Create Instances
                Instantiate(_component);

                // Return First In Pool
                return GetPoolObject(_component);
            }

            private UBPool<TObjType> GetPoolObject(TObjType pPrefab)
            {
                List<TObjType> lInstances = this[pPrefab];

                // If No More Object Create
                if (lInstances.Count <= 0) Instantiate(pPrefab);

                // Convert First Object In Pool In PoolObject
                TObjType lObject = lInstances[0];
                UBPool<TObjType> lPoolObject = new(pPrefab, lObject);

                // Remove Instance
                do lInstances.Remove(lObject);
                while (lInstances.Contains(lObject));

                return lPoolObject;
            }

            private void Instantiate(TObjType pPrefab)
            {
                List<TObjType> lInstanceContinaer = this[pPrefab];

                TObjType lNewInstance;

                // Correct Instantiate Count If Negative Of Equale To Zero
                if (UBPool<TObjType>.instantiateCount <= 0) UBPool<TObjType>.instantiateCount = 1;

                // Create Instances
                for (int lIdx = 0; lIdx < UBPool<TObjType>.instantiateCount; lIdx++)
                {
                    lNewInstance = GameObject.Instantiate(pPrefab, UBPool<TObjType>._poolContainerTransform);
                    lNewInstance.gameObject.SetActive(false);

                    lInstanceContinaer.Add(lNewInstance);
                }
            }

            public void Reset() => Clear();
        }
        #endregion

        #region // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Static Control
        // -------~~~~~~~~~~================# // Instiaciation
        private static Pool<T> _pool = new();
        public static int instantiateCount = 5;

        // -------~~~~~~~~~~================# // Container
        private static UBPoolContainer _poolContainer = null;
        private static GameObject _poolContainerObject = null;
        private static Transform _poolContainerTransform = null;
        public static Vector3 origin = Vector3.zero;

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Initialization
        private static void InitPoolContainer()
        {
            if (_poolContainer) return;

            // Create Pool Manager
            _poolContainerObject = new GameObject($"Pool : {typeof(T).Name}");
            _poolContainer = _poolContainerObject.AddComponent<UBPoolContainer>();
            _poolContainerTransform = _poolContainerObject.transform;

            // Store Pool
            UBPoolContainer.onPoolCreated?.Invoke(_pool);
        }

        private static void SetDontDestroyOnLoad()
        {
            InitPoolContainer();

            UnityEngine.Object.DontDestroyOnLoad(_poolContainerObject);
        }
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Component Instantiation 
        public static UBPool<T> GetInstanceComponent() => GetInstanceComponent(null);

        public static UBPool<T> GetInstanceComponent(Transform pParent = null)
        {
            InitPoolContainer();

            // Get Component In Pool And Re-Parent It
            return ReParenting(_pool.Call(), pParent);
        }

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Prefab Instantiation 
        public static UBPool<T> GetInstancePrefab(T pPrefab) => GetInstancePrefab(pPrefab, null);

        public static UBPool<T> GetInstancePrefab(T pPrefab, Transform pParent = null)
        {
            InitPoolContainer();

            // Get Object In Pool And Re-Parent It
            return ReParenting(_pool.Call(pPrefab), pParent);
        }

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Parenting 
        private static UBPool<T> ReParenting(UBPool<T> lObj, Transform pParent)
        {
            if (lObj == null) return null;

            // Parenting Pool Object
            lObj.gameObject.SetActive(true);
            lObj.transform.SetParent(pParent);
            lObj.transform.localPosition = Vector3.zero;
            lObj.transform.localRotation = Quaternion.identity;

            if (lObj.instance is IUBPooledObject asPooledObject)
                asPooledObject.PoolSelf = lObj;

            return lObj;
        }

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // Storage
        public static void Store(UBPool<T> pObj)
        {
            InitPoolContainer();

            if (pObj.instance != null)
            {
                List<T> lCurrentPool;

                // If No Container For This Pool Then Create IT
                if (!_pool.ContainsKey(pObj.prefab)) lCurrentPool = _pool[pObj.prefab] = new();
                else lCurrentPool = _pool[pObj.prefab];

                // Store Object If Not Already Stored
                if (lCurrentPool.Contains(pObj.instance)) return;
                
                _pool[pObj.prefab].Add(pObj.instance);

                // Re-Parent To Manager
                pObj.gameObject.SetActive(false);
                pObj.transform.SetParent(_poolContainer.transform);
                pObj.transform.localPosition = origin;
            }

            // Reset PoolObject
            pObj.Reset();
        }

        #endregion
    }

    public class UBPoolContainer : MonoBehaviour
    {
        public static System.Action<IUBPool> onPoolCreated;
        public List<IUBPool> _pools = new();

        private void Awake()
            => onPoolCreated += OnPoolCreated;

        private void OnDestroy()
        {
            // Clear All Pools
            onPoolCreated -= OnPoolCreated;
            foreach (IUBPool lPool in _pools)
                lPool.Reset();
        }

        private void OnPoolCreated(IUBPool pPool)
            => _pools.Add(pPool);
    }
}