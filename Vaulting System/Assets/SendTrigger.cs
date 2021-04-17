using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject gameObject;
    DetectionCharacterController detectionController;
    void Start()
    {
        gameObject = GameObject.Find("Player");
        if (gameObject)
            detectionController = gameObject.GetComponent<DetectionCharacterController>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if(detectionController)
            detectionController.OnTriggerEnterEvent(other);
    }
}
