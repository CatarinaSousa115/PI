using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LipSync : MonoBehaviour
{
    public SkinnedMeshRenderer meshRenderer;

    [Range(1, 20)]
    public float sensitivity = 10f;

    [Range(0, 100)]
    public float maxMouthOpen = 100f;

    private AudioSource audioSource;
    private int blendShapeIndex;

    private float[] samples = new float[256];

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        blendShapeIndex =
            meshRenderer.sharedMesh.GetBlendShapeIndex("Boca");
    }

    void Update()
    {
        audioSource.GetOutputData(samples, 0);

        float volume = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            volume += Mathf.Abs(samples[i]);
        }

        volume /= samples.Length;

        float mouthValue =
            Mathf.Clamp(volume * sensitivity * 100f, 0, maxMouthOpen);

        meshRenderer.SetBlendShapeWeight(
            blendShapeIndex,
            mouthValue
        );
    }
}