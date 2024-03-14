using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class fetch : MonoBehaviour
{
    string json; // Declare json variable here

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetText());
    }

    // Update is called once per frame
    IEnumerator GetText()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://api.openf1.org/v1/car_data?driver_number=55&session_key=9159&speed>=315"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                json = www.downloadHandler.text;
                Debug.Log(json);
                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
            }
        }
    }
}
