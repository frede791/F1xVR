using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


public class SphereMove : MonoBehaviour
{
    string json; // Variable to store received JSON data
    CarData car = new CarData(); // the current car info
    float updateInterval = 1f / 3.7f; // x frequency of approximately 3.7Hz
    string url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81"; // An event at Monza
    bool need_new_trajectory = true;

    List<float> listX = new List<float>(); // List for x positions
    List<float> listY = new List<float>(); // List for y positions
    List<DateTime> listTime = new List<DateTime>(); // List for times

    List<float> interpolatedPosX = new List<float>(); // Store the interpolated positions
    List<float> interpolatedPosY = new List<float>(); // Store the interpolated positions
    List<DateTime> interpolatedPosTime = new List<DateTime>(); // Store the interpolated times
    const int N = 15; // Do interpolation with 7 points ahead and 7 points behind
    const int M = 35; // Interpolation points = N * M for each trajectory
    State currentState = State.Begin;

    DateTime current_tracking_time;
    

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("State -> Begin");
        // this.gameObject.transform.localPosition = new Vector3(-320, -2052, -140); // Random pick of a initial point, can be deleted
        StartCoroutine("GetCarData"); // Start the coroutine to retrieve data from openF1
        StartCoroutine("DelayedUpdate");
    }

    IEnumerator GetCarData()
    {
        while (true)
        {
            float[] _x, _y;
            float secStep;
            int start_idx, end_idx;

            switch (currentState)
            {
                case State.Begin:
                    if (need_new_trajectory)
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
                                int count = 0;
                                foreach (CarData data in carDataArray)
                                {
                                    if (data.x == 0 && data.y == 0) { continue; }
                                    if (count < N)
                                    {
                                        listTime.Add(GetDateTime(data.date));
                                        listX.Add((float)(-data.x * 0.1));
                                        listY.Add((float)(data.y * 0.1));
                                        count += 1;
                                    }
                                    else // Get N points
                                    {
                                        break;
                                    }
                                }
                                if (count == N)
                                {
                                    car.x = listX[0];
                                    car.y = listY[0];
                                    car.date = listTime[0].ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                                    (_x, _y) = Cubic.InterpolateXY(listX.AsEnumerable().ToArray(), listY.AsEnumerable().ToArray(), (N - 1) * M);
                                    secStep = (float)(listTime[N - 1] - listTime[0]).TotalSeconds / ((N - 1) * M - 1);
                                    start_idx = 0;
                                    end_idx = (int)Math.Ceiling((listTime[(N - 1) / 2 + 1] - listTime[0]).TotalSeconds / secStep - 1);
                                    interpolatedPosX = _x.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();
                                    interpolatedPosY = _y.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();
                                    interpolatedPosTime.Add(listTime[0]);
                                    for (int i = 1; i < interpolatedPosX.Count; i++)
                                    {
                                        interpolatedPosTime.Add(listTime[0].AddSeconds(secStep * (start_idx + i - 1)));
                                    }
                                    need_new_trajectory = false;
                                }
                                else
                                {
                                    listTime.Clear();
                                    listX.Clear();
                                    listY.Clear();
                                }
                            }
                        }
                    }
                    yield return new WaitForSeconds(1); // Wait for 1 second before making the next request
                    break;
                case State.Running:
                    if (need_new_trajectory)
                    {
                        url = "https://api.openf1.org/v1/location?session_key=9157&driver_number=81&date>" + listTime[listTime.Count - 1].ToString("yyyy-MM-ddTHH:mm:ss.ffffff")
                            + "&date<" + GetNextSecond(listTime[listTime.Count - 1].ToString("yyyy-MM-ddTHH:mm:ss.ffffff"), 2);
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
                                    listX.RemoveAt(0);
                                    listY.RemoveAt(0);
                                    listTime.RemoveAt(0);

                                    // Add next car position and time
                                    listX.Add((float)(- carDataArray[0].x * 0.1));
                                    listY.Add((float)(carDataArray[0].y * 0.1));
                                    listTime.Add(GetDateTime(carDataArray[0].date));

                                    // Interpolate the car position
                                    (_x, _y) = Cubic.InterpolateXY(listX.AsEnumerable().ToArray(), listY.AsEnumerable().ToArray(), (N-1) * M);
                                    // Find step size in interpolation in seconds
                                    secStep = (float)(listTime[N - 1] - listTime[0]).TotalSeconds / ((N-1) * M - 1);
                                    // Find first index after the middle of the array by time
                                    start_idx = (int)Math.Floor((listTime[(N - 1) / 2] - listTime[0]).TotalSeconds / secStep + 1);
                                    // Find last index before the middle of the array by time
                                    end_idx = (int)Math.Ceiling((listTime[(N - 1) / 2 + 1] - listTime[0]).TotalSeconds / secStep - 1);

                                    interpolatedPosX = _x.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();
                                    interpolatedPosY = _y.Skip(start_idx).Take(end_idx - start_idx + 1).ToList();
                                    if (interpolatedPosX.Any(float.IsNaN) || interpolatedPosY.Any(float.IsNaN))
                                    {
                                        interpolatedPosX.Clear();
                                        interpolatedPosY.Clear();
                                        Debug.Log("NaN Trajectory, let car stay.");
                                    }
                                    else
                                    {
                                        // Add API position/time to the interpolated position/time at the beginning
                                        interpolatedPosX.Insert(0, listX[(N - 1) / 2]);
                                        interpolatedPosY.Insert(0, listY[(N - 1) / 2]);
                                        interpolatedPosTime.Insert(0, listTime[(N - 1) / 2]);
                                        for (int i = 1; i < interpolatedPosX.Count; i++)
                                        {
                                            interpolatedPosTime.Add(listTime[0].AddSeconds(secStep * (start_idx + i - 1)));
                                        }
                                        // New trajectory calculated. Do not calculate another one until interpolated positions have been used. 
                                        need_new_trajectory = false;
                                        // Update the car position
                                        Debug.Log("New Trajectory generated");
                                    }
                                }
                                else
                                {
                                    Debug.Log("Empty measurement");
                                }
                            }
                        }
                    }
                    yield return new WaitForSeconds(updateInterval/10);
                    break;
                default:
                    yield return new WaitForSeconds(5);
                    break;
            }

        }
    }

    // Update is called once per frame
    IEnumerator DelayedUpdate()
    {
        // Run through interpolatePosX elements and update when the respective time is reached
        while (true)
        {
            switch (currentState)
            {
                case State.Begin:
                    if (!need_new_trajectory)
                    {
                        // TODO, if at the beginning stage the car is still, will give a NaN traj and give an error, ut should be fine bcs it changes to the Running stage quickly
                        current_tracking_time = listTime[0];
                        for (int i = 0; i < interpolatedPosX.Count; i++)
                        {
                            Thread.Sleep((int) Math.Round((interpolatedPosTime[i] - current_tracking_time).TotalSeconds * 1000)); // Blocked sleep in ms
                            car.x = interpolatedPosX[i];
                            car.y = interpolatedPosY[i];
                            car.date = interpolatedPosTime[i].ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                            Vector3 newPosition = new Vector3(car.x, car.y, this.gameObject.transform.localPosition.z);
                            this.gameObject.transform.localPosition = newPosition;
                            Debug.Log("Update x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString());
                            current_tracking_time = interpolatedPosTime[i];
                        }
                        interpolatedPosTime.Clear();
                        interpolatedPosX.Clear();
                        interpolatedPosY.Clear();
                        need_new_trajectory = true;
                        currentState = State.Running;
                        Debug.Log("State -> Running");
                    }
                    yield return new WaitForSeconds((float)0.0001);
                    break;
                case State.Running:
                    if (!need_new_trajectory)
                    {
                        for (int i = 0; i < interpolatedPosX.Count; i++)
                        {
                            // TODO: if we let it sleep until the next position point, it will anyway be unsmooth
                            Thread.Sleep((int)Math.Round((interpolatedPosTime[i] - current_tracking_time).TotalSeconds * 1000)); // Blocked sleep in ms
                            car.x = interpolatedPosX[i];
                            car.y = interpolatedPosY[i];
                            car.date = interpolatedPosTime[i].ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                            Vector3 newPosition = new Vector3(car.x, car.y, this.gameObject.transform.localPosition.z);
                            this.gameObject.transform.localPosition = newPosition;
                            Debug.Log("Update x=" + this.gameObject.transform.localPosition.x.ToString() + "y=" + this.gameObject.transform.localPosition.y.ToString());
                            current_tracking_time = interpolatedPosTime[i];
                        }
                        interpolatedPosTime.Clear();
                        interpolatedPosX.Clear();
                        interpolatedPosY.Clear();
                        need_new_trajectory = true;
                    }
                    yield return new WaitForSeconds((float) 0.0001);
                    break;
                default:
                    yield return new WaitForSeconds(updateInterval);
                    break;
            }
            
        }

        // -------------- TODO ------------------
        // StopCoroutine("GetCarData") when race finished
        // Either stop it here in update() or stop it in the IEnumerator
    }

    // Return in format utcDateTime+durationSec
    public static string GetNextSecond(string utcDateTime, int durationSec = 10)
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

    public static DateTime GetDateTime(string utcDateTime)
    {
        //Debug.Log("Input " + utcDateTime);
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
            Debug.Log("Cannot get DateTime!");
            return DateTime.MinValue;
        }
    }

    public enum State
    {
        Begin,
        Running,
        Ending,
        Stopped
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