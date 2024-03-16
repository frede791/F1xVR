using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Globalization;

public class SphereMove : MonoBehaviour
{
    string json; // Variable to store received JSON data
    CarData car = new CarData(); // the current car info
    bool requestDataReceived = false; // Flag to track whether the first non-zero car location has been received
    float updateInterval = 1f / 3.7f; // Update frequency of approximately 3.7Hz
    string url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81"; // An event at Monza

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.transform.localPosition = new Vector3(-320, -2052, -140); // Random pick of a initial point, can be deleted
        StartCoroutine("GetCarData"); // Start the coroutine to retrieve data from openF1
    }

    IEnumerator GetCarData()
    {
        while (!requestDataReceived) // Run until the car starts moving
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    json = www.downloadHandler.text;
                    // Parse JSON array as an array of CarData {[x:,y:,z:,date:,...], [x:,y:,z:,date:,...], [x:,y:,z:,date:,...]}
                    CarData[] carDataArray = JsonHelper.FromJson<CarData>(json);

                    // Check if any entry has 'x' or 'y' not zero, meaning car moves
                    // This is for real-time game, if the car has not moved, the x y will all be 0
                    foreach (CarData data in carDataArray)
                    {
                        if (data.x != 0 || data.y != 0)
                        {
                            requestDataReceived = true;
                            car.date = data.date;
                            car.x = data.x;
                            car.y = data.y;
                            Debug.Log("Get initial data x=" + car.x.ToString() + "date=" + car.date);
                            break;
                        }
                    }

                }
            }
            yield return new WaitForSeconds(1); // Wait for 1 second before making the next request
        }
        while (true // After getting initial moving car
        {
            url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81&date>" + car.date + "&date<" + GetNextSecond(car.date, 10);
            Debug.Log("Try get url" + url); // Retrieve the next 10s car data
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    json = www.downloadHandler.text;
                    if (json != "[]")
                    {
                        // Parse JSON array
                        CarData[] carDataArray = JsonHelper.FromJson<CarData>(json);
                        // Render the next car position
                        car.date = carDataArray[0].date;
                        car.x = carDataArray[0].x;
                        car.y = carDataArray[0].y;
                        Debug.Log("Get new data x=" + car.x.ToString() + "date=" + car.date);
                    }
                    else
                    {
                        Debug.Log("Empty measurement");
                    }
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Some bullshit transformation
        float cos = 0.92233238F, sin = 0.38639743F, scale = 0.10095949422356618F;
        float trans_x = -330F, trans_y = -2427F;
        Vector3 newPosition = new Vector3(scale * (cos * car.x - sin * car.y) + trans_x, scale * (sin * car.x + cos * car.y) + trans_y, this.gameObject.transform.localPosition.z);

        this.gameObject.transform.localPosition = newPosition;
        Debug.Log("Updata x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString() + "date=" + car.date);

        // -------------- TODO ------------------
        // StopCoroutine("GetCarData") when race finished
        // Either stop it here in update() or stop it in the IEnumerator
    }

    // Return in format utcDateTime+durationSec
    public static string GetNextSecond(string utcDateTime, int durationSec=10)
    {
        //Debug.Log("Input " + utcDateTime);
        // Parse the input string to DateTime object
        if (DateTime.TryParseExact(utcDateTime, "yyyy-MM-ddTHH:mm:ss.ffffff",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime dt))
        {
            // Add one second to the parsed DateTime object
            dt = dt.AddSeconds(durationSec);

            // Format the DateTime object to ISO 8601 format
            //Debug.Log("Output " + dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
        }
        else
        {
            // Return an empty string if parsing fails
            return string.Empty;
        }
    }

    // Define a class to hold car data structure
    [System.Serializable]
    public class CarData
    {
        public int x=0;
        public int y=0;
        public int z=0;
        public string date="";
        // You can add more fields as needed
    }

    // Helper class to parse JSON array
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}
