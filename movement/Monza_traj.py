import json
from urllib.request import urlopen
import matplotlib.pyplot as plt
from datetime import datetime

# Fetch data from the API
response = urlopen('https://api.openf1.org/v1/location?session_key=9157&driver_number=81')
data = json.loads(response.read().decode('utf-8'))

# Extract x and y coordinates and datetime
x_coords = [-entry['x']/10 for entry in data]
y_coords = [entry['y']/10 for entry in data]
datetime_strings = [entry['date'] for entry in data] # in "yyyy-MM-ddTHH:mm:ss.ffffff" format
# Append ".000000" to datetime strings if milliseconds are missing
datetime_strings_with_ms = [dt_str + ".000000" if len(dt_str) == 19 else dt_str for dt_str in datetime_strings]

# Parse datetime strings
datetimes = [datetime.strptime(dt_str, "%Y-%m-%dT%H:%M:%S.%f") for dt_str in datetime_strings_with_ms]

# Calculate speed using differences in coordinates and time
speed = []
for i in range(1, len(x_coords)):
    distance = ((x_coords[i] - x_coords[i-1])**2 + (y_coords[i] - y_coords[i-1])**2)**0.5
    time_difference = (datetimes[i] - datetimes[i-1]).total_seconds()
    speed.append(distance / time_difference)

# Plot the 2D map with swapped coordinates
plt.figure(figsize=(8, 6))
plt.scatter(x_coords, y_coords, color='blue', marker='.', alpha=0.1)
plt.title('2D Map of x and y coordinates (Swapped and Reflected)')
plt.xlabel('X Coordinate')
plt.ylabel('Y Coordinate')
plt.grid(True)
plt.show()

elapsed_time = [(dt - datetimes[0]).total_seconds() for dt in datetimes]

# Plot speed curve according to time
plt.figure(figsize=(10, 6))
plt.plot(elapsed_time[1:], speed, color='green')  # Omit the first point since there's no speed calculation for it
plt.title('Speed Curve According to Time')
plt.xlabel('Time (s)')
plt.ylabel('Speed (m/s)')
plt.grid(True)
plt.show()
