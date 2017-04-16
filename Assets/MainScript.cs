using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Accord;
using Accord.Math;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
//needed for gaussian and polynomial
using Accord.Statistics.Kernels;

public class MainScript : MonoBehaviour {

	AndroidJavaClass javaPluginObject;

	//visuals
	GameObject watch_GameObject;

	Material[] material_Array = new Material[9];

	//Gesture

	MulticlassSupportVectorMachine<Gaussian> svm;
	ArrayList trainingData_ArrayList = new ArrayList ();
	ArrayList trainingDataLabels_ArrayList = new ArrayList ();
	bool currentlyClassifying_bool = false;
	bool currentlyTraining_bool = false;
	int currentTrainingGesture_int = 0;
	int currentGesturePrediction_int = 0;

	float numberOfTrials_float = 1.0f;
	float numberOfGestures_float = 3.0f;

	int currentTrainingTrial_int = 0;

	// Use this for initialization
	void Start () 
	{
		createjavaPluginObject();

		watch_GameObject = GameObject.Find("ImportedWatch");

		for (int i = 0; i < material_Array.Length; i++) 
		{
			material_Array[i] = GameObject.Find("Sphere "+i).GetComponent<Renderer> ().material;
		}

			
	}

	void createjavaPluginObject()
	{
		javaPluginObject = new AndroidJavaClass("com.biointeractivetech.cypressble.bleBroadcastReceiver");
		// We call our java class function to create our MyReceiver java object
		javaPluginObject.CallStatic("createInstance");

		//outputMessage_string = "Created Java Plugin Object";
	}



	// Update is called once per frame
	void Update () 
	{
		retreiveAndProcessFsrData();
		retreiveAndProcessImuData();
	}

	void retreiveAndProcessFsrData()
	{

		//outputMessage_string = "retreiveAndProcessFsrData";

		string fsrOutputInitial_string = javaPluginObject.GetStatic<string>("fsrData");
		javaPluginObject.SetStatic<string> ("fsrData", "");

		//outputMessage_string += "\n" + fsrOutputInitial_string;

		string[] fsrOutputSpilt_stringArray = fsrOutputInitial_string.Split ('\n');

		string[] fsrIndividualSensorValues_stringArray = fsrOutputSpilt_stringArray[0].Split(',');


		double[] fsrIndividualSensorValues_doubleArray = new double[10];

		for( int i=0; i < 10; i++ )
		{
			
			fsrIndividualSensorValues_doubleArray [i] = double.Parse (fsrIndividualSensorValues_stringArray [i]);

		}

		// ====================================================================
		// Needs to be hardcoded and switched, because valuse dont come in order
		// ====================================================================

		float maxSensorValue = 160.0f;

		double value0 = fsrIndividualSensorValues_doubleArray[1] / maxSensorValue;
		double value1 = fsrIndividualSensorValues_doubleArray[2] / maxSensorValue;
		double value2 = fsrIndividualSensorValues_doubleArray[4] / maxSensorValue;
		double value3 = fsrIndividualSensorValues_doubleArray[3] / maxSensorValue;
		double value4 = fsrIndividualSensorValues_doubleArray[6] / maxSensorValue;
		double value5 = fsrIndividualSensorValues_doubleArray[5] / maxSensorValue;
		double value6 = fsrIndividualSensorValues_doubleArray[7] / maxSensorValue;
		double value7 = fsrIndividualSensorValues_doubleArray[8] / maxSensorValue;
		double value8 = fsrIndividualSensorValues_doubleArray[9] / maxSensorValue;


		material_Array[0].color = Color.Lerp (Color.green, Color.red, (float)value0);
		material_Array[1].color = Color.Lerp (Color.green, Color.red, (float)value1);
		material_Array[2].color = Color.Lerp (Color.green, Color.red, (float)value2);
		material_Array[3].color = Color.Lerp (Color.green, Color.red, (float)value3);
		material_Array[4].color = Color.Lerp (Color.green, Color.red, (float)value4);
		material_Array[5].color = Color.Lerp (Color.green, Color.red, (float)value5);
		material_Array[6].color = Color.Lerp (Color.green, Color.red, (float)value6);
		material_Array[7].color = Color.Lerp (Color.green, Color.red, (float)value7);
		material_Array[8].color = Color.Lerp (Color.green, Color.red, (float)value8);


		//training
		if( currentlyTraining_bool )
		{
			trainingData_ArrayList.Add (fsrIndividualSensorValues_doubleArray);

			//needs to start at 0 for some reason
			trainingDataLabels_ArrayList.Add (currentTrainingGesture_int-1);

			if(trainingData_ArrayList.Count == (currentTrainingGesture_int * 60) + ( currentTrainingTrial_int  * numberOfGestures_float * 60))
			{
				//currentTrainingGesture_int++;
				currentlyTraining_bool = false;

				if (currentTrainingGesture_int == numberOfGestures_float) 
				{
					currentTrainingGesture_int = 0;
					currentTrainingTrial_int++;

					if (currentTrainingTrial_int == numberOfTrials_float) 
					{
						trainModel ();
						currentlyClassifying_bool = true;
					}
				}
			}
		}

		//Classifying
		if(currentlyClassifying_bool)
		{
			/*
				double[][] inputs = new double[1][];
				inputs [0] = fsrIndividualSensorValues_doubleArray;

				int[] result = svm.Decide(inputs);
				currentGesturePrediction_int = result [0];
			*/

			int result = svm.Decide(fsrIndividualSensorValues_doubleArray);
			currentGesturePrediction_int = result;

		}

	}

	void retreiveAndProcessImuData()
	{

		//retrieve Imu data
		string imuOutputInitial_string = javaPluginObject.GetStatic<string>("imuData");
		javaPluginObject.SetStatic<string> ("imuData", "");

		//outputMessage += "\n" + imuOutputInitial_string;

		//split it based on line delimeters, just incase multiple came through
		string[] imuOutputSplit_string = imuOutputInitial_string.Split ('\n');

		for( int i=0; i < imuOutputSplit_string.Length; i++ )
		{

			//split the full string into sepereate data point strings
			string[] imuIndividualSensorValues_string = imuOutputSplit_string[i].Split(',');

			float[] imuIndividualSensorValues_float = new float[imuIndividualSensorValues_string.Length];

			//populate a float array with converted string values
			for( int j=0; j < imuIndividualSensorValues_float.Length; j++ )
			{
				imuIndividualSensorValues_float[j] = float.Parse(imuIndividualSensorValues_string[j]);
			}

			//assign the values to roll, pitch, and yaw
			float roll_float = imuIndividualSensorValues_float [0];
			float pitch_float = imuIndividualSensorValues_float [1];
			float yaw_float = imuIndividualSensorValues_float [2];

			//outputMessage_string += "\nRoll:" + roll_float;
			//outputMessage_string += "\nPitch:" + pitch_float;
			//outputMessage_string += "\nYaw:" + yaw_float;

			//rotate gameObject
			watch_GameObject.transform.eulerAngles = new UnityEngine.Vector3(pitch_float, 180, -roll_float);
		}
	}

	void trainModel()
	{

		double[][] inputs2 = new double[trainingData_ArrayList.Count][];

		for (int i = 0; i < trainingData_ArrayList.Count; i++) 
		{

			double[] trainingData_doubleArray = (double[])trainingData_ArrayList [i];

			inputs2[i] = trainingData_doubleArray;
		}


		int[] outputs2 =  new int[trainingDataLabels_ArrayList.Count];
		for (int i = 0; i < trainingDataLabels_ArrayList.Count; i++) 
		{
			outputs2 [i] = (int)trainingDataLabels_ArrayList [i];
		}



		// Create the multi-class learning algorithm for the machine
		var teacher = new MulticlassSupportVectorLearning<Gaussian>()
		{
			// Configure the learning algorithm to use SMO to train the
			//  underlying SVMs in each of the binary class subproblems.
			Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
			{
				// Estimate a suitable guess for the Gaussian kernel's parameters.
				// This estimate can serve as a starting point for a grid search.
				UseKernelEstimation = true
			}
		};

		// Configure parallel execution options
		//teacher.ParallelOptions.MaxDegreeOfParallelism = 1;

		Gaussian kernel = new Gaussian ();
		svm = new MulticlassSupportVectorMachine<Gaussian> (trainingData_ArrayList.Count, kernel, 3);
		// Learn a machine
		svm = teacher.Learn(inputs2, outputs2);

	}

	void OnGUI()
	{
		GUI.skin.label.fontSize = 40;

		GUI.Label(new Rect (30, 30, 1000, 2000), "Gesture Recognition");

		GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 25;

		if (!currentlyTraining_bool && !currentlyClassifying_bool) 
		{
			if (GUI.Button (new Rect (30, 90, 320, 75), "Train Gesture Number: " + (currentTrainingGesture_int + 1))) 
			{
				if (currentTrainingGesture_int < numberOfGestures_float) 
				{
					currentTrainingGesture_int++;
					currentlyTraining_bool = true;
				} 
			}

			if (currentTrainingGesture_int == 0 && currentTrainingTrial_int == 0) 
			{
				GUI.skin.horizontalSliderThumb.fixedWidth = 30;
				GUI.skin.horizontalSliderThumb.fixedHeight = 30;
				GUI.skin.horizontalSlider.fixedWidth = 320;
				GUI.skin.horizontalSlider.fixedHeight = 30;

				GUI.Label (new Rect (30, 185, 500, 2000), "Number of Training Trials: " + numberOfTrials_float);
				numberOfTrials_float = GUI.HorizontalSlider (new Rect (30, 225, 320, 30), numberOfTrials_float, 1.0f, 5.0f);
				numberOfTrials_float = (int)numberOfTrials_float;

				GUI.Label (new Rect (30, 270, 500, 2000), "Number of Gestures: " + numberOfGestures_float);
				numberOfGestures_float = GUI.HorizontalSlider (new Rect (30, 310, 320, 30), numberOfGestures_float, 3.0f, 10.0f);
				numberOfGestures_float = (int)numberOfGestures_float;
			} 
			else
			{
				if (GUI.Button (new Rect (Screen.width - 130 , 90, 100, 75), "Reset"))
				{
					currentlyClassifying_bool = false;
					currentTrainingGesture_int = 0;
					currentTrainingTrial_int = 0;

					trainingData_ArrayList = new ArrayList ();
					trainingDataLabels_ArrayList = new ArrayList ();
				}
			}

		}


		if(currentlyTraining_bool) 
		{
			GUI.Label(new Rect (32, 90, 1000, 2000), "Currently Sampling Training Data. Please Wait");
		}

		if(currentlyClassifying_bool)
		{
			if (GUI.Button (new Rect (Screen.width - 200 , 90, 170, 75), "Reset"))
			{
				currentlyClassifying_bool = false;
				currentTrainingGesture_int = 0;
				currentTrainingTrial_int = 0;

				trainingData_ArrayList = new ArrayList ();
				trainingDataLabels_ArrayList = new ArrayList ();
			}
			GUI.skin.label.fontSize = 40;
			GUI.Label(new Rect (30, 100, 1000, 2000), "Predicting Gesture Number: " + (currentGesturePrediction_int+1) );
		}

	}

}
