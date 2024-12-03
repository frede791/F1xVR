# Initial Settings

## Unity Setting

1. Add a 3D sphere to the SampleScene.
2. Attach SmoothTracking.cs script to the 3D sphere.
3. Add camera to the scene if wanted.

For the z-axis in the map (the gravity axis), I did not set car.z, which can be obtained from the retrieved data. Instead I added mesh collider component (in the Inspector) for the map and rigidbody component for the sphere so then it will have gravitational force. I haven’t tried setting the car.z, can be tried.

## Scripts

### Monza_traj.py

The Monza_traj.py will generate data and plot for this race in Monza. Might be helpful for coordinate transformation and speed inspection.

### SmoothTracking.cs

- Only transformations are setting the scale to 0.1 and flipping x inputs which seem to be mirrored against the orientation of the map.
- Logic to implement is commented with "TODO". The most important is to identify the end of the game.
- set enableDebugLogs=true for Log information.

# Smooth tracking on the cleaned map

## Get textured cleaned map - Blender

1. Download .blend at [monza_clean.blend](https://1drv.ms/u/s!AoUFuadYhVFQkhLRwIBFD9CkydB9?e=FReUX1)

2. Open .blend file in blender

3. File -> External Data -> Pack then Unpack (create when necessary)

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417015031703.png" alt="image-20240417015031703" width="300"/>

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417015106319.png" width="300"/>

4. You should see a “textures” folder parallel to .blend

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417015138859.png" alt="image-20240417015138859" width="200"/>

5. File -> Export -> .fbx

   ![image-20240416222814621](C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/blender/README.assets/image-20240416222814621.png)

##  Get textured cleaned map - Unity

1. Open Unity Project

2. Create Assets/monza_clean/Resources (Follow this naming pls)

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417020045118.png" alt="image-20240417020045118" style="zoom:25%;" />

3. Import all images in “textures” to Resources folder

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417020203838.png" alt="image-20240417020203838" style="zoom:33%;" />

4. Import the .fbx to Assets/monza_clean

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417020127491.png" alt="image-20240417020127491" style="zoom:25%;" />


5. Select the .fbx, check the Read/Write checkbox

<img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/monza_import.png" alt="monza_import" style="zoom:38%;" />

6. Inspector -> Materials -> Extract Materials -> Select path Assets/monza_clean/Resources. Then in Resources folder you should have all images and a lot of metal balls without texture.

7. Attach the SaveTexture.cs script to something in the scene

8. Uncheck the script SmoothTracking.cs on Sphere temporarily

9. Run the game to run SaveTexture.cs

10. Look at the Log, there should only exist 3 warning (missing Image_0.001 0.002 0.003, these are fine). Then the Resources folder looks like:![image-20240417020444019](C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/image-20240417020444019.png)

11. Afterwards, you can attach the monza_clean scene to the SampleScene, all textures will be there

## Run the game

1. Set monza_clean transform to be roughly

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/blender/README.assets/image-20240416235339315.png" alt="image-20240416235339315" style="zoom:33%;" />

2. Add component “mesh collider” for the monza_clean, uncheck the collision detection choice “Use Fast Midphase”

3. After these, never inspect monza_clean scene again because it will repaint so much that everything stucks…

4. Remove the SaveTexture.cs script

5. Re-check the script SmoothTracking.cs on Sphere

6. Set the Sphere properties

   <img src="C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/blender/README.assets/image-20240417013405026.png" alt="image-20240417013405026" style="zoom:33%;" />

   

7. Unity Edit -> Project Settings

   ![proj_setting](C:/Users/ASUS/Desktop/MSc_Term1_2/VRI/Proj/F1xVR/gitRepo/F1xVR/movement/README.assets/proj_setting.png)

8. Run play, and if necessary swap from the "play" mode to "scene" mode to be able to watch the whole map.
