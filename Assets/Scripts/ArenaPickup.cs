using UnityEngine;
namespace BigMonster
{
    public sealed class ArenaPickup : MonoBehaviour
    {
        public Vector3 home;
        public bool collected;
        void Start() { home = transform.position; }
        void Update()
        {
            if (collected) return;
            transform.Rotate(new Vector3(15, 38, 22) * Time.deltaTime);
            transform.position = home + Vector3.up * (Mathf.Sin(Time.time * 2.4f + home.x) * .12f);
        }
        public void ResetPickup() { collected = false; gameObject.SetActive(true); transform.position = home; }
    }
}
