using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Globalization;
using System.Collections.Generic;

// Keep class name the same as file name
public class SmoothTracking : MonoBehaviour
{
    string json; // Variable to store received JSON data
    CarData car = new CarData(); // the current car info

    string url; // URL to fetch from
    const string session_key = "9157";
    const string driver_number = "81";

    Queue<float> listX = new Queue<float>(); // List for x positions
    Queue<float> listY = new Queue<float>(); // List for y positions
    Queue<DateTime> listTime = new Queue<DateTime>(); // List for times

    const int threshold = 15; // try to always keep at least 15 destinations in the listX

    bool start_game = false;
    bool is_fetching = false;
    bool isLerping = false; // Flag to track if lerping is in progress

    DateTime current_appending_time;
    DateTime current_tracking_time;

    bool enableDebugLogs = false;

    Rigidbody rb; // Rigidbody component reference

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        if (enableDebugLogs) Debug.Log("Start");
        StartCoroutine("WaitForGameBegin");
    }

    IEnumerator WaitForGameBegin()
    {
        while (!start_game)
        {
            url = "https://api.openf1.org/v1/location?session_key=" + session_key + "&driver_number=" + driver_number; // An event at Monza
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    if (enableDebugLogs) Debug.Log(www.error);
                }
                else
                {
                    json = www.downloadHandler.text;
                    // Parse JSON array as an array of CarData {[x:,y:,z:,date:,...], [x:,y:,z:,date:,...], [x:,y:,z:,date:,...]}
                    CarData[] carDataArray = JsonHelper.FromCustomJson<CarData>(json);

                    // Check if any entry has 'x' or 'y' not zero, meaning car moves
                    // This is for real-time game, if the car has not moved, the x y will all be 0
                    foreach (CarData data in carDataArray)
                    {
                        if (data.x == 0 && data.y == 0) { continue; }
                        if (listX.Count < threshold + 1)
                        {
                            listTime.Enqueue(GetDateTime(data.date));
                            listX.Enqueue((float)(-data.x * 0.1));
                            listY.Enqueue((float)(data.y * 0.1));
                            current_appending_time = GetDateTime(data.date);
                        }
                        else // Get threshold points
                        {
                            break;
                        }
                    }
                    if (listX.Count >= threshold + 1)
                    {
                        start_game = true;
                        car.x = listX.Dequeue();
                        car.y = listY.Dequeue();
                        current_tracking_time = listTime.Dequeue();
                        car.date = current_tracking_time.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");

                        Vector3 newPosition = new Vector3(car.x, car.y, this.gameObject.transform.localPosition.z);
                        rb.MovePosition(newPosition);
                        if (enableDebugLogs) Debug.Log("Initial x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString());
                    }
                    else
                    {
                        listTime.Clear();
                        listX.Clear();
                        listY.Clear();
                        yield return new WaitForSeconds(1f); //while again to request after 1s
                    }
                }

            }
        }
    }

    IEnumerator FetchandCacheData()
    {
        url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81&date>" + current_appending_time.ToString("yyyy-MM-ddTHH:mm:ss.ffffff")
            + "&date<" + GetNextSecond(current_appending_time.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"), 2);
        if (enableDebugLogs) Debug.Log("Try get url" + url); // Retrieve the next 2s car data
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                if (enableDebugLogs) Debug.Log(www.error);
            }
            else
            {
                json = www.downloadHandler.text;
                if (json != "[]")
                {
                    // Parse JSON array
                    CarData[] carDataArray = JsonHelper.FromCustomJson<CarData>(json);

                    foreach (CarData data in carDataArray)
                    {
                        listTime.Enqueue(GetDateTime(data.date));
                        listX.Enqueue((float)(-data.x * 0.1));
                        listY.Enqueue((float)(data.y * 0.1));
                        current_appending_time = GetDateTime(data.date);
                        yield return null; // Append one every frame
                    }
                }
                else
                {
                    if (enableDebugLogs) Debug.LogWarning("Empty measurement");
                    // -------------- TODO ------------------
                    // 2s no data, race ends?
                    // StopCoroutine when race finished
                    // Either stop it here in update() or stop it in the IEnumerator
                }
            }
        }
        is_fetching = false;
    }

    
    private void Update()
    {
        if (!start_game) return;
        if ( (!isLerping) && listX.Count < 1 )
        {
            // If this warining doesn't show, then in this update frame the car will definitely be moved forward a bit
            if (enableDebugLogs) Debug.LogWarning("Update frame no lerping destination!");
        } 
        else if ( (!isLerping) && listX.Count >= 1 )
        {
            car.x = listX.Dequeue();
            car.y = listY.Dequeue();
            DateTime destination_time = listTime.Dequeue();
            car.date = destination_time.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
            Vector3 newPosition = new Vector3(car.x, car.y, this.gameObject.transform.localPosition.z);
            isLerping = true;
            // Start the coroutine to lerp to the first destination in list
            StartCoroutine(LerpToPosition(newPosition, (float)(destination_time - current_tracking_time).TotalSeconds));
            current_tracking_time = destination_time;
        }
        if ( (!is_fetching) && listX.Count < threshold)
        {
            is_fetching = true;
            StartCoroutine("FetchandCacheData"); // Start the coroutine to retrieve 2s data once
        }
    }


    IEnumerator LerpToPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = this.gameObject.transform.localPosition;
        float startTime = Time.time;
        while ((Time.time - startTime) < duration)
        {
            rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, (Time.time - startTime) / duration));
            if (enableDebugLogs) Debug.Log("Lerp update x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString());
            yield return null;
        }
        // Ensure the final position is exactly the target position
        rb.MovePosition(targetPosition);
        isLerping = false;
    }

    // Return in format utcDateTime+durationSec
    public static string GetNextSecond(string utcDateTime, double durationSec = 10)
    {
        // Parse the input string to DateTime object
        if (DateTime.TryParseExact(utcDateTime, "yyyy-MM-ddTHH:mm:ss.ffffff",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime dt))
        {
            dt = dt.AddSeconds(durationSec);
            // Format the DateTime object to ISO 8601 format
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
        }
        else
        {
            // Return an empty string if parsing fails
            return string.Empty;
        }
    }

    public DateTime GetDateTime(string utcDateTime)
    {
        // Have no idea why there's a fking '2023-09-03T13:48:36' without microsec in Monza date data
        // So I append 000000 to make it '2023-09-03T13:48:36.000000'
        if (!utcDateTime.Contains('.'))
        {
            utcDateTime += ".000000";
        }

        // Parse the input string to DateTime object
        if (DateTime.TryParseExact(utcDateTime, "yyyy-MM-ddTHH:mm:ss.ffffff",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime dt))
        {
            return dt;
        }
        else
        {
            if (enableDebugLogs) Debug.LogWarning("Cannot get DateTime!");
            return DateTime.MinValue;
        }
    }

    // Define a class to hold car data structure
    [System.Serializable]
    public class CarData
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public string date;

        // You can add more fields as needed
    }

    // Helper class to parse JSON array
    public static class JsonHelper
    {
        public static T[] FromCustomJson<T>(string json)
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