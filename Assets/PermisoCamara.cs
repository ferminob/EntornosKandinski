using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class PermisoCamara : MonoBehaviour
{
    // Arrastrá acá Solution y, si hace falta, el Canvas con el desplegable
    [SerializeField] GameObject[] objetosParaActivar;

    IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);

            yield return new WaitForSeconds(0.5f);
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                yield return new WaitForSeconds(0.25f);

            // margen para que Android registre las cámaras
            yield return new WaitForSeconds(0.5f);
        }
#endif
        Debug.Log("Cámaras: " + WebCamTexture.devices.Length);

        foreach (var obj in objetosParaActivar)
            obj.SetActive(true);

        yield break;
    }
}