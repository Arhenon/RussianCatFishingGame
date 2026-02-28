using UnityEngine;

public class Hook : MonoBehaviour
{
    public FishAI caughtFish = null;

    void Update()
    {
        if (caughtFish != null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var hit in hits)
        {
            FishAI fish = hit.GetComponent<FishAI>();
            if (fish != null && !fish.isHooked)
            {
                fish.CheckForBait(this.transform);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (caughtFish != null) return;

        FishAI fish = other.GetComponent<FishAI>();
        if (fish != null)
        {
            AttachFish(fish);
        }
    }

    void AttachFish(FishAI fish)
    {
        caughtFish = fish;
        fish.isHooked = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 10f); 
        foreach (var hit in hits)
        {
            FishAI otherFish = hit.GetComponent<FishAI>();
            if (otherFish != null && otherFish != fish)
            {
                otherFish.ClearBait(); 
            }
        }

        Rigidbody2D fishRb = fish.GetComponent<Rigidbody2D>();
        if (fishRb != null) fishRb.simulated = false;

        Collider2D fishCol = fish.GetComponent<Collider2D>();
        if (fishCol != null) fishCol.enabled = false;

        Vector3 originalWorldScale = fish.transform.lossyScale;
        fish.transform.SetParent(this.transform);
        fish.transform.localPosition = Vector3.zero;

        Vector3 parentScale = this.transform.lossyScale;
        if (parentScale.x == 0) parentScale.x = 1;
        if (parentScale.y == 0) parentScale.y = 1;

        fish.transform.localScale = new Vector3(originalWorldScale.x / parentScale.x, originalWorldScale.y / parentScale.y, 1f);
    }
}