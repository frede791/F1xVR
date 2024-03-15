1. Add a 3D sphere to the map scene, set whatever size for the sphere.
2. Attach MoveSphere.cs script to the 3D sphere.
3. Add camera to the scene.
4. Play it and view the result.

For the z-axis in the map (the gravity axis), I did not set car.z, which can be obtained from the retrieved data. Instead I added mesh collider component (in the Inspector) for the map and rigidbody component for the sphere so then it will have gravitational force. I haven’t tried setting the car.z, can be tried.

The Monza_traj.py will generate data and plot for this race in Monza. Might be helpful for coordinate transformation.
