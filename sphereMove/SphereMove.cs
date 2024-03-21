using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;


public class SphereMove : MonoBehaviour
{
    string json; // Variable to store received JSON data
    CarData car = new CarData(); // the current car info
    bool requestDataReceived = false; // Flag to track whether the first non-zero car location has been received
    float updateInterval = 1f / 3.7f; // x frequency of approximately 3.7Hz
    string url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81"; // An event at Monza
    bool new_trajectory = true;

    List<float> listX = new List<float> { 0, 0, 0, 0, 0, 0, 0 }; // List for x positions
    List<float> listY = new List<float> { 0, 0, 0, 0, 0, 0, 0 }; // List for y positions
    List<DateTime> listTime = new List<DateTime>(); // List for times

    List<float> interpolatedPosX = new List<float>(); // Store the interpolated positionsw public array<float>(N*10); // Store the interpolated positions
    List<float> interpolatedPosY = new List<float>(); // Store the interpolated positions
    List<DateTime> interpolatedPosTime = new List<DateTime>(); // Store the interpolated times
    int N = 7;

    // Start is called before the first frame update
    void Start()
    {
        // this.gameObject.transform.localPosition = new Vector3(-320, -2052, -140); // Random pick of a initial point, can be deleted
        StartCoroutine("GetCarData"); // Start the coroutine to retrieve data from openF1
        StartCoroutine("DelayedUpdate");
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
                    CarData[] carDataArray = JsonHelper.FromCustomJson<CarData>(json);

                    // Check if any entry has 'x' or 'y' not zero, meaning car moves
                    // This is for real-time game, if the car has not moved, the x y will all be 0
                    foreach (CarData data in carDataArray)
                    {
                        if (data.x != 0 || data.y != 0)
                        {
                            requestDataReceived = true;
                            for (int i = 0; i < N; i++)
                            {
                                listTime.Add(data.date);
                            }
                            // Insert the new time to the list
                            listX.Insert(0, data.x); // Insert the new x position to the list
                            listY.Insert(0, data.y); // Insert the new y position to the list
                            Debug.Log("Get initial data x=" + car.x.ToString());
                            break;
                        }
                    }

                }
            }
            yield return new WaitForSeconds(1); // Wait for 1 second before making the next request
        }
        while (true) // After getting initial moving car
        {
            if (new_trajectory)
            {
                url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81&date>" + car.date + "&date<" + GetNextSecond(car.date, 2);
                Debug.Log("Try get url" + url); // Retrieve the next 2s car data
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
                            CarData[] carDataArray = JsonHelper.FromCustomJson<CarData>(json);
                            // Remove last car position and time
                            listX.RemoveAt(listX.Count - 1);
                            listY.RemoveAt(listY.Count - 1);
                            listTime.RemoveAt(listTime.Count - 1);

                            // Add next car position and time
                            listX.Insert(0, carDataArray[0].x);
                            listY.Insert(0, carDataArray[0].y);
                            listTime.Insert(0, carDataArray[0].date);

                            // Interpolate the car position
                            (float[] _x, float[] _y) = Cubic.InterpolateXY(listX.ToArray(), listY.ToArray(), N * 10);

                            // Find step size in interpolation in seconds
                            float secStep = (float) (listTime[N - 1] - listTime[0]).TotalSeconds / ((N - 1) * 10);

                            // Find first index after the middle of the array by time
                            int start_idx = (int) Math.Ceiling((listTime[(N - 1) / 2] - listTime[0]).TotalSeconds / secStep);

                            // Find last index before the middle of the array by time
                            int end_idx = (int) Math.Floor((listTime[(N - 1) / 2 + 1] - listTime[0]).TotalSeconds / secStep);

                            interpolatedPosX = _x.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();
                            interpolatedPosY = _y.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();

                            // Add API position/time to the interpolated position/time at the beginning
                            interpolatedPosX.Insert(0, listX[(N - 1) / 2]);
                            interpolatedPosY.Insert(0, listY[(N - 1) / 2]);
                            interpolatedPosTime.Insert(0, listTime[(N - 1) / 2]);

                            for (int i = start_idx; i <= end_idx; i++)
                            {
                                interpolatedPosTime.Insert(interpolatedPosTime.Count - 1, listTime[0].AddSeconds(secStep * i));
                            }

                            // New trajectory calculated. Do not calculate another one until interpolated positions have been used. 
                            new_trajectory = false;
                            // Update the car position
                            Debug.Log("Get new data x=" + car.x.ToString());
                        }
                        else
                        {
                            Debug.Log("Empty measurement");
                        }
                    }
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Update is called once per frame
    IEnumerator DelayedUpdate()
    {
        // Run through interpolatePosX elements and update when the respective time is reached
        while (true)
        {
            if (!new_trajectory)
            {
                for (int i = 0; i < interpolatedPosX.Count; i++)
                {
                    car.x = interpolatedPosX[i];
                    car.y = interpolatedPosY[i];
                    car.date = interpolatedPosTime[i];
                    Vector3 newPosition = new Vector3(car.x, car.y, this.gameObject.transform.localPosition.z);
                    this.gameObject.transform.localPosition = newPosition;
                    Debug.Log("Update x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString());

                    // Wait until the next time to update the position
                    yield return new WaitForSeconds((float)(interpolatedPosTime[i + 1] - interpolatedPosTime[i]).TotalSeconds);

                }

                // All interpolated positions have been used. Calculate new trajectory in the other coroutine
                new_trajectory = true;
            }
        }

        // float cos = 1F, sin = 0F, scale = 0.1F;
        // float trans_x = -0F, trans_y = -0F;
        // -------------- TODO ------------------
        // StopCoroutine("GetCarData") when race finished
        // Either stop it here in update() or stop it in the IEnumerator
    }

    // Return in format utcDateTime+durationSec
    public static string GetNextSecond(DateTime utcDateTime, int durationSec = 10)
    {
        //Debug.Log("Input " + utcDateTime);
        // Parse the input string to DateTime object
        //if (DateTime.TryParseExact(utcDateTime, "yyyy-MM-ddTHH:mm:ss.ffffff",
        //                            CultureInfo.InvariantCulture,
        //                            DateTimeStyles.None,
        //                            out DateTime dt))
        //{
        // Add one second to the parsed DateTime object
        DateTime dt = utcDateTime.AddSeconds(durationSec);

        // Format the DateTime object to ISO 8601 format
        //Debug.Log("Output " + dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
        return dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
        //}
        //else
        //{
        //    // Return an empty string if parsing fails
        //    return string.Empty;
        //}
    }

    // Define a class to hold car data structure
    [System.Serializable]
    public class CarData
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public DateTime date;

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
