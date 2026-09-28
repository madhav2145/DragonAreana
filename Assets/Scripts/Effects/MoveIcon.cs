using UnityEngine;

public class MoveIcon : MonoBehaviour
{
    private float timer = 0.5f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
