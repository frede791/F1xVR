import json
from urllib.request import urlopen
import matplotlib.pyplot as plt

# Fetch data from the API
response = urlopen('https://api.openf1.org/v1/location?session_key=9157&driver_number=81')
data = json.loads(response.read().decode('utf-8'))

# Extract x and y coordinates
x_coords = [entry['x'] for entry in data]
y_coords = [entry['y'] for entry in data]

# Plot the 2D map with swapped coordinates
plt.figure(figsize=(8, 6))
plt.scatter(x_coords, y_coords, color='blue', marker='.', alpha=0.1)
plt.title('2D Map of x and y coordinates (Swapped and Reflected)')
plt.xlabel('X Coordinate')
plt.ylabel('Y Coordinate')
plt.grid(True)
plt.show()
