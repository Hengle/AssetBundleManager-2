using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TDE
{
    public class SingletonComponent<T> : MonoBehaviour where T : Component
    {
        private static T m_Instance;

        private static readonly object m_Syslock = new object();

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (m_Syslock)
                    { //锁一下，避免多线程出问题
                        m_Instance = FindObjectOfType(typeof(T)) as T;
                        if (m_Instance == null)
                        {
                            GameObject obj = new GameObject();
                            obj.hideFlags = HideFlags.HideAndDontSave;
                            m_Instance = (T)obj.AddComponent(typeof(T));
                        }
                    }
                }
                return m_Instance;
            }
        }

        public virtual void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            if (m_Instance == null)
            {
                m_Instance = this as T;
            }
            else
            {
                Destroy(gameObject);
            }
            AfterAwake();
        }

        public virtual void AfterAwake()
        {

        }

        public static bool IsBuild
        {
            get
            {
                return m_Instance != null;
            }
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
