using UnityEngine;

public class CarController : MonoBehaviour, IInteractable
{
    [Header("Main")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private CarPathController carPathController;

    [SerializeField] private GameObject cameraPoint;
    [SerializeField] private GameObject winWindow;

    public bool CanInteract()
    {
        return canInteract;
    }

    public bool CanInteractWith(GameObject go)
    {
        return false;
    }

    public void SetCanIteract(bool value)
    {
        canInteract = value;
    }

    public void Interact(GameObject interactor)
    {
        Camera.main.transform.position = cameraPoint.transform.position;
        Camera.main.transform.localRotation = Quaternion.identity;
        Camera.main.transform.localPosition = Vector3.zero;

        carPathController.SetHeadlights(true);
        carPathController.audioSource.PlayOneShot(carPathController.doorCloseClip);
        carPathController.audioSource.PlayOneShot(carPathController.engineStartClip);

        canInteract = false;
        QuestManager.Instance.CompleteCurrentQuest();

        winWindow.SetActive(true);
    }
}
