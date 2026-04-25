using UnityEngine;

public class LipSync : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public AudioSource audioSource;
    public float sensitivity = 200f;
    public Transform playerCamera;
    public float activationDistance = 5f;
    public float activationAngle = 30f;

    void Update()
    {
        if (IsPlayerLooking())
        {
            if (!audioSource.isPlaying)
                audioSource.Play();

            float[] data = new float[256];
            audioSource.GetOutputData(data, 0);

            float volume = 0;
            foreach (var d in data) volume += Mathf.Abs(d);
            volume /= data.Length;

            float mouth = Mathf.Clamp(volume * sensitivity, 0, 100f);
            skinnedMesh.SetBlendShapeWeight(0, mouth * 10);
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            skinnedMesh.SetBlendShapeWeight(0, 0);
        }
    }

    bool IsPlayerLooking()
    {
        if (playerCamera == null) return false;

        float distance = Vector3.Distance(playerCamera.position, transform.position);
        if (distance > activationDistance) return false;

        // quanto mais perto, maior o ângulo aceite
        float dynamicAngle = Mathf.Lerp(90f, activationAngle, distance / activationDistance);

        Vector3 dirToStatue = (transform.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, dirToStatue);

        return angle < dynamicAngle;
    }
}