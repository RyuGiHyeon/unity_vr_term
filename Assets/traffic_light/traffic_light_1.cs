using UnityEngine;

public class traffic_light_1 : MonoBehaviour
{
    public GameObject redObject1;
    public GameObject greenObject1;
    public GameObject yellowObject1;

    private Material redMaterial;
    private Material greenMaterial;
    private Material yellowMaterial;

    public Collider trafficCollider;

    private void Start()
    {
        // Initialize materials from objects
        redMaterial = redObject1.GetComponent<Renderer>().material;
        greenMaterial = greenObject1.GetComponent<Renderer>().material;
        yellowMaterial = yellowObject1.GetComponent<Renderer>().material;

        // Start initial coroutine with delay
        StartCoroutine(StartWithDelay());
    }

    private System.Collections.IEnumerator StartWithDelay()
    {
        // Wait for 45 seconds before starting the traffic light cycle
        yield return new WaitForSeconds(0f);

        // Start the transparency handling coroutine
        StartCoroutine(HandleTransparency());
    }

    private System.Collections.IEnumerator HandleTransparency()
    {
        while (true)
        {
            // Instantly toggle red object's transparency
            yield return ChangeTransparency(redMaterial, true);
            trafficCollider.enabled = true;
            yield return new WaitForSeconds(45f);
            yield return ChangeTransparency(redMaterial, false);

            // Instantly toggle green object's transparency
            yield return ChangeTransparency(greenMaterial, true);
            trafficCollider.enabled = false;
            yield return new WaitForSeconds(12f);
            yield return ChangeTransparency(greenMaterial, false);

            // Instantly toggle yellow object's transparency
            yield return ChangeTransparency(yellowMaterial, true);
            yield return new WaitForSeconds(3f);
            yield return ChangeTransparency(yellowMaterial, false);
        }
    }

    private System.Collections.IEnumerator ChangeTransparency(Material material, bool fadeOut)
    {
        Color color = material.color;
        float alpha = fadeOut ? 0f : 1f;
        material.color = new Color(color.r, color.g, color.b, alpha);

        yield return null; // Yield once to prevent instant loop execution issues
    }
}
