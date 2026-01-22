using UnityEngine;

public class HelperHotkeys : MonoBehaviour
{
    [SerializeField] private GameObject helperMessage;
    [SerializeField] private GameObject directionArrow;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Toggle(helperMessage);
        }

        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            Toggle(directionArrow);
        }
    }


    private void Toggle(GameObject go)
    {
        if (go == null) return;
        go.SetActive(!go.activeSelf);
    }
}
