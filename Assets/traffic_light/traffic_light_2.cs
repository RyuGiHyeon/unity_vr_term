using UnityEngine;

public class traffic_light_2 : MonoBehaviour
{
    public GameObject redObject2;
    public GameObject greenObject2;
    public GameObject yellowObject2;

    private Material redMaterial;
    private Material greenMaterial;
    private Material yellowMaterial;

    private void Start()
    {
        // Initialize materials from objects
        redMaterial = redObject2.GetComponent<Renderer>().material;
        greenMaterial = greenObject2.GetComponent<Renderer>().material;
        yellowMaterial = yellowObject2.GetComponent<Renderer>().material;

        // Start initial coroutine with delay
        StartCoroutine(StartWithDelay());
    }

    private System.Collections.IEnumerator StartWithDelay()
    {
        // Wait for 45 seconds before starting the traffic light cycle
        yield return new WaitForSeconds(15f);

        // Start the transparency handling coroutine
        StartCoroutine(HandleTransparency());
    }

    private System.Collections.IEnumerator HandleTransparency()
    {
        while (true)
        {
            // Instantly toggle red object's transparency
            yield return ChangeTransparency(redMaterial, true);
            yield return new WaitForSeconds(45f);
            yield return ChangeTransparency(redMaterial, false);

            // Instantly toggle green object's transparency
            yield return ChangeTransparency(greenMaterial, true);
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
