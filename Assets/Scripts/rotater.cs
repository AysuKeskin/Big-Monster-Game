using UnityEngine;

public class rotater : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        transform.Rotate (new Vector3 (15, 30, 45) * Time.deltaTime);
        
    }
}
